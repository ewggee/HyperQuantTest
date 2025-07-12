using Connector.Core.Connectors;
using Refit;

namespace Connector.IntegrationTests;

public class BitfinexConnectorIntegrationTests : IDisposable
{
    private readonly BitfinexConnector _connector;

    public BitfinexConnectorIntegrationTests()
    {
        _connector = new BitfinexConnector();
    }

    [Theory]
    [InlineData("tBTCUSD")]
    [InlineData("fBTC")]
    public async Task GetNewTradesAsync_OnlyValidPair_ReturnsTrade(string pair)
    {
        var sideValues = new[] { "buy", "sell" };
        var defaultLimit = 125;

        var trades = await _connector.GetNewTradesAsync(pair);

        Assert.NotEmpty(trades);
        Assert.True(trades.Count() <= defaultLimit);
        Assert.All(trades, t =>
        {
            Assert.Contains(t.Side, sideValues);
        });
    }

    [Theory]
    [InlineData("INVALID")]
    public async Task GetNewTradesAsync_InvalidPair_ReturnsEmptyResult(string pair)
    {
        var trades = await _connector.GetNewTradesAsync(pair);

        Assert.Empty(trades);
    }

    [Theory]
    [InlineData("tBTCUSD", false, 100)]
    public async Task GetNewTradesAsync_AllValidParameters_ReturnsTrade(string pair, bool sortAsc, int limit)
    {
        var sideValues = new[] { "buy", "sell" };
        var from = DateTimeOffset.UtcNow.AddHours(-24);
        var to = DateTimeOffset.UtcNow;

        var trades = await _connector.GetNewTradesAsync(pair, from, to, sortAsc, limit);

        Assert.NotEmpty(trades);
        Assert.True(trades.Count() <= limit);
        Assert.All(trades, t =>
        {
            Assert.InRange(t.Time, from, to);
            Assert.Contains(t.Side, sideValues);
        });
    }

    [Theory]
    [InlineData("tBTCUSD", 300, 100)]
    public async Task GetCandleSeriesAsync_AllValidParameters_ReturnsCandles(string pair, int periodInSec, int limit)
    {
        var from = DateTimeOffset.UtcNow.AddHours(-24);
        var to = DateTimeOffset.UtcNow;

        var candles = await _connector.GetCandleSeriesAsync(pair, periodInSec, from, to, limit);

        Assert.NotEmpty(candles);
        Assert.True(candles.Count() <= limit);
        Assert.All(candles, c =>
        {
            Assert.InRange(c.OpenTime, from, to);
        });
    }

    [Theory]
    [InlineData("tBTCUSD", 200)]
    [InlineData("tBTCUSD", 700)]
    public async Task GetCandleSeriesAsync_InvalidPeriodInSec_ThrowsArgumentException(string pair, int periodInSec)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _connector.GetCandleSeriesAsync(pair, periodInSec, null));
    }

    [Theory]
    [InlineData("tBTCUSD")]
    public async Task GetTickerAsync_ValidPair_ReturnsTicker(string pair)
    {
        var ticker = await _connector.GetTickerAsync(pair);

        Assert.NotNull(ticker);
    }

    [Theory]
    [InlineData("INVALID")]
    public async Task GetTickerAsync_InvalidPair_Throws(string pair)
    {
        await Assert.ThrowsAsync<ApiException>(() => _connector.GetTickerAsync(pair));
    }

    public void Dispose()
    {
        _connector.Dispose();
        GC.SuppressFinalize(this);
    }
}