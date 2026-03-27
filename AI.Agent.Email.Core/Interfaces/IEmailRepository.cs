using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;

namespace AI.Agent.Email.Core.Interfaces;

public interface IEmailRepository
{
    Task<global::AI.Agent.Email.Core.Entities.Email?> GetByIdAsync(int id);
    Task<global::AI.Agent.Email.Core.Entities.Email?> GetByGmailMessageIdAsync(string gmailMessageId);
    Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetUnreadByUserIdAsync(int userId);
    Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetUnprocessedAsync(int limit = 10);
    Task<int> GetTotalCountByUserIdAsync(int userId);
    Task<int> GetUnreadCountByUserIdAsync(int userId);
    Task<int> GetUrgentCountByUserIdAsync(int userId);
    Task<IEnumerable<global::AI.Agent.Email.Core.Entities.Email>> GetRecentByUserIdAsync(int userId, int count = 10);
    Task AddAsync(global::AI.Agent.Email.Core.Entities.Email email);
    Task UpdateAsync(global::AI.Agent.Email.Core.Entities.Email email);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string gmailMessageId);
}
