using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionResponse> DepositAsync(Guid walletId, DepositRequest request, string userId);
    Task<TransactionResponse> WithdrawAsync(Guid walletId, WithdrawalRequest request, string userId);
    Task<TransactionResponse> TransferAsync(TransferRequest request, string userId);
    Task<TransactionResponse> PayBillAsync(Guid walletId, PayBillRequest request, string userId);
    Task<PagedResponse<TransactionResponse>> GetWalletTransactionsAsync(
        Guid walletId, 
        DateTime? startDate, 
        DateTime? endDate, 
        TransactionType? type,
        int pageNumber, 
        int pageSize, 
        string userId);
}