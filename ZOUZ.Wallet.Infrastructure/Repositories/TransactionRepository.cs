using ZOUZ.Wallet.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Infrastructure.Data;

namespace ZOUZ.Wallet.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
    {
        private readonly WalletDbContext _context;

        public TransactionRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.DestinationWallet)
                .Include(t => t.Bill)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType? type = null,
            int pageNumber = 1,
            int pageSize = 10)
        {
            IQueryable<Transaction> query = _context.Transactions
                .Include(t => t.Bill)
                .Where(t => t.WalletId == walletId || t.DestinationWalletId == walletId);

            // Appliquer les filtres
            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            // Trier par date (le plus récent d'abord)
            query = query.OrderByDescending(t => t.CreatedAt);

            // Pagination
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public async Task<int> CountTransactionsAsync(
            Guid walletId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TransactionType? type = null)
        {
            IQueryable<Transaction> query = _context.Transactions
                .Where(t => t.WalletId == walletId || t.DestinationWalletId == walletId);

            // Appliquer les filtres
            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<Transaction>> GetSuccessfulTransactionsAsync(
            Guid walletId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.WalletId == walletId && 
                           t.IsSuccessful && 
                           t.CreatedAt >= startDate && 
                           t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalTransactionAmountAsync(
            Guid walletId,
            TransactionType type,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.WalletId == walletId && 
                           t.Type == type && 
                           t.IsSuccessful && 
                           t.CreatedAt >= startDate && 
                           t.CreatedAt <= endDate)
                .SumAsync(t => t.Amount);
        }

        public async Task<IEnumerable<Transaction>> GetSuspiciousTransactionsAsync(
            DateTime startDate,
            DateTime endDate)
        {
            // Cette méthode pourrait être utilisée par un système de surveillance pour détecter des transactions suspectes
            // Par exemple, identifier les transactions de gros montants ou avec des schémas inhabituels

            var averageAmount = await _context.Transactions
                .Where(t => t.IsSuccessful && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .AverageAsync(t => (double)t.Amount);

            // Considérer comme suspectes les transactions dont le montant est 5 fois supérieur à la moyenne
            var threshold = (decimal)(averageAmount * 5);

            return await _context.Transactions
                .Include(t => t.Wallet)
                .Where(t => t.IsSuccessful && 
                           t.CreatedAt >= startDate && 
                           t.CreatedAt <= endDate && 
                           t.Amount > threshold)
                .OrderByDescending(t => t.Amount)
                .ToListAsync();
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task AddBillAsync(Bill bill)
        {
            await _context.Bills.AddAsync(bill);
        }

        public async Task UpdateAsync(Transaction transaction)
        {
            _context.Entry(transaction).State = EntityState.Modified;
        }

        public async Task UpdateBillAsync(Bill bill)
        {
            _context.Entry(bill).State = EntityState.Modified;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }