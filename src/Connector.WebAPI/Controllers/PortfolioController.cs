using Connector.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Connector.WebAPI.Controllers;

[ApiController]
[Route("api/portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioService _portfolioService;

    public PortfolioController(PortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpPost("balance")]
    public async Task<IActionResult> CalculcateBalance([FromBody] Dictionary<string, decimal> assets)
    {
        var balance = await _portfolioService.CalculateBalancesAsync(assets);

        return Ok(balance);
    }
}
