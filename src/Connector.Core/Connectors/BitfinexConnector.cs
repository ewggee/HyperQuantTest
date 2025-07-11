using Connector.Core.Clients;
using Connector.Core.Interfaces;
using Connector.Core.Models;

namespace Connector.Core.Connectors;

public class BitfinexConnector : ITestConnector, IDisposable
{
    private readonly BitfinexRestClient _restClient;
    private readonly BitfinexWebSocketClient _wsClient;

    public BitfinexConnector()
    {
        _restClient = new BitfinexRestClient();
        _wsClient = new BitfinexWebSocketClient();

        _wsClient.ConnectAsync().GetAwaiter().GetResult();
    }

    public event Action<Trade> NewBuyTrade
    {
        add => _wsClient.NewBuyTrade += value;
        remove => _wsClient.NewBuyTrade -= value;
    }

    public event Action<Trade> NewSellTrade
    {
        add => _wsClient.NewSellTrade += value;
        remove => _wsClient.NewSellTrade -= value;
    }

    public event Action<Candle> CandleSeriesProcessing
    {
        add => _wsClient.CandleSeriesProcessing += value;
        remove => _wsClient.CandleSeriesProcessing -= value;
    }

    public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
    {
        return _restClient.GetTradesAsync(pair, maxCount);
    }

    public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, int? count = 0)
    {
        return _restClient.GetCandlesAsync(pair, periodInSec, from, to, count);
    }

    public Task<Ticker> GetTickerAsync(string pair)
    {
        return _restClient.GetTickerAsync(pair);
    }

    public Task SubscribeTradesAsync(string pair)
    {
        return _wsClient.SubscribeTradesAsync(pair);
    }

    public Task UnsubscribeTradesAsync(string pair)
    {
        return _wsClient.UnsubscribeTradesAsync(pair);
    }

    public Task SubscribeCandlesAsync(string pair, int periodInSec)
    {
        return _wsClient.SubscribeCandlesAsync(pair, periodInSec);
    }

    public Task UnsubscribeCandlesAsync(string pair)
    {
        return _wsClient.UnsubscribeCandlesAsync(pair);
    }

    public void Dispose()
    {
        _wsClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
