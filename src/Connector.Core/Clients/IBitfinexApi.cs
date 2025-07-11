using Refit;

namespace Connector.Core.Clients;

public interface IBitfinexApi
{
    [Get("/trades/{pair}/hist?limit={limit}")]
    Task<decimal[][]> GetTradesAsync(string pair, int limit);

    [Get("/candles/trade:{timeframe}:{pair}/hist?start={start}&end={end}&limit={limit}")]
    Task<decimal[][]> GetCandleSeriesAsync(string pair, string timeframe, long? start = null, long? end = null, int? limit = null);

    [Get("/ticker/{pair}")]
    Task<decimal[]> GetTickerAsync(string pair);
}
