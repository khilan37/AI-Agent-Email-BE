namespace AI.Agent.Email.Core.Entities;

public class AgentLog
{
    public int Id { get; set; }
    public int EmailId { get; set; }
    public string ActionTaken { get; set; } = string.Empty;
    public string? ActionDetails { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double? ProcessingTimeMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Email Email { get; set; } = null!;
}
