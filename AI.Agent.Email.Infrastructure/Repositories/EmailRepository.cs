using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI.Agent.Email.Infrastructure.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly AppDbContext _context;

    public EmailRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<global::AI.Agent.Email.Core.Entities.Email?> GetByIdAsync(int id)
    {
        return await _context.Emails
            .Include(e => e.Tasks)
            .Include(e => e.AgentLogs)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<global::AI.Agent.Email.Core.Entities.Email?> GetByGmailMessageIdAsync(string gmailMessageId)
    {
        return await _context.Emails
            .FirstOrDefaultAsync(e => e.GmailMessageId == gmailMessageId);
    }

    public async Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20)
    {
        return await _context.Emails
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ReceivedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetUnreadByUserIdAsync(int userId)
    {
        return await _context.Emails
            .Where(e => e.UserId == userId && e.Status == EmailStatus.Unread)
            .OrderByDescending(e => e.ReceivedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetUnprocessedAsync(int limit = 10)
    {
        return await _context.Emails
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.ReceivedDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountByUserIdAsync(int userId)
    {
        return await _context.Emails
            .CountAsync(e => e.UserId == userId);
    }

    public async Task<int> GetUnreadCountByUserIdAsync(int userId)
    {
        return await _context.Emails
            .CountAsync(e => e.UserId == userId && e.Status == EmailStatus.Unread);
    }

    public async Task<int> GetUrgentCountByUserIdAsync(int userId)
    {
        return await _context.Emails
            .CountAsync(e => e.UserId == userId && e.Category == EmailCategory.Urgent);
    }

    public async Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetRecentByUserIdAsync(int userId, int count = 10)
    {
        return await _context.Emails
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.ReceivedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddAsync(global::AI.Agent.Email.Core.Entities.Email email)
    {
        await _context.Emails.AddAsync(email);
    }

    public Task UpdateAsync(global::AI.Agent.Email.Core.Entities.Email email)
    {
        email.UpdatedAt = DateTime.UtcNow;
        _context.Emails.Update(email);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var email = await _context.Emails.FindAsync(id);
        if (email != null)
        {
            _context.Emails.Remove(email);
        }
    }

    public async Task<bool> ExistsAsync(string gmailMessageId)
    {
        return await _context.Emails.AnyAsync(e => e.GmailMessageId == gmailMessageId);
    }
}
