using Microsoft.EntityFrameworkCore;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Infrastructure.Data;

namespace ZOUZ.Wallet.Infrastructure.Repositories;

public class OfferRepository : IOfferRepository
    {
        private readonly WalletDbContext _context;

        public OfferRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<Offer> GetByIdAsync(Guid id)
        {
            return await _context.Offers.FindAsync(id);
        }

        public async Task<IEnumerable<Offer>> GetAllAsync()
        {
            return await _context.Offers.ToListAsync();
        }

        public async Task<IEnumerable<Offer>> GetActiveOffersAsync()
        {
            return await _context.Offers
                .Where(o => o.IsActive && o.ValidTo >= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<IEnumerable<Offer>> GetOffersByTypeAsync(OfferType type)
        {
            return await _context.Offers
                .Where(o => o.Type == type)
                .ToListAsync();
        }

        public async Task<IEnumerable<Offer>> GetOffersAsync(
            bool activeOnly = false, 
            OfferType? type = null, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            IQueryable<Offer> query = _context.Offers;

            // Filtrer par statut actif
            if (activeOnly)
            {
                query = query.Where(o => o.IsActive && o.ValidTo >= DateTime.UtcNow);
            }

            // Filtrer par type
            if (type.HasValue)
            {
                query = query.Where(o => o.Type == type.Value);
            }

            // Trier par date de création (plus récent d'abord)
            query = query.OrderByDescending(o => o.CreatedAt);

            // Pagination
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public async Task<int> CountOffersAsync(bool activeOnly = false, OfferType? type = null)
        {
            IQueryable<Offer> query = _context.Offers;

            // Filtrer par statut actif
            if (activeOnly)
            {
                query = query.Where(o => o.IsActive && o.ValidTo >= DateTime.UtcNow);
            }

            // Filtrer par type
            if (type.HasValue)
            {
                query = query.Where(o => o.Type == type.Value);
            }

            return await query.CountAsync();
        }

        public async Task<IEnumerable<Core.Entities.Wallet>> GetWalletsByOfferIdAsync(Guid offerId, int pageNumber = 1, int pageSize = 10)
        {
            return await _context.Wallets
                .Where(w => w.OfferId == offerId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountWalletsByOfferIdAsync(Guid offerId)
        {
            return await _context.Wallets
                .Where(w => w.OfferId == offerId)
                .CountAsync();
        }

        public async Task AddAsync(Offer offer)
        {
            await _context.Offers.AddAsync(offer);
        }

        public async Task UpdateAsync(Offer offer)
        {
            _context.Entry(offer).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Guid id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer != null)
            {
                _context.Offers.Remove(offer);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }