using AI.Agent.Email.Core.DTOs;
using AI.Agent.Email.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.Agent.Email.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var totalEmails = await _unitOfWork.Emails.GetTotalCountByUserIdAsync(CurrentUserId);
        var unreadEmails = await _unitOfWork.Emails.GetUnreadCountByUserIdAsync(CurrentUserId);
        var urgentEmails = await _unitOfWork.Emails.GetUrgentCountByUserIdAsync(CurrentUserId);
        var pendingTasks = await _unitOfWork.Tasks.GetPendingCountByUserIdAsync(CurrentUserId);
        var totalTasks = await _unitOfWork.Tasks.GetTotalCountByUserIdAsync(CurrentUserId);

        var recentEmails = await _unitOfWork.Emails.GetRecentByUserIdAsync(CurrentUserId, 5);
        var recentTasks = await _unitOfWork.Tasks.GetByUserIdAsync(CurrentUserId);

        var response = new DashboardStatsResponse
        {
            TotalEmails = totalEmails,
            UnreadEmails = unreadEmails,
            UrgentEmails = urgentEmails,
            PendingTasks = pendingTasks,
            TotalTasks = totalTasks,
            RecentEmails = recentEmails.Select(e => new RecentEmailDto
            {
                Id = e.Id,
                Subject = e.Subject,
                SenderName = e.SenderName,
                Category = e.Category?.ToString(),
                ReceivedDate = e.ReceivedDate
            }).ToList(),
            RecentTasks = recentTasks.Take(5).Select(t => new RecentTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status.ToString(),
                DueDate = t.DueDate
            }).ToList(),
            RecentActivity = new List<ActivityDto>() // Would populate from agent logs
        };

        return Ok(response);
    }
}
