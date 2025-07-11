using Connector.Core.Models;

namespace Connector.Core.Interfaces;

public interface IRestConnector
{
    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);

    //todo: вынести в доки
    //Тип для count изменил с long на int, т.к. апи максимум может вернуть 10000 записей, поэтому long в данном случае считаю оверхедом
    //https://docs.bitfinex.com/reference/rest-public-candles#response-fields:~:text=Number%20of%20records%20in%20response%20(max.%2010000).
    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, int? count = 0);

    Task<Ticker> GetTickerAsync(string pair);
}
