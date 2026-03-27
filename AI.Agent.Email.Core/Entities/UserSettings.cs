namespace AI.Agent.Email.Core.Entities;

public class UserSettings
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool AutoReplyEnabled { get; set; } = false;
    public int DefaultReplyDelayMinutes { get; set; } = 5;
    public int EmailCheckIntervalMinutes { get; set; } = 1;
    public bool RequireApprovalForReplies { get; set; } = true;
    public double MinConfidenceThreshold { get; set; } = 0.7;
    public string? DefaultReplyLanguage { get; set; } = "en";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool InAppNotificationsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual User User { get; set; } = null!;
}
