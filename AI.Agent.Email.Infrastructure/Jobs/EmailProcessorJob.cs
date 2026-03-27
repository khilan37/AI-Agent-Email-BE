using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.External;
using Microsoft.Extensions.Logging;

namespace AI.Agent.Email.Infrastructure.Jobs;

public class EmailProcessorJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GeminiAIService _aiService;
    private readonly GmailService _gmailService;
    private readonly ILogger<EmailProcessorJob> _logger;

    public EmailProcessorJob(
        IUnitOfWork unitOfWork,
        GeminiAIService aiService,
        GmailService gmailService,
        ILogger<EmailProcessorJob> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _gmailService = gmailService;
        _logger = logger;
    }

    public async Task ProcessUnprocessedEmailsAsync()
    {
        _logger.LogInformation("Starting email processor job at {Time}", DateTime.UtcNow);
        
        try
        {
            var unprocessedEmails = await _unitOfWork.Emails.GetUnprocessedAsync(limit: 20);
            
            foreach (var email in unprocessedEmails)
            {
                await ProcessEmailAsync(email.Id);
            }
            
            _logger.LogInformation("Email processor job completed. Processed {Count} emails", unprocessedEmails.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email processor job");
            throw;
        }
    }

    public async Task ProcessEmailAsync(int emailId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var email = await _unitOfWork.Emails.GetByIdAsync(emailId);
            if (email == null)
            {
                _logger.LogWarning("Email {EmailId} not found", emailId);
                return;
            }

            _logger.LogInformation("Processing email {EmailId}: {Subject}", emailId, email.Subject);

            // Step 1: AI Analysis
            var analysis = await _aiService.AnalyzeEmailAsync(email.Subject, email.BodyPlainText);
            
            // Update email with AI insights
            if (Enum.TryParse<EmailCategory>(analysis.Category, out var category))
                email.Category = category;
            
            if (Enum.TryParse<EmailIntent>(analysis.Intent, out var intent))
                email.Intent = intent;
            
            email.Priority = analysis.Priority;
            email.ConfidenceScore = analysis.Confidence;
            email.SuggestedAction = analysis.SuggestedAction;
            email.GeneratedReply = analysis.Reply;
            
            // Extract dates and data
            if (!string.IsNullOrEmpty(analysis.ExtractedData?.MeetingDate))
            {
                if (DateTime.TryParse(analysis.ExtractedData.MeetingDate, out var meetingDate))
                    email.MeetingDate = meetingDate;
            }
            
            email.TaskTitle = analysis.ExtractedData?.TaskTitle;
            
            if (!string.IsNullOrEmpty(analysis.ExtractedData?.FollowUpDate))
            {
                if (DateTime.TryParse(analysis.ExtractedData.FollowUpDate, out var followUpDate))
                    email.FollowUpDate = followUpDate;
            }

            // Step 2: Execute suggested action
            var actionTaken = await ExecuteActionAsync(email, analysis);

            // Mark as processed
            email.IsProcessed = true;
            email.ProcessedAt = DateTime.UtcNow;
            email.Status = EmailStatus.Processed;
            
            await _unitOfWork.Emails.UpdateAsync(email);

            // Log the action
            var agentLog = new AgentLog
            {
                EmailId = emailId,
                ActionTaken = actionTaken,
                ActionDetails = $"Category: {analysis.Category}, Intent: {analysis.Intent}, Action: {analysis.SuggestedAction}",
                Success = true,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
            await _unitOfWork.AgentLogs.AddAsync(agentLog);
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Successfully processed email {EmailId} in {Ms}ms", emailId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Error processing email {EmailId}", emailId);
            
            // Log the failure
            var agentLog = new AgentLog
            {
                EmailId = emailId,
                ActionTaken = "ProcessingFailed",
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
            await _unitOfWork.AgentLogs.AddAsync(agentLog);
            await _unitOfWork.SaveChangesAsync();
            
            throw;
        }
    }

    private async Task<string> ExecuteActionAsync(global::AI.Agent.Email.Core.Entities.Email email, AIAnalysisResult analysis)
    {
        var settings = await _unitOfWork.Users.GetSettingsAsync(email.UserId);
        var action = analysis.SuggestedAction;
        
        // Check confidence threshold
        if (analysis.Confidence < (settings?.MinConfidenceThreshold ?? 0.7))
        {
            _logger.LogInformation("Email {EmailId} confidence {Confidence} below threshold, skipping auto-action", 
                email.Id, analysis.Confidence);
            return "Skipped_LowConfidence";
        }

        switch (action)
        {
            case "Reply":
                return await HandleReplyAsync(email, analysis, settings);
                
            case "ScheduleMeeting":
                return await HandleMeetingRequestAsync(email, analysis);
                
            case "CreateTask":
                return await HandleTaskCreationAsync(email, analysis);
                
            case "SetReminder":
                return await HandleReminderAsync(email, analysis);
                
            case "Ignore":
                email.Status = EmailStatus.Ignored;
                return "Ignored";
                
            default:
                return "NoAction";
        }
    }

    private async Task<string> HandleReplyAsync(global::AI.Agent.Email.Core.Entities.Email email, AIAnalysisResult analysis, UserSettings? settings)
    {
        // Check if auto-reply is enabled
        if (settings?.AutoReplyEnabled != true)
        {
            _logger.LogInformation("Auto-reply disabled for user {UserId}, storing generated reply", email.UserId);
            return "Reply_Generated_NotSent";
        }

        // Check if approval is required
        if (settings?.RequireApprovalForReplies == true && analysis.Confidence < 0.9)
        {
            _logger.LogInformation("Reply requires approval for email {EmailId}", email.Id);
            return "Reply_PendingApproval";
        }

        // Send reply via Gmail
        var account = await _unitOfWork.EmailAccounts.GetByIdAsync(email.EmailAccountId);
        if (account == null || string.IsNullOrEmpty(account.AccessToken))
        {
            _logger.LogWarning("Cannot send reply for email {EmailId}: no valid account", email.Id);
            return "Reply_Failed_NoAccount";
        }

        var replyBody = analysis.Reply ?? "Thank you for your email. I will get back to you soon.";
        
        var sent = await _gmailService.SendReplyAsync(
            account.AccessToken,
            email.GmailMessageId,
            email.SenderEmail,
            email.Subject,
            replyBody);

        if (sent)
        {
            email.IsAutoReplied = true;
            email.ReplySentAt = DateTime.UtcNow;
            email.Status = EmailStatus.Replied;
            _logger.LogInformation("Auto-reply sent for email {EmailId}", email.Id);
            return "Reply_Sent";
        }
        else
        {
            _logger.LogWarning("Failed to send auto-reply for email {EmailId}", email.Id);
            return "Reply_Failed";
        }
    }

    private Task<string> HandleMeetingRequestAsync(global::AI.Agent.Email.Core.Entities.Email email, AIAnalysisResult analysis)
    {
        // For now, just mark it. In a real implementation, you'd integrate with calendar API
        _logger.LogInformation("Meeting request detected for email {EmailId}", email.Id);
        return Task.FromResult("Meeting_Notification_Set");
    }

    private async Task<string> HandleTaskCreationAsync(global::AI.Agent.Email.Core.Entities.Email email, AIAnalysisResult analysis)
    {
        var taskTitle = email.TaskTitle ?? analysis.ExtractedData?.TaskTitle ?? $"Follow up: {email.Subject}";
        
        var task = new TaskItem
        {
            UserId = email.UserId,
            EmailId = email.Id,
            Title = taskTitle.Length > 500 ? taskTitle[..500] : taskTitle,
            Description = $"Created from email: {email.Subject}\n\nFrom: {email.SenderEmail}",
            Status = global::AI.Agent.Email.Core.Enums.TaskStatus.Pending,
            DueDate = email.FollowUpDate ?? DateTime.UtcNow.AddDays(1)
        };

        await _unitOfWork.Tasks.AddAsync(task);
        _logger.LogInformation("Task created for email {EmailId}", email.Id);
        
        return "Task_Created";
    }

    private Task<string> HandleReminderAsync(global::AI.Agent.Email.Core.Entities.Email email, AIAnalysisResult analysis)
    {
        // Reminder is essentially stored in the FollowUpDate field
        _logger.LogInformation("Reminder set for email {EmailId} on {Date}", email.Id, email.FollowUpDate);
        return Task.FromResult("Reminder_Set");
    }
}
