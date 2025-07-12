using Connector.Core.Models;

namespace Connector.Core.Interfaces;

//todo: упомянуть в доках про:
//1. изменение возвращаемых значений у методов с void на Task
//2. удаление неиспользуемых параметров методов
//3. IEnumerable<Candle> в ивенте CandleSeriesProcessing вместо Candle
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
