using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI.Agent.Email.Infrastructure.Repositories;

public class EmailAccountRepository : IEmailAccountRepository
{
    private readonly AppDbContext _context;

    public EmailAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EmailAccount?> GetByIdAsync(int id)
    {
        return await _context.EmailAccounts.FindAsync(id);
    }

    public async Task<EmailAccount?> GetByUserIdAndProviderAsync(int userId, string provider)
    {
        return await _context.EmailAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Provider == provider);
    }

    public async Task<IEnumerable<EmailAccount>> GetActiveByUserIdAsync(int userId)
    {
        return await _context.EmailAccounts
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync();
    }

    public async Task AddAsync(EmailAccount account)
    {
        await _context.EmailAccounts.AddAsync(account);
    }

    public Task UpdateAsync(EmailAccount account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _context.EmailAccounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _context.EmailAccounts.FindAsync(id);
        if (account != null)
        {
            _context.EmailAccounts.Remove(account);
        }
    }
}
