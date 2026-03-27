using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AI.Agent.Email.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.Email)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<TaskItem>> GetByUserIdAsync(int userId, global::AI.Agent.Email.Core.Enums.TaskStatus? status = null)
    {
        var query = _context.Tasks.Where(t => t.UserId == userId);
        
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }
        
        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByEmailIdAsync(int emailId)
    {
        return await _context.Tasks
            .Where(t => t.EmailId == emailId)
            .ToListAsync();
    }

    public async Task<int> GetPendingCountByUserIdAsync(int userId)
    {
        return await _context.Tasks
            .CountAsync(t => t.UserId == userId && 
                (t.Status == global::AI.Agent.Email.Core.Enums.TaskStatus.Pending || t.Status == global::AI.Agent.Email.Core.Enums.TaskStatus.InProgress));
    }

    public async Task<int> GetTotalCountByUserIdAsync(int userId)
    {
        return await _context.Tasks.CountAsync(t => t.UserId == userId);
    }

    public async Task AddAsync(TaskItem task)
    {
        await _context.Tasks.AddAsync(task);
    }

    public Task UpdateAsync(TaskItem task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        if (task.Status == global::AI.Agent.Email.Core.Enums.TaskStatus.Completed && !task.CompletedAt.HasValue)
        {
            task.CompletedAt = DateTime.UtcNow;
        }
        _context.Tasks.Update(task);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
        }
    }
}
