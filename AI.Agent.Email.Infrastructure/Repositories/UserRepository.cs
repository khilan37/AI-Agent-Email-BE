using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI.Agent.Email.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<UserSettings?> GetSettingsAsync(int userId)
    {
        return await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public Task UpdateSettingsAsync(UserSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.UserSettings.Update(settings);
        return Task.CompletedTask;
    }
}
