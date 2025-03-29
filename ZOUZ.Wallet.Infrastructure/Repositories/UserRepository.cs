using Microsoft.EntityFrameworkCore;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Infrastructure.Data;

namespace ZOUZ.Wallet.Infrastructure.Repositories;

 public class UserRepository : IUserRepository
    {
        private readonly WalletDbContext _context;

        public UserRepository(WalletDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users
                .Include(u => u.Wallets)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Wallets)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersAsync(
            string searchTerm = null, 
            string role = null, 
            int pageNumber = 1, 
            int pageSize = 10)
        {
            IQueryable<User> query = _context.Users;

            // Filtrer par terme de recherche
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => 
                    u.Username.Contains(searchTerm) || 
                    u.Email.Contains(searchTerm) || 
                    u.FullName.Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm));
            }

            // Filtrer par rôle
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // Trier par nom
            query = query.OrderBy(u => u.FullName);

            // Pagination
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            return await query.ToListAsync();
        }

        public async Task<int> CountUsersAsync(string searchTerm = null, string role = null)
        {
            IQueryable<User> query = _context.Users;

            // Filtrer par terme de recherche
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => 
                    u.Username.Contains(searchTerm) || 
                    u.Email.Contains(searchTerm) || 
                    u.FullName.Contains(searchTerm) ||
                    u.PhoneNumber.Contains(searchTerm));
            }

            // Filtrer par rôle
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            return await query.CountAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> PhoneNumberExistsAsync(string phoneNumber)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }