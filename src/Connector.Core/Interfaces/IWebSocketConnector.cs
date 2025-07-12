using Connector.Core.Models;

namespace Connector.Core.Interfaces;

public interface IWebSocketConnector
{
    event Action<Trade> NewBuyTrade;
    event Action<Trade> NewSellTrade;
    Task SubscribeTradesAsync(string pair);
    Task UnsubscribeTradesAsync(string pair);

    event Action<IEnumerable<Candle>> CandleSeriesProcessing;
    Task SubscribeCandlesAsync(string pair, int periodInSec);
    Task UnsubscribeCandlesAsync(string pair);
}
