using ZOUZ.Wallet.Core.Entities;

namespace ZOUZ.Wallet.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task<User> GetByUsernameAsync(string username);
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
    Task<IEnumerable<User>> GetUsersAsync(string searchTerm = null, string role = null, int pageNumber = 1, int pageSize = 10);
    Task<int> CountUsersAsync(string searchTerm = null, string role = null);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}