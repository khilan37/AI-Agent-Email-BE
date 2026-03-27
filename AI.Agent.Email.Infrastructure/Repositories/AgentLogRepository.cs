using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI.Agent.Email.Infrastructure.Repositories;

public class AgentLogRepository : IAgentLogRepository
{
    private readonly AppDbContext _context;

    public AgentLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AgentLog?> GetByIdAsync(int id)
    {
        return await _context.AgentLogs.FindAsync(id);
    }

    public async Task<IEnumerable<AgentLog>> GetByEmailIdAsync(int emailId)
    {
        return await _context.AgentLogs
            .Where(l => l.EmailId == emailId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AgentLog>> GetRecentAsync(int count = 50)
    {
        return await _context.AgentLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task AddAsync(AgentLog log)
    {
        await _context.AgentLogs.AddAsync(log);
    }

    public async Task<int> GetSuccessCountAsync()
    {
        return await _context.AgentLogs.CountAsync(l => l.Success);
    }

    public async Task<int> GetFailureCountAsync()
    {
        return await _context.AgentLogs.CountAsync(l => !l.Success);
    }
}
