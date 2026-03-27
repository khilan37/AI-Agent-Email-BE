namespace AI.Agent.Email.Core.DTOs;

public class DashboardStatsResponse
{
    public int TotalEmails { get; set; }
    public int UnreadEmails { get; set; }
    public int UrgentEmails { get; set; }
    public int PendingTasks { get; set; }
    public int TotalTasks { get; set; }
    public List<RecentEmailDto> RecentEmails { get; set; } = new();
    public List<RecentTaskDto> RecentTasks { get; set; } = new();
    public List<ActivityDto> RecentActivity { get; set; } = new();
}

public class RecentEmailDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime ReceivedDate { get; set; }
}

public class RecentTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class ActivityDto
{
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
