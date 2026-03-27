using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Entities;
using AI.Agent.Email.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _unitOfWork.Users.GetSettingsAsync(CurrentUserId);
        
        if (settings == null)
        {
            // Create default settings
            settings = new UserSettings
            {
                UserId = CurrentUserId,
                AutoReplyEnabled = false,
                DefaultReplyDelayMinutes = 5,
                EmailCheckIntervalMinutes = 1,
                RequireApprovalForReplies = true,
                MinConfidenceThreshold = 0.7,
                DefaultReplyLanguage = "en",
                EmailNotificationsEnabled = true,
                InAppNotificationsEnabled = true
            };
            await _unitOfWork.Users.UpdateSettingsAsync(settings);
            await _unitOfWork.SaveChangesAsync();
        }

        var dto = new UserSettingsDto
        {
            AutoReplyEnabled = settings.AutoReplyEnabled,
            DefaultReplyDelayMinutes = settings.DefaultReplyDelayMinutes,
            EmailCheckIntervalMinutes = settings.EmailCheckIntervalMinutes,
            RequireApprovalForReplies = settings.RequireApprovalForReplies,
            MinConfidenceThreshold = settings.MinConfidenceThreshold,
            DefaultReplyLanguage = settings.DefaultReplyLanguage,
            EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
            InAppNotificationsEnabled = settings.InAppNotificationsEnabled
        };

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request)
    {
        var settings = await _unitOfWork.Users.GetSettingsAsync(CurrentUserId);
        
        if (settings == null)
        {
            settings = new UserSettings
            {
                UserId = CurrentUserId
            };
        }

        settings.AutoReplyEnabled = request.AutoReplyEnabled;
        settings.DefaultReplyDelayMinutes = request.DefaultReplyDelayMinutes;
        settings.EmailCheckIntervalMinutes = request.EmailCheckIntervalMinutes;
        settings.RequireApprovalForReplies = request.RequireApprovalForReplies;
        settings.MinConfidenceThreshold = request.MinConfidenceThreshold;
        settings.DefaultReplyLanguage = request.DefaultReplyLanguage;
        settings.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        settings.InAppNotificationsEnabled = request.InAppNotificationsEnabled;

        await _unitOfWork.Users.UpdateSettingsAsync(settings);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { Message = "Settings updated successfully" });
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetEmailAccounts()
    {
        var accounts = await _unitOfWork.EmailAccounts.GetActiveByUserIdAsync(CurrentUserId);
        
        var dtos = accounts.Select(a => new EmailAccountDto
        {
            Id = a.Id,
            Provider = a.Provider,
            Email = a.Email,
            IsActive = a.IsActive,
            IsConnected = a.IsConnected,
            CreatedAt = a.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("accounts/connect")]
    public async Task<IActionResult> ConnectAccount([FromBody] ConnectGmailRequest request)
    {
        // TODO: Implement OAuth flow with Gmail
        // For now, return placeholder response
        return Ok(new { Message = "Account connection initiated. Complete OAuth in settings page." });
    }

    [HttpDelete("accounts/{id}")]
    public async Task<IActionResult> DisconnectAccount(int id)
    {
        var account = await _unitOfWork.EmailAccounts.GetByIdAsync(id);
        
        if (account == null || account.UserId != CurrentUserId)
            return NotFound();

        account.IsActive = false;
        account.IsConnected = false;
        await _unitOfWork.EmailAccounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new DisconnectAccountResponse { Success = true, Message = "Account disconnected successfully" });
    }
}
