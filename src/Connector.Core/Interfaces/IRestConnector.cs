using Connector.Core.Models;

namespace Connector.Core.Interfaces;

public interface IRestConnector
{
    Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, DateTimeOffset? from, DateTimeOffset? to = null, bool sortAsc = false, int maxCount = 125);

    Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, int? count = 0);

    Task<Ticker> GetTickerAsync(string pair);
}
