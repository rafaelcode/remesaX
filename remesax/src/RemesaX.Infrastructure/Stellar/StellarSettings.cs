namespace RemesaX.Infrastructure.Stellar;

public class StellarSettings
{
    public string Network { get; set; } = "Testnet";
    public string HorizonUrl { get; set; } = "https://horizon-testnet.stellar.org";
    public string IssuerSecretKey { get; set; } = string.Empty;
    public string IssuerPublicKey { get; set; } = string.Empty;
    public string AssetCode { get; set; } = "USDX";
}
