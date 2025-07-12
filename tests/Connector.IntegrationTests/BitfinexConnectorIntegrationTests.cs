using Connector.Core.Connectors;
using Connector.Core.Models;
using Refit;

namespace Connector.IntegrationTests;

public class BitfinexConnectorIntegrationTests : IDisposable
{
    private readonly TimeSpan Timeout = TimeSpan.FromMinutes(60);
    private readonly BitfinexConnector _connector;

    public BitfinexConnectorIntegrationTests()
    {
        _connector = new BitfinexConnector();
    }

    public void Dispose()
    {
        _connector.Dispose();
        GC.SuppressFinalize(this);
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

    [Fact]
    public async Task NewBuySellTrade_ReturnsTrade()
    {
        var pairs = new string[] { "tETHUSD", "tBTCUSD", "tXRPUSD" };

        var tradeReceived = new TaskCompletionSource<bool>();
        Trade? receivedTrade = null;

        _connector.NewBuyTrade += trade =>
        {
            receivedTrade = trade;
            tradeReceived.SetResult(true);
        };
        _connector.NewSellTrade += trade =>
        {
            receivedTrade = trade;
            tradeReceived.SetResult(true);
        };

        foreach (var pair in pairs)
        {
            await _connector.SubscribeTradesAsync(pair);
        }

        var completedTask = await Task.WhenAny(tradeReceived.Task, Task.Delay(Timeout));

        if (completedTask == tradeReceived.Task)
        {
            Assert.NotNull(receivedTrade);
        }
        else
        {
            Assert.Fail($"No trade received within the timeout period: {Timeout}.\nPlease try again or increase the '{nameof(Timeout)}' value.");
        }
    }

    [Fact]
    public async Task CandleSeriesProcessing_ReturnsCandle()
    {
        var pairs = new string[] { "tETHUSD", "tBTCUSD", "tXRPUSD" };
        var periodInSecForHour = (int)TimeSpan.FromHours(1).TotalSeconds;

        var candlesReceived = new TaskCompletionSource<bool>();
        IEnumerable<Candle>? receivedCandles = null;

        _connector.CandleSeriesProcessing += candles =>
        {
            receivedCandles = candles;
            candlesReceived.SetResult(true);
        };

        foreach (var pair in pairs)
        {
            await _connector.SubscribeCandlesAsync(pair, periodInSecForHour);
        }

        var completedTask = await Task.WhenAny(candlesReceived.Task, Task.Delay(Timeout));

        if (completedTask == candlesReceived.Task)
        {
            Assert.NotNull(receivedCandles);
        }
        else
        {
            Assert.Fail($"No candle received within the timeout period: {Timeout}.\nPlease try again or increase the '{nameof(Timeout)}' value.");
        }
    }
}