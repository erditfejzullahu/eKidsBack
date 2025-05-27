using AutoMapper;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
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
        private readonly IDiscussionAnswerService _discussionAnswerService;
        private readonly IFileUploadService _fileUploadService;

        public DiscussionsController(IFileUploadService fileUploadService, IMapper mapper, ILogger<DiscussionsController> logger, ApplicationDbContext context, IDiscussionAnswerService discussionAnswerService)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
            _context = context;
            _mapper = mapper;
            _discussionAnswerService = discussionAnswerService;
        }

        [HttpPost("CreateDiscussionAnswer")]
        public async Task<IActionResult> CreateDiscussionAnswer([FromBody] CreateDiscussionAnswerDto createDiscussionAnswer, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var getDiscussion = await _context.Discussions.FindAsync(createDiscussionAnswer.DiscussionId);
                if(getDiscussion == null)
                {
                    return NotFound("Not found discussion");
                }
                string item_url = string.Empty;
                if (!string.IsNullOrEmpty(createDiscussionAnswer.DiscussionFile))
                {
                    var uploadPath = await _fileUploadService.UploadFile(createDiscussionAnswer.DiscussionFile, FileCategory.Other);
                    item_url = $"{Request.Scheme}://{Request.Host}/{uploadPath}";
                }

                var createAnswer = new DiscussionAnswers
                {
                    Content = createDiscussionAnswer.DiscussionAnswerContent,
                    UserId = createDiscussionAnswer.UserId,
                    DiscussionId = createDiscussionAnswer.DiscussionId,
                    Votes = 0,
                    Item_Url = item_url,
                    ParentId = createDiscussionAnswer.ParentId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.DiscussionAnswers.AddAsync(createAnswer);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(createAnswer);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error creating discussion answer");
                return BadRequest();
            }
        }

        [HttpPatch("HandleDiscussionVotes")]
        public async Task<IActionResult> HandleDiscussionVotes([FromBody] DiscussionHandleVoteDto discussionHandleVoteDto, CancellationToken token)
        {
            try
            {
                var handleVote = await _discussionAnswerService.HandleDiscussionVoteStatusAsync(discussionHandleVoteDto.UserId, discussionHandleVoteDto.DiscussionId, discussionHandleVoteDto.DiscussionVoteType, token);
                return Ok(new { VoteResponse = handleVote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handling discussion vote");
                return BadRequest();
            }
        }

        [HttpPatch("HandleAnswerVotes")]
        public async Task<IActionResult> HandleAnswerVotes([FromBody] DiscussionAnswerHandleVoteDto handleVoteDto, CancellationToken token)
        {
            try
            {
                var handleVote = await _discussionAnswerService.HandleAnswerVoteStatusAsync(handleVoteDto.UserId, handleVoteDto.DiscussionAnswerId, handleVoteDto.DiscussionId, handleVoteDto.DiscussionVoteType, token);
                return Ok(new { VoteResponse = handleVote }); //0 for voteup, 1 for votedown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling comment votes");
                return BadRequest();
            }
        }

        [HttpGet("GetDiscussionComments/{id}")]
        public async Task<IActionResult> GetDiscussionComments(int id, [FromQuery] int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var discussionAnswers = await _discussionAnswerService.GetDiscussionAnswersDtoAsync(id, userId, paginationDto, token);
                if(discussionAnswers.Item1.Count == 0)
                {
                    return NotFound(new { Message = "No answersFound" });
                }
                return Ok(new { data = discussionAnswers.Item1, discussionAnswers.hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting discussion comments");
                return BadRequest();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscussionById(int id, [FromQuery] int userId)
        {
            try
            {
                var discussion = await _context.Discussions
                    .Where(c => c.ID == id)
                    .Select(c => new
                    {
                        c.ID,
                        c.Title,
                        c.Content,
                        c.UserId,
                        c.PreferAnonimity,
                        c.Views,
                        c.Edited,
                        c.Votes,
                        User = c.PreferAnonimity == DiscussionAnonimityStatus.Visible ? new
                        {
                            c.User.Username,
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            c.User.ProfilePictureUrl,
                        } : null,
                        Tags = c.DiscussionWithTags.Select(dt => new
                        {
                            dt.DiscussionTag.ID,
                            dt.DiscussionTag.Title
                        }),
                        VoteDetails = c.DiscussionVotes.Where(dv => dv.DiscussionId == id && dv.UserId == userId).FirstOrDefault(),
                        c.CreatedAt
                    })
                    .FirstOrDefaultAsync();
                if(discussion == null)
                {
                    return NotFound(new { Message = "No discussion found" });
                }
                return Ok(discussion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting discussionid");
                return BadRequest();
            }
        }

        [HttpGet("GetAllDiscussionsByUser/{userId}")]
        public async Task<IActionResult> GetDiscussionsByUserCreated(int userId, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                paginationDto.Validate();
                var discussionsCount = await _context.Discussions.Where(c => c.UserId == userId).CountAsync(token);
                var discussions = await _context.Discussions
                    .AsSplitQuery()
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.CreatedAt)
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
                        c.Edited,
                        c.Votes,
                        User = c.PreferAnonimity == DiscussionAnonimityStatus.Visible ? new
                        {
                            c.User.Username,
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            c.User.ProfilePictureUrl,
                        } : null,
                        Tags = c.DiscussionWithTags.Select(dt => new
                        {
                            dt.DiscussionTag.ID,
                            dt.DiscussionTag.Title
                        }),
                        c.CreatedAt
                    })
                    .ToListAsync(token);

                if(discussions.Count == 0)
                {
                    return NotFound(new { Message = "no discussions found" });
                }
                var hasMore = discussions.Count == paginationDto.Take && discussions.Count < discussionsCount;
                return Ok(new { discussionsCount, discussions, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error in retriving discussions created by user");
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiscussions([FromQuery] DiscussionSorterDto sortDto, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                paginationDto.Validate();
                var query = _context.Discussions.AsNoTracking().AsQueryable().AsSplitQuery();
                switch (sortDto.SortBy)
                {
                    case DiscussionSortOptions.Latest:
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case DiscussionSortOptions.Active:
                        query = query.Where(c => c.DiscussionAnswers.Count > 0);
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case DiscussionSortOptions.Urgent:
                        query = query.Where(c => c.IsUrgent == true);
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    case DiscussionSortOptions.NoAnswers:
                        query = query.Where(c => c.DiscussionAnswers.Count == 0);
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                    default:
                        query = query.OrderByDescending(c => c.CreatedAt);
                        break;
                }
                //using var transation = await _context.Database.BeginTransactionAsync(token);
                var allDiscussions = await query.CountAsync(token);
                    var discussions = await query
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
                        c.Edited,
                        c.Votes,
                        User = c.PreferAnonimity == DiscussionAnonimityStatus.Visible ? new
                        {
                            c.User.Username,
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            c.User.ProfilePictureUrl,
                        } : null,
                        Tags = c.DiscussionWithTags.Select(dt => new
                        {
                            dt.DiscussionTag.ID,
                            dt.DiscussionTag.Title
                        }),
                        c.CreatedAt
                    })
                    .ToListAsync(token);

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
        public async Task<IActionResult> GetTypingTags([FromQuery] string? title)
        {
            try
            {
                var allTagsQuery = _context.DiscussionTags.AsNoTracking().AsQueryable();

                List<object> allTags;
                if (string.IsNullOrEmpty(title))
                {
                    allTags = await allTagsQuery
                        .Select(c => new
                        {
                            c.ID,
                            c.Title
                        })
                        .ToListAsync<object>();
                }
                else
                {
                    allTags = await allTagsQuery
                        .Where(c => EF.Functions.Contains(c.Title, $"\"{title}*\""))
                        .Select(c => new { c.Title, c.ID })
                        .ToListAsync<object>();
                }
                
                if(allTags.Count == 0)
                {
                    return NotFound(new { Message = "No tags found" });
                }
                return Ok(allTags);
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
                    IsUrgent = discussionDto.IsUrgent,
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
