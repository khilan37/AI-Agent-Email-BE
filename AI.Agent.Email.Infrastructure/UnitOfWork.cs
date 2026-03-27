using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using AI.Agent.Email.Infrastructure.Repositories;

namespace AI.Agent.Email.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IEmailRepository Emails { get; }
    public ITaskRepository Tasks { get; }
    public IAgentLogRepository AgentLogs { get; }
    public IUserRepository Users { get; }
    public IEmailAccountRepository EmailAccounts { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Emails = new EmailRepository(context);
        Tasks = new TaskRepository(context);
        AgentLogs = new AgentLogRepository(context);
        Users = new UserRepository(context);
        EmailAccounts = new EmailAccountRepository(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
