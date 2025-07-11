using Connector.Core.Helpers;
using Connector.Core.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Connector.Core.Clients;

public class BitfinexWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _ws;
    private readonly Uri _wsUri = new Uri("wss://api-pub.bitfinex.com/ws/2");
    private readonly Dictionary<int, string> _subscriptions;
    private readonly CancellationTokenSource _cts;

    public event Action<Trade> NewBuyTrade = null!;
    public event Action<Trade> NewSellTrade = null!;
    public event Action<Candle> CandleSeriesProcessing = null!;

    public BitfinexWebSocketClient()
    {
        _ws = new ClientWebSocket();
        _subscriptions = new Dictionary<int, string>();
        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_ws.State == WebSocketState.Open) return;

        await _ws.ConnectAsync(_wsUri, cancellationToken);

        try
        {
            _ = Task.Run(ReceiveMessages, _cts.Token);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Task SubscribeTradesAsync(string pair)
    {
        var message = JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "trades",
            symbol = pair
        });

        return SendMessageAsync(message);
    }

    public Task SubscribeCandlesAsync(string pair, int periodInSec)
    {
        var timeframe = BitfinexTimeframeHelper.PeriodInSecToTimeframe(periodInSec);
        var message = JsonSerializer.Serialize(new
        {
            @event = "subscribe",
            channel = "candles",
            key = $"trade:{timeframe}:{pair}"
        });

        return SendMessageAsync(message);
    }

    public Task UnsubscribeTradesAsync(string pair)
    {
        return UnsubscribeAsync("trades", pair);
    }

    public Task UnsubscribeCandlesAsync(string pair)
    {
        return UnsubscribeAsync("candles", pair);
    }

    private Task SendMessageAsync(string message)
    {
        return _ws.SendAsync(
            buffer: Encoding.UTF8.GetBytes(message),
            messageType: WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken: _cts.Token);
    }

    private Task UnsubscribeAsync(string channel, string pair)
    {
        var channelId = _subscriptions.FirstOrDefault(x => x.Value.StartsWith($"{channel}:{pair}")).Key;
        if (channelId != 0)
        {
            var message = JsonSerializer.Serialize(new
            {
                @event = "unsubscribe",
                chanId = channelId
            });

            return SendMessageAsync(message);
        }

        return Task.CompletedTask;
    }

    private async Task ReceiveMessages()
    {
        var buffer = new byte[8192];
        while (_ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
        {
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        if (root.TryGetProperty("event", out var eventProp)
            && eventProp.GetString() == "subscribed")
        {
            _subscriptions[root.GetProperty("chanId").GetInt32()] =
                $"{root.GetProperty("channel").GetString()}:{root.GetProperty("symbol").GetString()}";

            return;
        }

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 1)
        {
            var channelId = root[0].GetInt32();
            if (_subscriptions.TryGetValue(channelId, out var channelType))
            {
                if (channelType.StartsWith("trades"))
                    ProcessTradeMessage(root);
                else if (channelType.StartsWith("candles"))
                    ProcessCandleMessage(root);
            }
        }
    }

    private void ProcessTradeMessage(JsonElement root)
    {
        if (root.GetArrayLength() > 1 && root[1].ValueKind == JsonValueKind.String)
        {
            var tradeType = root[1].GetString();
            if (tradeType == "tu" || tradeType == "te") // Trade update/execution
            {
                var trade = new Trade
                {
                    Id = root[3].GetInt64().ToString(),
                    Price = root[4].GetDecimal(),
                    Amount = Math.Abs(root[5].GetDecimal()),
                    Side = root[5].GetDecimal() > 0 ? "buy" : "sell",
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(root[2].GetInt64()),
                    Pair = root[3].GetString()
                };

                if (trade.Side == "buy")
                    NewBuyTrade?.Invoke(trade);
                else
                    NewSellTrade?.Invoke(trade);
            }
        }
    }

    private void ProcessCandleMessage(JsonElement root)
    {
        if (root.GetArrayLength() > 1 && root[1].ValueKind == JsonValueKind.Array)
        {
            var candleData = root[1];
            var candle = new Candle
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleData[0].GetInt64()),
                OpenPrice = candleData[1].GetDecimal(),
                HighPrice = candleData[3].GetDecimal(),
                LowPrice = candleData[4].GetDecimal(),
                ClosePrice = candleData[2].GetDecimal(),
                TotalVolume = candleData[5].GetDecimal()
            };
            CandleSeriesProcessing?.Invoke(candle);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        GC.SuppressFinalize(this);
    }
}
