using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly IRepository<Conversations> _conversationsRepository;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(IRepository<Conversations> conversationsRepository, ILogger<ConversationsController> logger)
        {
            _conversationsRepository = conversationsRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageDto messageDto)
        {
            var message = new Conversations
            {
                SenderUsername = messageDto.SenderUsername,
                ReceiverUsername = messageDto.ReceiverUsername,
                Content = messageDto.Content,
                IsRead = messageDto.IsRead,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            _conversationsRepository.Add(message);
            await _conversationsRepository.SaveAsync(default);
            return Ok(message);
        }

        [HttpGet("/api/Conversations/{sender}/{receiver}")]
        public async Task<IActionResult> GetMessagesMade(string sender, string receiver, [FromQuery] int page = 1, [FromQuery] int pageSize = 15, CancellationToken token = default)
        {
            try
            {
                if (page <= 0)
                {
                    return BadRequest("Page must be greater than 0.");
                }

                if (pageSize <= 0 || pageSize > 100)
                {
                    return BadRequest("Page size must be between 1 and 100.");
                }

                var skip = (page - 1) * pageSize;

                var messages = await _conversationsRepository.GetAll()
                    .AsNoTracking()
                    .Where(c => (c.SenderUsername == sender && c.ReceiverUsername == receiver) || (c.SenderUsername == receiver && c.ReceiverUsername == sender))
                    .Select(c => new
                    {
                        c.ID,
                        c.Content,
                        c.FileUrl,
                        c.IsRead,
                        c.SenderUsername,
                        c.ReceiverUsername,
                        c.CreatedAt,
                        Sender = new
                        {
                            c.Sender.Firstname,
                            c.Sender.Lastname,
                            c.Sender.Username,
                            c.Sender.ProfilePictureUrl,
                        },
                        Receiver = new
                        {
                            c.Receiver.Firstname,
                            c.Receiver.Lastname,
                            c.Receiver.Username,
                            c.Receiver.ProfilePictureUrl
                        }
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip(skip)
                    .Take(pageSize)
                    //.OrderBy(c => c.CreatedAt)
                    .ToListAsync(token);

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving messages for {sender} and reciver {receiver}");
                return BadRequest(new { Message = "Error retriving messages" });
            }
        }

    }
}
