using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupportController> _logger;
        private readonly IFileUploadService _uploadService;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public SupportController(IHubContext<NotificationsHub> notificationsHub, ApplicationDbContext context, ILogger<SupportController> logger, IFileUploadService uploadService)
        {
            _context = context;
            _logger = logger;
            _uploadService = uploadService;
            _notificationsHub = notificationsHub;
        }

        [Authorize]
        [HttpPost("CreateReportSupportTicket")]
        public async Task<IActionResult> CreateReportSupport(CreateReportSupportTicketDto ticketDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                string? url = string.Empty;
                if (!string.IsNullOrEmpty(ticketDto.Base64Data))
                {
                    var relativeUrl = _uploadService.UploadFile(ticketDto.Base64Data, FileCategory.Other);
                    url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                }

                var ticket = new ReportTickets
                {
                    UserId = userId,
                    AvailableTicketId = ticketDto.AvailableTicketId,
                    ReportedUserId = ticketDto.ReportedUserId,
                    OtherMessage = ticketDto.OtherMessage,
                    Image = url,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.ReportTickets.AddAsync(ticket, token);

                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var informationResponse = ticket.AvailableTicket.TicketType == Database.Shared.Enums.AvailableTicketsTypes.Report
                    ? $"Njoftim mbi raportimin {ticket.AvailableTicket.TicketTitle} me {DateTime.Now.ToString("f", albanianCulture)}"
                    : $"Njoftim mbi kerkesen per suport {ticket.AvailableTicket.TicketTitle} me {DateTime.Now.ToString("f", albanianCulture)}";

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = informationResponse,
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && c.IsRead == false).CountAsync();
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }
                return Ok(new {Message = "Ticket created successfully"});
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error creating ticket");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("CreateAvailableTicket")]
        public async Task<IActionResult> CreateAvailableTickets(CreateAvailableTicketDto ticketDto)
        {
            try
            {
                var ticket = new AvailableTickets
                {
                    TicketTitle = ticketDto.TicketTitle,
                    TicketType = ticketDto.TicketType,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.AvailableTickets.AddAsync(ticket);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Ticket created" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating available ticket");
                return BadRequest();
            }
        }

        [HttpGet("GetAvailableTickets")]
        public async Task<IActionResult> GetAvailableTickets()
        {
            try
            {
                //var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                //if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                //{
                //    return Unauthorized();
                //}

                var tickets = await _context.AvailableTickets.AsNoTracking().OrderBy(c => c.CreatedAt).ToListAsync();
                if(tickets.Count == 0)
                {
                    return NotFound(new {Message = "No Available Tickets found"});
                }
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all available tickets");
                return BadRequest();
            }
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("GetReportSupportTickets")]
        public async Task<IActionResult> GetReportSupportTickets()
        {
            try
            {
                var tickets = await _context.ReportTickets
                    .Select(c => new
                    {
                        c.ID,
                        c.UserId,
                        c.AvailableTicket,
                        ReportedUser = new
                        {
                            Name = c.ReportedUser.Firstname + " " + c.ReportedUser.Lastname,
                            c.ReportedUser.ID,
                            c.ReportedUser.ProfilePictureUrl
                        },
                        SubmittedUser = new
                        {
                            Name = c.UserSubmitted.Firstname + " " + c.UserSubmitted.Lastname,
                            c.UserSubmitted.ID,
                            c.UserSubmitted.ProfilePictureUrl
                        },
                        c.OtherMessage,
                        c.CreatedAt,
                        c.LastModified
                    }).ToListAsync();

                if(tickets.Count == 0)
                {
                    return NotFound(new { Message = "No tickets made found" });
                }
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report support tickets");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteAvailableTicket/{id}")]
        public async Task<IActionResult> DeleteAvailableTicket(int id)
        {
            try
            {
                var ticket = await _context.AvailableTickets.FirstOrDefaultAsync(c => c.ID == id);
                if(ticket == null)
                {
                    return NotFound(new { Message = "No ticket found" });
                }
                _context.AvailableTickets.Remove(ticket);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "ticket deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleeting available ticket");
                return BadRequest();
            }
        }
    }
}
