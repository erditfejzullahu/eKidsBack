using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscussionsController : ControllerBase
    {
        private readonly ILogger<DiscussionsController> _logger;
        private readonly ApplicationDbContext _context;

        public DiscussionsController( ILogger<DiscussionsController> logger, ApplicationDbContext context )
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiscussions([FromBody] int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                //using var transation = await _context.Database.BeginTransactionAsync(token);
                var allDiscussions = await _context.Discussions.CountAsync(token);
                var discussions = await _context.Discussions
                    .Include(c => c.User)
                    .Include(c => c.DiscussionWithTags)
                    .ThenInclude(dt => dt.DiscussionTag)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new
                    {
                        c.ID,
                        c.Title,
                        c.Content,
                        c.UserId,
                        c.PreferAnonimity,
                        c.Views,
                        c.Votes,
                        User = new
                        {
                            c.User.Username,
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            c.User.ProfilePictureUrl,
                        },
                        Tags = c.DiscussionWithTags.Select(dt => new
                        {
                            dt.DiscussionTag.ID,
                            dt.DiscussionTag.Title
                        }),
                        c.CreatedAt
                    })
                    .ToListAsync();

                if(discussions.Count == 0)
                {
                    return NotFound(new { Message = "No discussion found" });
                }
                //await transation.CommitAsync(token);
                bool hasMore = discussions.Count == paginationDto.Take && discussions.Count < allDiscussions;
                return Ok(new {discussionsCount = allDiscussions, data = discussions, hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error in GetAllDiscussions");
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDiscussion([FromBody] DiscussionDto discussionDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {

                var discussion = new Discussions
                {
                    Title = discussionDto.Title,
                    Content = discussionDto.Content,
                    UserId = discussionDto.UserId,
                    PreferAnonimity = discussionDto.PreferAnonimity,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                
                foreach (var item in discussionDto.Tags)
                {
                    DiscussionTags tag;

                    if (item.TagId.HasValue)
                    {
                        tag = await _context.DiscussionTags.FindAsync(item.TagId.Value, token);
                        if (tag == null)
                        {
                            return BadRequest(new { Message = "Id provided does not exist" });
                        }
                    }
                    else
                    {
                        tag = await _context.DiscussionTags.FirstOrDefaultAsync(c => c.Title == item.Title);
                        if(tag == null)
                        {
                            tag = new DiscussionTags
                            {
                                Title = item.Title,
                                Description = item.Description,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };

                            await _context.DiscussionTags.AddAsync(tag, token);
                            await _context.SaveChangesAsync(token);
                        }
                    }

                    discussion.DiscussionWithTags.Add(new DiscussionWithTags
                    {
                        TagId = tag.ID,
                        Discussion = discussion,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                    });

                }

                    await _context.Discussions.AddAsync(discussion, token);
                    await _context.SaveChangesAsync(token);
                    return Ok(new { Message = "Discussion Created" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);    
                _logger.LogError(ex, " Error in creating discussion");
                return BadRequest();
            }
        }

        [HttpPatch("changeAnonimity/{id}")]
        public async Task<IActionResult> ChangeAnonimity(int id, [FromQuery] DiscussionAnonimityStatus anonimityStatus, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussion = await _context.Discussions.FindAsync(id, token);
                if(discussion == null)
                {
                    return NotFound(new {Message = "Discussion not found!"});
                }

                discussion.PreferAnonimity = anonimityStatus;
                discussion.LastModified = DateTime.UtcNow;

                _context.Update(discussion);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                return Ok(new { Message = "Anonimity Changed" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in changing anonimity");
                return BadRequest();
            }
        } 
    }
}
