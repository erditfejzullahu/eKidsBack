using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SupportController> _logger;

        public SupportController(ApplicationDbContext context, ILogger<SupportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("CreateReportSupportTicket")]
        public async Task<IActionResult> CreateReportSupport(CreateReportSupportTicketDto ticketDto)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var ticket = new ReportTickets
                {
                    UserId = userId,
                    AvailableTicketId = ticketDto.AvailableTicketId,
                    ReportedUserId = ticketDto.ReportedUserId,
                    OtherMessage = ticketDto.OtherMessage,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.ReportTickets.AddAsync(ticket);
                await _context.SaveChangesAsync();

                return Ok(new {Message = "Ticket created successfully"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket");
                return BadRequest();
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost("CreateAvailableTicket")]
        public async Task<IActionResult> CreateAvailableTickets(CreateAvailableTicketDto ticketDto)
        {
            try
            {
                var ticket = new AvailableTickets
                {
                    TicketTitle = ticketDto.TicketTitle,
                    TicketTypes = ticketDto.TicketType,
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
