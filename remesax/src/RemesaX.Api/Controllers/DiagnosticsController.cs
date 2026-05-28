using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RemesaX.Infrastructure.Stellar;

namespace RemesaX.Api.Controllers;

/// <summary>
/// Endpoints de diagnostico para verificar configuracion y estado del sistema.
/// Util en desarrollo y para troubleshooting en produccion.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly StellarSettings _settings;

    public DiagnosticsController(IOptions<StellarSettings> options)
    {
        _settings = options.Value;
    }

    /// <summary>
    /// Muestra la configuracion Stellar cargada (con secret enmascarado).
    /// Util para detectar problemas de configuracion rapidamente.
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            network = _settings.Network,
            horizonUrl = _settings.HorizonUrl,
            assetCode = _settings.AssetCode,
            issuerPublicKey = _settings.IssuerPublicKey,
            issuerPublicKeyLength = _settings.IssuerPublicKey?.Length ?? 0,
            issuerPublicKeyValid = !string.IsNullOrEmpty(_settings.IssuerPublicKey)
                && _settings.IssuerPublicKey.Length == 56
                && _settings.IssuerPublicKey.StartsWith("G"),
            issuerSecretKeyMasked = MaskSecret(_settings.IssuerSecretKey),
            issuerSecretKeyLength = _settings.IssuerSecretKey?.Length ?? 0,
            issuerSecretKeyValid = !string.IsNullOrEmpty(_settings.IssuerSecretKey)
                && _settings.IssuerSecretKey.Length == 56
                && _settings.IssuerSecretKey.StartsWith("S"),
            diagnosticHint = GetHint()
        });
    }

    private string GetHint()
    {
        if (string.IsNullOrWhiteSpace(_settings.IssuerPublicKey))
            return "IssuerPublicKey esta VACIO. Revisa appsettings.Development.json";
        if (_settings.IssuerPublicKey.StartsWith("REEMPLAZAR"))
            return "IssuerPublicKey tiene el placeholder. Reemplazalo con tu G real.";
        if (_settings.IssuerPublicKey.Length != 56)
            return $"IssuerPublicKey tiene {_settings.IssuerPublicKey.Length} chars, debe tener 56.";
        if (!_settings.IssuerPublicKey.StartsWith("G"))
            return "IssuerPublicKey debe empezar con G.";

        if (string.IsNullOrWhiteSpace(_settings.IssuerSecretKey))
            return "IssuerSecretKey esta VACIO. Revisa appsettings.Development.json";
        if (_settings.IssuerSecretKey.StartsWith("REEMPLAZAR"))
            return "IssuerSecretKey tiene el placeholder. Reemplazalo con tu S real.";
        if (_settings.IssuerSecretKey.Length != 56)
            return $"IssuerSecretKey tiene {_settings.IssuerSecretKey.Length} chars, debe tener 56.";
        if (!_settings.IssuerSecretKey.StartsWith("S"))
            return "IssuerSecretKey debe empezar con S.";

        return "Configuracion Stellar parece correcta.";
    }

    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return "(vacio)";
        if (secret.Length < 12) return $"(invalido: {secret})";
        return $"{secret.Substring(0, 4)}...{secret.Substring(secret.Length - 4)}";
    }
}
