using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemesaX.Core.Dtos;
using RemesaX.Core.Interfaces;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Assets;
using StellarDotnetSdk.Memos;
using StellarDotnetSdk.Operations;
using StellarDotnetSdk.Transactions;
using System.Text.Json;

namespace RemesaX.Infrastructure.Stellar;

/// <summary>
/// Adaptador que encapsula toda la interacción con la red Stellar.
/// Compatible con stellar-dotnet-sdk v14+.
/// </summary>
public class StellarService : IStellarService, IDisposable
{
    private readonly StellarSettings _settings;
    private readonly ILogger<StellarService> _logger;
    private readonly Server _server;
    private readonly HttpClient _httpClient = new();

    public StellarService(IOptions<StellarSettings> options, ILogger<StellarService> logger)
    {
        _settings = options.Value;
        _logger = logger;

        if (_settings.Network.Equals("Testnet", StringComparison.OrdinalIgnoreCase))
        {
            Network.UseTestNetwork();
        }
        else
        {
            Network.UsePublicNetwork();
        }

        _server = new Server(_settings.HorizonUrl);
    }

    /// <summary>
    /// Crea una cuenta de testnet financiada por el friendbot de Stellar.
    /// </summary>
    public async Task<StellarAccountInfo> CreateTestnetAccountAsync()
    {
        var keyPair = KeyPair.Random();
        _logger.LogInformation("Generada nueva cuenta: {PublicKey}", keyPair.AccountId);

        var friendbotUrl = $"https://friendbot.stellar.org?addr={keyPair.AccountId}";
        var response = await _httpClient.GetAsync(friendbotUrl);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Friendbot no pudo financiar la cuenta: {response.StatusCode}. Detalle: {body}");
        }

        await Task.Delay(2000);

        var balances = await GetBalancesAsync(keyPair.AccountId);

        return new StellarAccountInfo
        {
            PublicKey = keyPair.AccountId,
            SecretKey = keyPair.SecretSeed,
            Balances = balances
        };
    }

    /// <summary>
    /// Consulta saldos de una cuenta Stellar via Horizon.
    /// </summary>
    public async Task<List<BalanceInfo>> GetBalancesAsync(string publicKey)
    {
        var account = await _server.Accounts.Account(publicKey);

        return account.Balances
            .Select(b => new BalanceInfo
            {
                AssetCode = b.AssetType == "native" ? "XLM" : (b.AssetCode ?? "?"),
                Balance = b.BalanceString
            })
            .ToList();
    }

    /// <summary>
    /// Crea una trustline desde una cuenta hacia el asset USDX emitido por el issuer.
    /// La cuenta firma con su propia secretKey - es declaración voluntaria de aceptar el asset.
    /// </summary>
    public async Task<string> CreateTrustlineAsync(string accountSecretKey, string assetCode, string limit = "1000000")
    {
        var accountKeyPair = KeyPair.FromSecretSeed(accountSecretKey);
        var account = await _server.Accounts.Account(accountKeyPair.AccountId);

        var asset = Asset.CreateNonNativeAsset(assetCode, _settings.IssuerPublicKey);

        // En v14: ChangeTrustOperation recibe Asset directamente
        var changeTrustOp = new ChangeTrustOperation(asset: asset, limit: limit);

        var transaction = new TransactionBuilder(account)
            .AddOperation(changeTrustOp)
            .AddMemo(Memo.Text("RemesaX trustline"))
            .Build();

        transaction.Sign(accountKeyPair);

        _logger.LogInformation("Creando trustline {AssetCode} para {Account}",
            assetCode, accountKeyPair.AccountId);

        try
        {
            var result = await _server.SubmitTransaction(transaction);
            if (result is { IsSuccess: true })
            {
                _logger.LogInformation("Trustline creada: {Hash}", result.Hash);
                return result.Hash;
            }
            throw new InvalidOperationException($"Trustline rechazada: {JsonSerializer.Serialize(result)}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error creando trustline");
            throw new InvalidOperationException($"Trustline fallida: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Envía un pago de USDX desde la cuenta emisora hacia la cuenta destino.
    /// La cuenta destino debe tener trustline al asset USDX emitido por nosotros.
    /// </summary>
    public async Task<string> SendPaymentAsync(string destinationAddress, decimal amount, string assetCode)
    {
        var sourceKeyPair = KeyPair.FromSecretSeed(_settings.IssuerSecretKey);
        var destinationKeyPair = KeyPair.FromAccountId(destinationAddress);

        var sourceAccount = await _server.Accounts.Account(sourceKeyPair.AccountId);

        var asset = Asset.CreateNonNativeAsset(assetCode, _settings.IssuerPublicKey);

        // En v14: PaymentOperation se instancia directo, sin Builder y sin set SourceAccount
        // El sourceAccount viene del TransactionBuilder (firmará la transacción quien sea sourceKeyPair)
        var paymentOperation = new PaymentOperation(
            destination: destinationKeyPair,
            asset: asset,
            amount: amount.ToString("F7", System.Globalization.CultureInfo.InvariantCulture));

        var transaction = new TransactionBuilder(sourceAccount)
            .AddOperation(paymentOperation)
            .AddMemo(Memo.Text("RemesaX MVP"))
            .Build();

        transaction.Sign(sourceKeyPair);

        _logger.LogInformation("Enviando {Amount} {Asset} a {Destination}",
            amount, assetCode, destinationAddress);

        try
        {
            var result = await _server.SubmitTransaction(transaction);

            if (result is { IsSuccess: true })
            {
                _logger.LogInformation("Transacción exitosa: {Hash}", result.Hash);
                return result.Hash;
            }

            var rawResponse = JsonSerializer.Serialize(result);
            _logger.LogWarning("Transacción rechazada por la red: {Response}", rawResponse);
            throw new InvalidOperationException($"Pago rechazado. Respuesta: {rawResponse}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Excepción al enviar pago a Stellar");
            throw new InvalidOperationException($"Pago fallido en Stellar: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _server?.Dispose();
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}