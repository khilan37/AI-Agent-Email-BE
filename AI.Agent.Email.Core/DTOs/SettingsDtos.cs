namespace AI.Agent.Email.Core.DTOs;

public class UserSettingsDto
{
    public bool AutoReplyEnabled { get; set; }
    public int DefaultReplyDelayMinutes { get; set; }
    public int EmailCheckIntervalMinutes { get; set; }
    public bool RequireApprovalForReplies { get; set; }
    public double MinConfidenceThreshold { get; set; }
    public string? DefaultReplyLanguage { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
}

public class UpdateSettingsRequest
{
    public bool AutoReplyEnabled { get; set; }
    public int DefaultReplyDelayMinutes { get; set; }
    public int EmailCheckIntervalMinutes { get; set; }
    public bool RequireApprovalForReplies { get; set; }
    public double MinConfidenceThreshold { get; set; }
    public string? DefaultReplyLanguage { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
}

public class EmailAccountDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConnectGmailRequest
{
    public string AuthCode { get; set; } = string.Empty;
}

public class DisconnectAccountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
