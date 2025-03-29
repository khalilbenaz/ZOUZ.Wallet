using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;

namespace ZOUZ.Wallet.Core.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, TransactionType? type = null, int pageNumber = 1, int pageSize = 10);
    Task<int> CountTransactionsAsync(Guid walletId, DateTime? startDate = null, DateTime? endDate = null, TransactionType? type = null);
    Task<IEnumerable<Transaction>> GetSuccessfulTransactionsAsync(Guid walletId, DateTime startDate, DateTime endDate);
    Task<decimal> GetTotalTransactionAmountAsync(Guid walletId, TransactionType type, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Transaction>> GetSuspiciousTransactionsAsync(DateTime startDate, DateTime endDate);
    Task AddAsync(Transaction transaction);
    Task AddBillAsync(Bill bill);
    Task UpdateAsync(Transaction transaction);
    Task UpdateBillAsync(Bill bill);
    Task SaveChangesAsync(); 
}