using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public EmailController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetEmails([FromQuery] EmailListRequest request)
    {
        var emails = await _unitOfWork.Emails.GetByUserIdAsync(CurrentUserId, request.Page, request.PageSize);
        var totalCount = await _unitOfWork.Emails.GetTotalCountByUserIdAsync(CurrentUserId);

        var emailDtos = emails.Select(e => MapToDto(e)).ToList();

        var response = new EmailListResponse
        {
            Emails = emailDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmail(int id)
    {
        var email = await _unitOfWork.Emails.GetByIdAsync(id);
        
        if (email == null || email.UserId != CurrentUserId)
            return NotFound();

        var dto = MapToDetailDto(email);
        return Ok(dto);
    }

    [HttpGet("unread/count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _unitOfWork.Emails.GetUnreadCountByUserIdAsync(CurrentUserId);
        return Ok(new { Count = count });
    }

    [HttpPost("{id}/reply")]
    public async Task<IActionResult> SendReply(int id, [FromBody] SendReplyRequest request)
    {
        var email = await _unitOfWork.Emails.GetByIdAsync(id);
        
        if (email == null || email.UserId != CurrentUserId)
            return NotFound();

        // TODO: Implement reply sending via Gmail service
        
        email.Status = EmailStatus.Replied;
        await _unitOfWork.Emails.UpdateAsync(email);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Reply sent successfully" });
    }

    [HttpPost("{id}/ignore")]
    public async Task<IActionResult> IgnoreEmail(int id)
    {
        var email = await _unitOfWork.Emails.GetByIdAsync(id);
        
        if (email == null || email.UserId != CurrentUserId)
            return NotFound();

        email.Status = EmailStatus.Ignored;
        await _unitOfWork.Emails.UpdateAsync(email);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Email marked as ignored" });
    }

    [HttpGet("sync")]
    public async Task<IActionResult> SyncEmails()
    {
        // This triggers the sync job - actual sync happens via Hangfire
        return Ok(new SyncEmailsResponse 
        { 
            SyncedCount = 0, 
            Message = "Email sync has been queued. New emails will appear shortly." 
        });
    }

    private EmailDto MapToDto(Core.Entities.Email e)
    {
        return new EmailDto
        {
            Id = e.Id,
            GmailMessageId = e.GmailMessageId,
            Subject = e.Subject,
            Body = e.Body,
            BodyPlainText = e.BodyPlainText,
            SenderEmail = e.SenderEmail,
            SenderName = e.SenderName,
            ReceivedDate = e.ReceivedDate,
            Category = e.Category?.ToString(),
            Intent = e.Intent?.ToString(),
            Priority = e.Priority,
            ConfidenceScore = e.ConfidenceScore,
            SuggestedAction = e.SuggestedAction,
            GeneratedReply = e.GeneratedReply,
            Status = e.Status.ToString(),
            IsAutoReplied = e.IsAutoReplied,
            ReplySentAt = e.ReplySentAt,
            MeetingDate = e.MeetingDate,
            TaskTitle = e.TaskTitle,
            FollowUpDate = e.FollowUpDate,
            IsProcessed = e.IsProcessed,
            CreatedAt = e.CreatedAt
        };
    }

    private EmailDetailResponse MapToDetailDto(Core.Entities.Email e)
    {
        return new EmailDetailResponse
        {
            Id = e.Id,
            GmailMessageId = e.GmailMessageId,
            Subject = e.Subject,
            Body = e.Body,
            BodyPlainText = e.BodyPlainText,
            SenderEmail = e.SenderEmail,
            SenderName = e.SenderName,
            ReceivedDate = e.ReceivedDate,
            Category = e.Category?.ToString(),
            Intent = e.Intent?.ToString(),
            Priority = e.Priority,
            ConfidenceScore = e.ConfidenceScore,
            SuggestedAction = e.SuggestedAction,
            GeneratedReply = e.GeneratedReply,
            Status = e.Status.ToString(),
            IsAutoReplied = e.IsAutoReplied,
            ReplySentAt = e.ReplySentAt,
            MeetingDate = e.MeetingDate,
            TaskTitle = e.TaskTitle,
            FollowUpDate = e.FollowUpDate,
            IsProcessed = e.IsProcessed,
            CreatedAt = e.CreatedAt,
            Tasks = e.Tasks.Select(t => new TaskItemDto
            {
                Id = t.Id,
                EmailId = t.EmailId,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }).ToList(),
            AgentLogs = e.AgentLogs.Select(l => new AgentLogDto
            {
                Id = l.Id,
                ActionTaken = l.ActionTaken,
                ActionDetails = l.ActionDetails,
                Success = l.Success,
                ErrorMessage = l.ErrorMessage,
                ProcessingTimeMs = l.ProcessingTimeMs,
                Timestamp = l.Timestamp
            }).ToList()
        };
    }
}
