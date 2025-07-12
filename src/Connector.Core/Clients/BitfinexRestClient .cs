using Connector.Core.Helpers;
using Connector.Core.Models;
using Refit;

namespace Connector.Core.Clients;

public class BitfinexRestClient
{
    private readonly IBitfinexApi _api;

    public BitfinexRestClient()
    {
        _api = RestService.For<IBitfinexApi>("https://api.bitfinex.com/v2/");
    }

    /// <exception cref="ArgumentException"></exception>
    public async Task<IEnumerable<Trade>> GetTradesAsync(
        string pair, DateTimeOffset? from = null, DateTimeOffset? to = null, bool sortAsc = false, int limit = 125)
    {
        if (limit < 0 || limit > 10_000)
            throw new ArgumentException("Limit must be between 1 and 10'000");

        if (from >= to)
            throw new ArgumentException("'from' date cannot be less than or equal to 'to'");

        var start = from?.ToUnixTimeMilliseconds();
        var end = to?.ToUnixTimeMilliseconds();

        var response = await _api.GetTradesAsync(pair, limit, sortAsc ? 1 : -1, start, end)
            .ConfigureAwait(false);

        return response.Select(t => new Trade
        {
            Id = t[0].ToString(),
            Time = DateTimeOffset.FromUnixTimeMilliseconds((long)t[1]),
            Amount = t[2],
            Side = t[2] > 0 ? "buy" : "sell",
            Price = t[3]
        });
    }

    /// <exception cref="ArgumentException"></exception>
    public async Task<IEnumerable<Candle>> GetCandlesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, int? limit = 0)
    {
        if (limit < 0 || limit > 10_000)
            throw new ArgumentException("Limit must be between 1 and 10,000");

        if (from >= to)
            throw new ArgumentException("'from' date cannot be less than or equal to 'to'");

        var timeFrame = BitfinexTimeframeHelper.PeriodInSecToTimeframe(periodInSec);

        var start = from?.ToUnixTimeMilliseconds();
        var end = to?.ToUnixTimeMilliseconds();

        var response = await _api.GetCandleSeriesAsync(pair, timeFrame, start, end, limit)
            .ConfigureAwait(false);

        return response.Select(c => new Candle
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)c[0]),
            OpenPrice = c[1],
            HighPrice = c[2],
            LowPrice = c[3],
            ClosePrice = c[4],
            TotalVolume = c[5]
        });
    }

    /// <exception cref="BitfinexApiException"></exception>
    public async Task<Ticker> GetTickerAsync(string pair)
    {
        var response = await _api.GetTickerAsync(pair)
            .ConfigureAwait(false);

        return new Ticker
        {
            Bid = response[0],
            BidSize = response[1],
            Ask = response[2],
            AskSize = response[3],
            DailyChange = response[4],
            DailyChangeRelative = response[5],
            LastPrice = response[6],
            DailyVolume = response[7],
            DailyHigh = response[8],
            DailyLow = response[9]
        };
    }
}
