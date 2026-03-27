namespace AI.Agent.Email.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IEmailRepository Emails { get; }
    ITaskRepository Tasks { get; }
    IAgentLogRepository AgentLogs { get; }
    IUserRepository Users { get; }
    IEmailAccountRepository EmailAccounts { get; }
    Task<int> SaveChangesAsync();
}
