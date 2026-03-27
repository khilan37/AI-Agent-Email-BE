namespace AI.Agent.Email.Core.Entities;

public class EmailAccount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Provider { get; set; } = "Gmail";
    public string Email { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? TokenExpiryTime { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Email> Emails { get; set; } = new List<Email>();
}
