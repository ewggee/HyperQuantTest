using Refit;

namespace Connector.Core.Clients;

public interface IBitfinexApi
{
    [Get("/trades/{pair}/hist?limit={limit}&sort={sort}&start={start}&end={end}")]
    Task<decimal[][]> GetTradesAsync(string pair, int limit = 125, int sort = -1, long? start = null, long? end = null);

    [Get("/candles/trade:{timeframe}:{pair}/hist?start={start}&end={end}&limit={limit}")]
    Task<decimal[][]> GetCandleSeriesAsync(string pair, string timeframe, long? start = null, long? end = null, int? limit = null);

    [Get("/ticker/{pair}")]
    Task<decimal[]> GetTickerAsync(string pair);
}
