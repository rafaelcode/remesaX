using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RemesaX.Core.Dtos;
using RemesaX.Core.Entities;
using RemesaX.Core.Interfaces;
using RemesaX.Infrastructure.Persistence;

namespace RemesaX.Infrastructure.Stellar;

public class RemittanceService : IRemittanceService
{
    private readonly IStellarService _stellar;
    private readonly RemesaXDbContext _db;
    private readonly ILogger<RemittanceService> _logger;

    // Tasas de cambio mock - en producción vendría de un proveedor real
    private static readonly Dictionary<string, decimal> MockRates = new()
    {
        { "MXN", 17.85m },
        { "ARS", 985.50m },
        { "COP", 4150.00m },
        { "PEN", 3.78m },
        { "BRL", 5.05m }
    };

    public RemittanceService(
        IStellarService stellar,
        RemesaXDbContext db,
        ILogger<RemittanceService> logger)
    {
        _stellar = stellar;
        _db = db;
        _logger = logger;
    }

    public async Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request)
    {
        // Validación de moneda destino
        if (!MockRates.TryGetValue(request.TargetCurrency.ToUpper(), out var rate))
        {
            throw new InvalidOperationException(
                $"Moneda no soportada: {request.TargetCurrency}. " +
                $"Soportadas: {string.Join(", ", MockRates.Keys)}");
        }

        // Crear registro en estado Pending
        var remittance = new Remittance
        {
            SenderName = request.SenderName,
            DestinationAddress = request.DestinationAddress,
            AmountUsdx = request.Amount,
            TargetCurrency = request.TargetCurrency.ToUpper(),
            ExchangeRate = rate,
            Status = RemittanceStatus.Processing
        };

        _db.Remittances.Add(remittance);
        await _db.SaveChangesAsync();

        try
        {
            // Ejecutar pago en Stellar
            var txHash = await _stellar.SendPaymentAsync(
                request.DestinationAddress,
                request.Amount,
                "USDX");

            remittance.TransactionHash = txHash;
            remittance.Status = RemittanceStatus.Completed;
            remittance.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando remesa {Id}", remittance.Id);
            remittance.Status = RemittanceStatus.Failed;
            remittance.ErrorMessage = ex.Message;
        }

        await _db.SaveChangesAsync();
        return MapToResponse(remittance);
    }

    public async Task<RemittanceResponse?> GetByIdAsync(Guid id)
    {
        var remittance = await _db.Remittances.FindAsync(id);
        return remittance is null ? null : MapToResponse(remittance);
    }

    public async Task<List<RemittanceResponse>> GetRecentAsync(int take)
    {
        var recent = await _db.Remittances
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync();

        return recent.Select(MapToResponse).ToList();
    }

    private static RemittanceResponse MapToResponse(Remittance r) => new()
    {
        Id = r.Id,
        SenderName = r.SenderName,
        DestinationAddress = r.DestinationAddress,
        AmountUsdx = r.AmountUsdx,
        TargetCurrency = r.TargetCurrency,
        ExchangeRate = r.ExchangeRate,
        AmountLocal = r.AmountLocal,
        TransactionHash = r.TransactionHash,
        ExplorerUrl = r.TransactionHash is null 
            ? null 
            : $"https://stellar.expert/explorer/testnet/tx/{r.TransactionHash}",
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        CompletedAt = r.CompletedAt
    };
}
