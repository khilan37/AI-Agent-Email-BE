namespace AI.Agent.Email.Core.DTOs;

public class AgentLogDto
{
    public int Id { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public string? ActionDetails { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double? ProcessingTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ProcessEmailRequest
{
    public int EmailId { get; set; }
}

public class ProcessEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Intent { get; set; }
    public string? SuggestedAction { get; set; }
}

public class GenerateReplyRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Tone { get; set; } = "professional";
}

public class GenerateReplyResponse
{
    public string Reply { get; set; } = string.Empty;
}

public class AIInsightsResponse
{
    public int EmailId { get; set; }
    public string? Category { get; set; }
    public string? Intent { get; set; }
    public string? Priority { get; set; }
    public double? ConfidenceScore { get; set; }
    public string? SuggestedAction { get; set; }
    public string? GeneratedReply { get; set; }
    public ExtractedDataDto? ExtractedData { get; set; }
}

public class ExtractedDataDto
{
    public DateTime? MeetingDate { get; set; }
    public string? TaskTitle { get; set; }
    public DateTime? FollowUpDate { get; set; }
}
