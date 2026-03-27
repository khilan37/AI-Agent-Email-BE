using AI.Agent.Email.Core.Enums;

namespace AI.Agent.Email.Core.Entities;

public class Email
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EmailAccountId { get; set; }
    public string GmailMessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyPlainText { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public EmailCategory? Category { get; set; }
    public EmailIntent? Intent { get; set; }
    public string? Priority { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? SuggestedAction { get; set; }
    public string? GeneratedReply { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Unread;
    public bool IsAutoReplied { get; set; }
    public DateTime? ReplySentAt { get; set; }
    public DateTime? MeetingDate { get; set; }
    public string? TaskTitle { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual EmailAccount EmailAccount { get; set; } = null!;
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<AgentLog> AgentLogs { get; set; } = new List<AgentLog>();
}
