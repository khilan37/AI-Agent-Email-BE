using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Enums;
using AI.Agent.Email.Core.Interfaces;
using AI.Agent.Email.Infrastructure.External;
using AI.Agent.Email.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly GeminiAIService _aiService;
    private readonly EmailProcessorJob _processorJob;

    public AgentController(
        IUnitOfWork unitOfWork,
        GeminiAIService aiService,
        EmailProcessorJob processorJob)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _processorJob = processorJob;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpPost("process/{emailId}")]
    public async Task<IActionResult> ProcessEmail(int emailId)
    {
        var email = await _unitOfWork.Emails.GetByIdAsync(emailId);
        
        if (email == null || email.UserId != CurrentUserId)
            return NotFound();

        try
        {
            await _processorJob.ProcessEmailAsync(emailId);
            
            // Reload email to get updated data
            email = await _unitOfWork.Emails.GetByIdAsync(emailId);
            
            return Ok(new ProcessEmailResponse
            {
                Success = true,
                Message = "Email processed successfully",
                Category = email?.Category?.ToString(),
                Intent = email?.Intent?.ToString(),
                SuggestedAction = email?.SuggestedAction
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProcessEmailResponse
            {
                Success = false,
                Message = $"Error processing email: {ex.Message}"
            });
        }
    }

    [HttpPost("generate-reply")]
    public async Task<IActionResult> GenerateReply([FromBody] GenerateReplyRequest request)
    {
        var reply = await _aiService.GenerateReplyAsync(request.Subject, request.Body, request.Tone);
        
        if (string.IsNullOrEmpty(reply))
        {
            return StatusCode(500, new { Message = "Failed to generate reply" });
        }

        return Ok(new GenerateReplyResponse { Reply = reply });
    }

    [HttpGet("insights/{emailId}")]
    public async Task<IActionResult> GetInsights(int emailId)
    {
        var email = await _unitOfWork.Emails.GetByIdAsync(emailId);
        
        if (email == null || email.UserId != CurrentUserId)
            return NotFound();

        var response = new AIInsightsResponse
        {
            EmailId = email.Id,
            Category = email.Category?.ToString(),
            Intent = email.Intent?.ToString(),
            Priority = email.Priority,
            ConfidenceScore = email.ConfidenceScore,
            SuggestedAction = email.SuggestedAction,
            GeneratedReply = email.GeneratedReply,
            ExtractedData = new ExtractedDataDto
            {
                MeetingDate = email.MeetingDate,
                TaskTitle = email.TaskTitle,
                FollowUpDate = email.FollowUpDate
            }
        };

        return Ok(response);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetRecentLogs([FromQuery] int count = 50)
    {
        var logs = await _unitOfWork.AgentLogs.GetRecentAsync(count);
        
        var dtos = logs.Select(l => new AgentLogDto
        {
            Id = l.Id,
            ActionTaken = l.ActionTaken,
            ActionDetails = l.ActionDetails,
            Success = l.Success,
            ErrorMessage = l.ErrorMessage,
            ProcessingTimeMs = l.ProcessingTimeMs,
            Timestamp = l.Timestamp
        }).ToList();

        return Ok(dtos);
    }
}
