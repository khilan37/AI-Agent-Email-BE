using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TaskController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] string? status = null)
    {
        TaskStatus? taskStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatus>(status, out var parsedStatus))
        {
            taskStatus = parsedStatus;
        }

        var tasks = await _unitOfWork.Tasks.GetByUserIdAsync(CurrentUserId, taskStatus);
        
        var dtos = tasks.Select(t => new TaskItemDto
        {
            Id = t.Id,
            EmailId = t.EmailId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            DueDate = t.DueDate,
            CreatedAt = t.CreatedAt,
            CompletedAt = t.CompletedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTask(int id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        
        if (task == null || task.UserId != CurrentUserId)
            return NotFound();

        var dto = new TaskItemDto
        {
            Id = task.Id,
            EmailId = task.EmailId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var task = new Core.Entities.TaskItem
        {
            UserId = CurrentUserId,
            EmailId = request.EmailId,
            Title = request.Title,
            Description = request.Description,
            Status = TaskStatus.Pending,
            DueDate = request.DueDate
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        var dto = new TaskItemDto
        {
            Id = task.Id,
            EmailId = task.EmailId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        
        if (task == null || task.UserId != CurrentUserId)
            return NotFound();

        task.Title = request.Title;
        task.Description = request.Description;
        task.DueDate = request.DueDate;
        
        if (Enum.TryParse<TaskStatus>(request.Status, out var status))
        {
            task.Status = status;
        }

        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Task updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        
        if (task == null || task.UserId != CurrentUserId)
            return NotFound();

        await _unitOfWork.Tasks.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTask(int id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        
        if (task == null || task.UserId != CurrentUserId)
            return NotFound();

        task.Status = TaskStatus.Completed;
        await _unitOfWork.Tasks.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Task marked as completed" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetTaskStats()
    {
        var total = await _unitOfWork.Tasks.GetTotalCountByUserIdAsync(CurrentUserId);
        var pending = await _unitOfWork.Tasks.GetPendingCountByUserIdAsync(CurrentUserId);

        return Ok(new { Total = total, Pending = pending, Completed = total - pending });
    }
}
