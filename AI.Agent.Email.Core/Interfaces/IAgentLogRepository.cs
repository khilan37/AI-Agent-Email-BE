using AI.Agent.Email.Core.Entities;

namespace AI.Agent.Email.Core.Interfaces;

public interface IAgentLogRepository
{
    Task<AgentLog?> GetByIdAsync(int id);
    Task<IEnumerable<AgentLog>> GetByEmailIdAsync(int emailId);
    Task<IEnumerable<AgentLog>> GetRecentAsync(int count = 50);
    Task AddAsync(AgentLog log);
    Task<int> GetSuccessCountAsync();
    Task<int> GetFailureCountAsync();
}
