using AI.Agent.Email.Core.Entities;

namespace AI.Agent.Email.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<UserSettings?> GetSettingsAsync(int userId);
    Task UpdateSettingsAsync(UserSettings settings);
}
