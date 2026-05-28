namespace RemesaX.Core.Entities;

public class Remittance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SenderName { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public decimal AmountUsdx { get; set; }
    public string TargetCurrency { get; set; } = "MXN";
    public decimal ExchangeRate { get; set; }
    public decimal AmountLocal => AmountUsdx * ExchangeRate;
    public string? TransactionHash { get; set; }
    public RemittanceStatus Status { get; set; } = RemittanceStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum RemittanceStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
