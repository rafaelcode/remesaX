using Microsoft.AspNetCore.Mvc;
using RemesaX.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace RemesaX.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IStellarService _stellarService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IStellarService stellarService, ILogger<WalletController> logger)
    {
        _stellarService = stellarService;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve el saldo on-chain de una cuenta Stellar
    /// </summary>
    [HttpGet("{address}/balance")]
    public async Task<IActionResult> GetBalance(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length != 56 || !address.StartsWith("G"))
        {
            return BadRequest(new { error = "Direccion invalida. Debe ser una public key Stellar valida (56 chars, empezando con G)." });
        }

        try
        {
            var balances = await _stellarService.GetBalancesAsync(address);
            return Ok(balances);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo obtener saldo de {Address}", address);
            return NotFound(new { error = $"Cuenta no encontrada o error de red: {ex.Message}" });
        }
    }

    /// <summary>
    /// Crea una nueva cuenta Stellar de testnet financiada por friendbot.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount()
    {
        try
        {
            var account = await _stellarService.CreateTestnetAccountAsync();
            return Ok(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando cuenta");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crea una trustline desde una cuenta hacia el asset USDX del issuer.
    /// PASO PREVIO necesario antes de recibir USDX por primera vez.
    /// </summary>
    [HttpPost("trustline")]
    public async Task<IActionResult> CreateTrustline([FromBody] CreateTrustlineRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Body de la peticion vacio." });
        }

        if (string.IsNullOrWhiteSpace(request.AccountSecretKey))
        {
            return BadRequest(new { error = "El campo 'accountSecretKey' es requerido." });
        }

        var secretKey = request.AccountSecretKey.Trim();

        if (secretKey.Length != 56)
        {
            return BadRequest(new
            {
                error = $"'accountSecretKey' tiene longitud invalida: {secretKey.Length} caracteres. " +
                        "Una secret key Stellar tiene exactamente 56 caracteres y empieza con 'S'.",
                hint = "Pegaste la secretKey completa que devolvio POST /api/Wallet/create?"
            });
        }

        if (!secretKey.StartsWith("S"))
        {
            return BadRequest(new
            {
                error = "'accountSecretKey' debe empezar con 'S' (es la llave privada, no la public key).",
                hint = "Las public keys empiezan con 'G' y las secret keys con 'S'. Aqui necesitamos la secret."
            });
        }

        if (string.IsNullOrWhiteSpace(request.AssetCode))
        {
            request.AssetCode = "USDX";
        }

        try
        {
            var hash = await _stellarService.CreateTrustlineAsync(secretKey, request.AssetCode);
            return Ok(new
            {
                transactionHash = hash,
                explorerUrl = $"https://stellar.expert/explorer/testnet/tx/{hash}",
                message = $"Trustline a {request.AssetCode} creada. La cuenta ya puede recibir este asset."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando trustline");
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class CreateTrustlineRequest
{
    /// <summary>Secret key de la cuenta que crea la trustline (56 chars, empieza con S)</summary>
    [Required]
    public string AccountSecretKey { get; set; } = string.Empty;

    /// <summary>Codigo del asset (default: USDX)</summary>
    public string AssetCode { get; set; } = "USDX";
}