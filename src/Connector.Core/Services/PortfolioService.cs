using Connector.Core.Interfaces;

namespace Connector.Core.Services;

public class PortfolioService
{
    private readonly ITestConnector _connector;

    public PortfolioService(ITestConnector connector)
    {
        _connector = connector;
    }

    public async Task<Dictionary<string, decimal>> CalculateBalancesAsync(Dictionary<string, decimal> assets)
    {
        var rates = new Dictionary<string, decimal>();

        foreach (var asset in assets.Keys)
        {
            var ticker = await _connector.GetTickerAsync($"t{asset}USD");
            rates[asset] = ticker.LastPrice;
        }

        decimal totalUsdValue = assets.Sum(asset => asset.Value * rates[asset.Key]);

        var balances = new Dictionary<string, decimal>
        {
            ["USDT"] = totalUsdValue
        };

        foreach (var targetCurrency in assets.Keys)
        {
            if (targetCurrency == "USDT") continue;

            balances[targetCurrency] = totalUsdValue / rates[targetCurrency];
        }

        return balances;
    }
}
