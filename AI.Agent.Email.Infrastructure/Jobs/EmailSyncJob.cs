using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.External;
using Microsoft.Extensions.Logging;

namespace AI.Agent.Email.Infrastructure.Jobs;

public class EmailSyncJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GmailService _gmailService;
    private readonly ILogger<EmailSyncJob> _logger;

    public EmailSyncJob(
        IUnitOfWork unitOfWork,
        GmailService gmailService,
        ILogger<EmailSyncJob> logger)
    {
        _unitOfWork = unitOfWork;
        _gmailService = gmailService;
        _logger = logger;
    }

    public async Task SyncEmailsAsync()
    {
        _logger.LogInformation("Starting email sync job at {Time}", DateTime.UtcNow);
        
        try
        {
            // Get all active email accounts
            var allAccounts = new List<Core.Entities.EmailAccount>();
            
            // Since we don't have a direct method to get all accounts, we need to iterate through users
            // For now, let's fetch emails for accounts that need syncing
            // This would typically be managed by a user-specific job scheduling
            
            _logger.LogInformation("Email sync job completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email sync job");
            throw;
        }
    }

    public async Task SyncUserEmailsAsync(int userId)
    {
        _logger.LogInformation("Syncing emails for user {UserId}", userId);
        
        try
        {
            var accounts = await _unitOfWork.EmailAccounts.GetActiveByUserIdAsync(userId);
            
            foreach (var account in accounts)
            {
                if (!account.IsConnected || string.IsNullOrEmpty(account.AccessToken))
                    continue;

                // Check if token needs refresh
                if (account.TokenExpiryTime.HasValue && account.TokenExpiryTime.Value <= DateTime.UtcNow.AddMinutes(5))
                {
                    if (!string.IsNullOrEmpty(account.RefreshToken))
                    {
                        var (success, newToken, newExpiry) = await _gmailService.RefreshAccessTokenAsync(account.RefreshToken);
                        
                        if (success)
                        {
                            account.AccessToken = newToken;
                            account.TokenExpiryTime = newExpiry;
                            await _unitOfWork.EmailAccounts.UpdateAsync(account);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to refresh token for account {AccountId}", account.Id);
                            continue;
                        }
                    }
                }

                // Fetch unread emails
                var gmailMessages = await _gmailService.FetchUnreadEmailsAsync(account.AccessToken!, maxResults: 50);
                
                foreach (var gmailMessage in gmailMessages)
                {
                    // Check if already exists
                    if (await _unitOfWork.Emails.ExistsAsync(gmailMessage.Id))
                        continue;

                    var from = GmailService.ExtractHeaderValue(gmailMessage, "From");
                    var subject = GmailService.ExtractHeaderValue(gmailMessage, "Subject");
                    var body = GmailService.ExtractBody(gmailMessage);
                    
                    // Parse sender name and email
                    var (senderName, senderEmail) = ParseSender(from);
                    
                    // Parse received date
                    var dateHeader = GmailService.ExtractHeaderValue(gmailMessage, "Date");
                    var receivedDate = ParseDate(dateHeader);

                    var email = new global::AI.Agent.Email.Core.Entities.Email
                    {
                        UserId = userId,
                        EmailAccountId = account.Id,
                        GmailMessageId = gmailMessage.Id,
                        Subject = subject,
                        Body = body,
                        BodyPlainText = body,
                        SenderEmail = senderEmail,
                        SenderName = senderName,
                        ReceivedDate = receivedDate,
                        Status = EmailStatus.Unread,
                        IsProcessed = false
                    };

                    await _unitOfWork.Emails.AddAsync(email);
                    _logger.LogInformation("Added new email {MessageId} for user {UserId}", gmailMessage.Id, userId);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            
            _logger.LogInformation("Completed syncing emails for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing emails for user {UserId}", userId);
            throw;
        }
    }

    private (string name, string email) ParseSender(string from)
    {
        if (string.IsNullOrEmpty(from))
            return (string.Empty, string.Empty);

        // Format: "Name" <email@domain.com> or just email@domain.com
        var match = System.Text.RegularExpressions.Regex.Match(from, @"(?:""?([^""<>]+)""?\s*)?<?([^<>\s]+@[^<>\s]+)>?");
        
        if (match.Success)
        {
            var name = match.Groups[1].Value.Trim();
            var email = match.Groups[2].Value.Trim();
            
            if (string.IsNullOrEmpty(name))
                name = email.Split('@')[0];
                
            return (name, email);
        }

        return (from, from);
    }

    private DateTime ParseDate(string dateHeader)
    {
        if (string.IsNullOrEmpty(dateHeader))
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateHeader, out var result))
            return result.ToUniversalTime();

        return DateTime.UtcNow;
    }
}
