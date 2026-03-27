using AI.Agent.Email.Core.Entities;

namespace AI.Agent.Email.Core.Interfaces;

public interface IEmailAccountRepository
{
    Task<EmailAccount?> GetByIdAsync(int id);
    Task<EmailAccount?> GetByUserIdAndProviderAsync(int userId, string provider);
    Task<IEnumerable<EmailAccount>> GetActiveByUserIdAsync(int userId);
    Task AddAsync(EmailAccount account);
    Task UpdateAsync(EmailAccount account);
    Task DeleteAsync(int id);
}
