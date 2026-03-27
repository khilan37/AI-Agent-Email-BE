using AI.Agent.Email.Core.Enums;

namespace AI.Agent.Email.Core.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? EmailId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public global::AI.Agent.Email.Core.Enums.TaskStatus Status { get; set; } = global::AI.Agent.Email.Core.Enums.TaskStatus.Pending;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Email? Email { get; set; }
}
