using Microsoft.AspNetCore.Mvc;
using RemesaX.Core.Dtos;
using RemesaX.Core.Interfaces;

namespace RemesaX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RemittancesController : ControllerBase
{
    private readonly IRemittanceService _remittanceService;
    private readonly ILogger<RemittancesController> _logger;

    public RemittancesController(
        IRemittanceService remittanceService,
        ILogger<RemittancesController> logger)
    {
        _remittanceService = remittanceService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva remesa: emite USDX al destinatario en Stellar testnet
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RemittanceResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateRemittanceRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        _logger.LogInformation("Iniciando remesa: {Amount} USDX a {Destination}",
            request.Amount, request.DestinationAddress);

        try
        {
            var result = await _remittanceService.CreateRemittanceAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Remesa rechazada");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar remesa");
            return StatusCode(500, new { error = "Error interno" });
        }
    }

    /// <summary>
    /// Consulta el estado de una remesa por su ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RemittanceResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _remittanceService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Lista las últimas remesas (dashboard)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 20)
    {
        var results = await _remittanceService.GetRecentAsync(take);
        return Ok(results);
    }
}
