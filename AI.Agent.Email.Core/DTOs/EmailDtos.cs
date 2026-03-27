using AI.Agent.Email.Core.Enums;

namespace AI.Agent.Email.Core.DTOs;

public class EmailDto
{
    public int Id { get; set; }
    public string GmailMessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyPlainText { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string? Category { get; set; }
    public string? Intent { get; set; }
    public string? Priority { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? SuggestedAction { get; set; }
    public string? GeneratedReply { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAutoReplied { get; set; }
    public DateTime? ReplySentAt { get; set; }
    public DateTime? MeetingDate { get; set; }
    public string? TaskTitle { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmailListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Search { get; set; }
}

public class EmailListResponse
{
    public List<EmailDto> Emails { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class EmailDetailResponse : EmailDto
{
    public List<TaskItemDto> Tasks { get; set; } = new();
    public List<AgentLogDto> AgentLogs { get; set; } = new();
}

public class SendReplyRequest
{
    public string ReplyBody { get; set; } = string.Empty;
}

public class UpdateEmailStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class SyncEmailsResponse
{
    public int SyncedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
