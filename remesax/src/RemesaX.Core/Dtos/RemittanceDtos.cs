using System.ComponentModel.DataAnnotations;

namespace RemesaX.Core.Dtos;

public class CreateRemittanceRequest
{
    [Required]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [MinLength(56), MaxLength(56)]
    public string DestinationAddress { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000)]
    public decimal Amount { get; set; }

    public string TargetCurrency { get; set; } = "MXN";
}

public class RemittanceResponse
{
    public Guid Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public decimal AmountUsdx { get; set; }
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal AmountLocal { get; set; }
    public string? TransactionHash { get; set; }
    public string? ExplorerUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class StellarAccountInfo
{
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public List<BalanceInfo> Balances { get; set; } = new();
}

public class BalanceInfo
{
    public string AssetCode { get; set; } = string.Empty;
    public string Balance { get; set; } = string.Empty;
}
