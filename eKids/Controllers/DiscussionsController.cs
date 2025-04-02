using AutoMapper;
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
        private readonly IMapper _mapper;

        public DiscussionsController(IMapper mapper, ILogger<DiscussionsController> logger, ApplicationDbContext context )
        {
            _logger = logger;
            _context = context;
            _mapper = mapper;
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

        [HttpGet("TypingTags")]
        public async Task<IActionResult> GetTypingTags([FromQuery] string title)
        {
            try
            {
                var tags = await _context.DiscussionTags.Where(c => EF.Functions.Contains(c.Title, $"\"{title}*\"")).Select(c => new {c.Title}).ToListAsync();
                if(tags.Count == 0)
                {
                    return NotFound(new { Message = "No tags found" });
                }
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error in retriving tags gettypingtags");
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
                    Edited = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                
                foreach (var item in discussionDto.Tags)
                {
                    DiscussionTags tag;

                    tag = await _context.DiscussionTags.FirstOrDefaultAsync(c => c.Title.ToLower() == item.Title.ToLower(), token);
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

                    discussion.DiscussionWithTags.Add(new DiscussionsWithTags
                    {
                        TagId = tag.ID,
                        Discussion = discussion,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                    });

                }

                await _context.Discussions.AddAsync(discussion, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Discussion Created" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);    
                _logger.LogError(ex, " Error in creating discussion");
                return BadRequest();
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> EditDiscussion(int id, DiscussionDto discussionDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussion = await _context.Discussions
                    .Include(d => d.DiscussionWithTags)
                    .FirstOrDefaultAsync(d => d.ID == id, token);
                if (discussion == null)
                {
                    return NotFound(new { Message = "Discussion not found" });
                }
                _mapper.Map(discussionDto, discussion);
                discussion.LastModified = DateTime.UtcNow;

                if(discussionDto.Tags.Count > 0)
                {
                    var tags = await _context.DiscussionsWithTags.Where(c => c.DiscussionId == id).ToListAsync(token);
                    _context.DiscussionsWithTags.RemoveRange(tags);

                    foreach (var item in discussionDto.Tags)
                    {
                        DiscussionTags tag;
                        
                        tag = await _context.DiscussionTags.FirstOrDefaultAsync(c => c.Title.ToLower() == item.Title.ToLower(), token);
                        if(tag == null)
                        {
                            tag = new DiscussionTags
                            {
                                Title = item.Title,
                                Description = item.Description,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };

                            await _context.DiscussionTags.AddAsync(tag); 
                            await _context.SaveChangesAsync(token);
                        }

                        discussion.DiscussionWithTags.Add(new DiscussionsWithTags
                        {
                            TagId = tag.ID,
                            Discussion = discussion,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                    }
                }

                _context.Discussions.Update(discussion);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Discussion updated successfully" });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in edditing discussion");
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(int id, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussion = await _context.Discussions.FindAsync(id, token);
                if(discussion == null)
                {
                    return NotFound(new { Message = "No discussion found" });
                }

                _context.Discussions.Remove(discussion);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Discussion deleted" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in deleting discussion");
                return BadRequest();
            }
        }
    }
}
