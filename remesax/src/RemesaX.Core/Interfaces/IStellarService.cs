using RemesaX.Core.Dtos;

namespace RemesaX.Core.Interfaces;

public interface IStellarService
{
    Task<StellarAccountInfo> CreateTestnetAccountAsync();
    Task<List<BalanceInfo>> GetBalancesAsync(string publicKey);
    Task<string> SendPaymentAsync(string destinationAddress, decimal amount, string assetCode);
    Task<string> CreateTrustlineAsync(string accountSecretKey, string assetCode, string limit = "1000000");
}

public interface IRemittanceService
{
    Task<RemittanceResponse> CreateRemittanceAsync(CreateRemittanceRequest request);
    Task<RemittanceResponse?> GetByIdAsync(Guid id);
    Task<List<RemittanceResponse>> GetRecentAsync(int take);
}