using Connector.Core.Exceptions;
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
    public async Task<IEnumerable<Trade>> GetTradesAsync(string pair, int limit)
    {
        if (limit < 0 || limit > 10_000)
            throw new ArgumentException("Limit must be between 1 and 10'000");

        //todo: упомянуть в доках про контекст синхронизации
        var response = await _api.GetTradesAsync(pair, limit)
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

        //todo: вынести в доки
        //Спорный момент насчёт decimal-свойств класса Candle,
        //т.к. апи возвращает натуральное число для: OPEN, CLOSE, HIGH, LOW
        //https://docs.bitfinex.com/reference/rest-public-candles#response-fields
        return response.Select(c => new Candle
        {
            OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((int)c[0]),
            OpenPrice = c[1],
            HighPrice = c[2],
            LowPrice = c[3],
            ClosePrice = c[4],
            TotalVolume = c[5]
        });
    }

    //todo: упомянуть про ticker в ТЗ
    /// <exception cref="BitfinexApiException"></exception>
    public async Task<Ticker> GetTickerAsync(string pair)
    {
        var response = await _api.GetTickerAsync(pair)
            .ConfigureAwait(false);

        if (response[0].ToString() == "error" && response[1] == 10020)
        {
            throw new BitfinexApiException(
                errorCode: (int)response[1],
                message: $"Invalid pair: {pair}");
        }

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
