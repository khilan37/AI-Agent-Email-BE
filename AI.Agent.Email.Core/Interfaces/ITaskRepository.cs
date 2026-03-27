using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;

namespace AI.Agent.Email.Core.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id);
    Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId, global::AI.Agent.Email.Core.Enums.TaskStatus? status = null);
    Task<IEnumerable<TaskItem>> GetByEmailIdAsync(int emailId);
    Task<int> GetPendingCountByUserIdAsync(int userId);
    Task<int> GetTotalCountByUserIdAsync(int userId);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(int id);
}
