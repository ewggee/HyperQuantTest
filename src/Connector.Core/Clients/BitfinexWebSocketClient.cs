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
    public event Action<IEnumerable<Candle>> CandleSeriesProcessing = null!;

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
        var buffer = new byte[64 * 1024];
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

        // Обработка подписок
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("event", out var eventProp)
            && eventProp.GetString() == "subscribed")
        {
            ProcessSubscribeMessage(root);
        }
        // Обработка данных торгов
        else if (root.ValueKind == JsonValueKind.Array
            && root.GetArrayLength() > 1)
        {
            ProcessDataMessage(root);
        }
    }

    private void ProcessSubscribeMessage(JsonElement root)
    {
        var chanId = root.GetProperty("chanId").GetInt32();
        var channel = root.GetProperty("channel").GetString();

        var symbol = root.TryGetProperty("symbol", out var symbolProp)
            ? symbolProp.GetString()
            : root.GetProperty("key").GetString()?.Split(':').Last();

        _subscriptions[chanId] = $"{channel}:{symbol}";
    }

    private void ProcessDataMessage(JsonElement root)
    {
        var channelId = root[0].GetInt32();
        if (!_subscriptions.TryGetValue(channelId, out var channelType))
            return;

        if (channelType.StartsWith("trades"))
        {
            ProcessTradeMessage(root);
        }
        else if (channelType.StartsWith("candles"))
        {
            ProcessCandleMessage(root);
        }
    }

    private void ProcessTradeMessage(JsonElement root)
    {
        if (root.GetArrayLength() < 3 || root[1].ValueKind != JsonValueKind.String)
            return;

        // te - Trade Execution, tu - Trade Update
        var messageType = root[1].GetString();
        if (messageType != "te" && messageType != "tu")
            return;

        var tradeData = root[2];
        if (tradeData.ValueKind != JsonValueKind.Array || tradeData.GetArrayLength() < 4)
            return;

        var channelId = root[0].GetInt32();
        if (!_subscriptions.TryGetValue(channelId, out var channelInfo))
            return;

        var pair = channelInfo.Split(':')[1];

        var trade = new Trade
        {
            Id = tradeData[0].GetInt64().ToString(),
            Time = DateTimeOffset.FromUnixTimeMilliseconds(tradeData[1].GetInt64()),
            Amount = Math.Abs(tradeData[2].GetDecimal()),
            Price = tradeData[3].GetDecimal(),
            Side = tradeData[2].GetDecimal() > 0 ? "buy" : "sell", 
            Pair = pair
        };

        if (trade.Side == "buy")
            NewBuyTrade?.Invoke(trade);
        else
            NewSellTrade?.Invoke(trade);
    }

    private void ProcessCandleMessage(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
            return;

        var candlesArray = root[1];
        if (candlesArray.ValueKind != JsonValueKind.Array || candlesArray.GetArrayLength() == 0)
            return;

        var candlesList = new List<Candle>();
        foreach (var candleElement in candlesArray.EnumerateArray())
        {
            if (candleElement.ValueKind != JsonValueKind.Array || candleElement.GetArrayLength() < 6)
                continue;

            var candle = new Candle
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candleElement[0].GetInt64()),
                OpenPrice = candleElement[1].GetDecimal(),
                ClosePrice = candleElement[2].GetDecimal(),
                HighPrice = candleElement[3].GetDecimal(),
                LowPrice = candleElement[4].GetDecimal(),
                TotalVolume = candleElement[5].GetDecimal()
            };

            candlesList.Add(candle);
        }
        CandleSeriesProcessing?.Invoke(candlesList);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        GC.SuppressFinalize(this);
    }
}
