using Microsoft.EntityFrameworkCore;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Infrastructure.Data;

namespace ZOUZ.Wallet.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
    {
        private readonly WalletDbContext _context;

        public WalletRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Core.Entities.Wallet> GetByIdAsync(Guid id)
        {
            return await _context.Wallets
                .Include(w => w.Offer)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<Core.Entities.Wallet>> GetAllAsync()
        {
            return await _context.Wallets
                .Include(w => w.Offer)
                .ToListAsync();
        }

        public async Task<IEnumerable<Core.Entities.Wallet>> GetWalletsAsync(
            string ownerId = null,
            string ownerName = null,
            Guid? offerId = null,
            decimal? minBalance = null,
            decimal? maxBalance = null,
            WalletStatus? status = null,
            KycLevel? kycLevel = null,
            int pageNumber = 1,
            int pageSize = 10,
            bool includeOffers = false)
        {
            IQueryable<Core.Entities.Wallet> query = _context.Wallets;

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(ownerId))
            {
                query = query.Where(w => w.OwnerId == ownerId);
            }

            if (!string.IsNullOrEmpty(ownerName))
            {
                query = query.Where(w => w.OwnerName.Contains(ownerName));
            }

            if (offerId.HasValue)
            {
                query = query.Where(w => w.OfferId == offerId);
            }

            if (minBalance.HasValue)
            {
                query = query.Where(w => w.Balance >= minBalance.Value);
            }

            if (maxBalance.HasValue)
            {
                query = query.Where(w => w.Balance <= maxBalance.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            if (kycLevel.HasValue)
            {
                query = query.Where(w => w.KycLevel == kycLevel.Value);
            }

            // Inclure les offres si demandé
            if (includeOffers)
            {
                query = query.Include(w => w.Offer);
            }

            // Pagination
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public async Task<int> CountWalletsAsync(
            string ownerId = null,
            string ownerName = null,
            Guid? offerId = null,
            decimal? minBalance = null,
            decimal? maxBalance = null,
            WalletStatus? status = null,
            KycLevel? kycLevel = null)
        {
            IQueryable<Core.Entities.Wallet> query = _context.Wallets;

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(ownerId))
            {
                query = query.Where(w => w.OwnerId == ownerId);
            }

            if (!string.IsNullOrEmpty(ownerName))
            {
                query = query.Where(w => w.OwnerName.Contains(ownerName));
            }

            if (offerId.HasValue)
            {
                query = query.Where(w => w.OfferId == offerId);
            }

            if (minBalance.HasValue)
            {
                query = query.Where(w => w.Balance >= minBalance.Value);
            }

            if (maxBalance.HasValue)
            {
                query = query.Where(w => w.Balance <= maxBalance.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            if (kycLevel.HasValue)
            {
                query = query.Where(w => w.KycLevel == kycLevel.Value);
            }

            return await query.CountAsync();
        }

        public async Task<DateTime?> GetLastTransactionDateAsync(Guid walletId)
        {
            var latestTransaction = await _context.Transactions
                .Where(t => t.WalletId == walletId && t.IsSuccessful)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            return latestTransaction?.CreatedAt;
        }

        public async Task<bool> HasActiveTransactionsAsync(Guid walletId)
        {
            // Vérifier s'il y a des transactions actives dans les derniers 30 jours
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            return await _context.Transactions
                .AnyAsync(t => t.WalletId == walletId && t.CreatedAt >= thirtyDaysAgo);
        }

        public async Task AddAsync(Core.Entities.Wallet wallet)
        {
            await _context.Wallets.AddAsync(wallet);
        }

        public async Task UpdateAsync(Core.Entities.Wallet wallet)
        {
            // Pas besoin d'appeler Update si l'entité est déjà suivie
            _context.Entry(wallet).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Guid id)
        {
            var wallet = await _context.Wallets.FindAsync(id);
            if (wallet != null)
            {
                _context.Wallets.Remove(wallet);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }