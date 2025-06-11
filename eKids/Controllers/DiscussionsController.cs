using AutoMapper;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using eKids.Hubs;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;

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
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public DiscussionsController(IFileUploadService fileUploadService, IHubContext<NotificationsHub> notificationsHub, IMapper mapper, ILogger<DiscussionsController> logger, ApplicationDbContext context, IDiscussionAnswerService discussionAnswerService)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
            _context = context;
            _mapper = mapper;
            _discussionAnswerService = discussionAnswerService;
            _notificationsHub = notificationsHub;
        }

        [Authorize]
        [HttpPost("CreateDiscussionAnswer")]
        public async Task<IActionResult> CreateDiscussionAnswer([FromBody] CreateDiscussionAnswerDto createDiscussionAnswer, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Model invalid" });
                }
                var getDiscussion = await _context.Discussions.AsNoTracking().Where(c => c.ID == createDiscussionAnswer.DiscussionId).FirstOrDefaultAsync(token);
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
                var sanitizer = new HtmlSanitizer();
                
                sanitizer.AllowedTags.Add("b");        // bold
                sanitizer.AllowedTags.Add("i");        // italic
                sanitizer.AllowedTags.Add("u");        // underline
                sanitizer.AllowedTags.Add("p");        // paragraphs
                sanitizer.AllowedTags.Add("br");       // line breaks
                sanitizer.AllowedTags.Add("ul");       // unordered lists
                sanitizer.AllowedTags.Add("ol");       // ordered lists
                sanitizer.AllowedTags.Add("li");       // list items
                sanitizer.AllowedTags.Add("strong");   // strong emphasis
                sanitizer.AllowedTags.Add("em");       // emphasis
                sanitizer.AllowedTags.Add("blockquote"); // quotes

                
                sanitizer.AllowedAttributes.Add("style"); // For basic styling
                sanitizer.AllowedAttributes.Add("class");

                
                sanitizer.AllowedTags.Add("a");
                sanitizer.AllowedAttributes.Add("href");
                sanitizer.AllowDataAttributes = false; // Disallow data-* attributes

                
                sanitizer.AllowedSchemes.Add("http");
                sanitizer.AllowedSchemes.Add("https");
                sanitizer.AllowedSchemes.Add("mailto");
                sanitizer.AllowedSchemes.Add("h1");
                sanitizer.AllowedSchemes.Add("h2");
                sanitizer.AllowedSchemes.Add("h3");
                sanitizer.AllowedSchemes.Add("h4");
                sanitizer.AllowedSchemes.Add("h5");
                sanitizer.AllowedTags.Add("code");
                sanitizer.AllowedTags.Add("pre");

                var createAnswer = new DiscussionAnswers
                {
                    Content = sanitizer.Sanitize(createDiscussionAnswer.DiscussionAnswerContent.Trim()),
                    UserId = userId,
                    DiscussionId = getDiscussion.ID,
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

        [Authorize]
        [HttpPatch("HandleDiscussionVotes")]
        public async Task<IActionResult> HandleDiscussionVotes([FromBody] DiscussionHandleVoteDto discussionHandleVoteDto, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new {Message = "Model invalid"});
                }

                var handleVote = await _discussionAnswerService.HandleDiscussionVoteStatusAsync(userId, discussionHandleVoteDto.DiscussionId, discussionHandleVoteDto.DiscussionVoteType, token);
                return Ok(new { VoteResponse = handleVote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handling discussion vote");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPatch("HandleAnswerVotes")]
        public async Task<IActionResult> HandleAnswerVotes([FromBody] DiscussionAnswerHandleVoteDto handleVoteDto, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Model invalid" });
                }
                var handleVote = await _discussionAnswerService.HandleAnswerVoteStatusAsync(userId, handleVoteDto.DiscussionAnswerId, handleVoteDto.DiscussionId, handleVoteDto.DiscussionVoteType, token);
                return Ok(new { VoteResponse = handleVote }); //0 for voteup, 1 for votedown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling comment votes");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetDiscussionComments/{id}")]
        public async Task<IActionResult> GetDiscussionComments(int id, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
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

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDiscussionById(int id)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
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
                var query = _context.Discussions.Where(c => c.UserId == userId).AsNoTracking();
                var discussionsCount = await query.CountAsync(token);
                var discussions = await query
                    //.AsSplitQuery()
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
                var hasMore = paginationDto.Skip + discussions.Count < discussionsCount;
                return Ok(new { discussionsCount, discussions, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error in retriving discussions created by user");
                return BadRequest();
            }
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateDiscussion([FromBody] DiscussionDto discussionDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new {Message = "Model not valid"});
                }

                var sanitizer = new HtmlSanitizer();

                var discussion = new Discussions
                {
                    Title = sanitizer.Sanitize(discussionDto.Title.Trim()),
                    Content = sanitizer.Sanitize(discussionDto.Content),
                    UserId = userId,
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
                            Title = sanitizer.Sanitize(item.Title),
                            Description = sanitizer.Sanitize(item.Description),
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

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi krijimin e diskutimit {discussion.Title} me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if(connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && !c.IsRead).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }
                return Ok(new { Message = "Discussion Created" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);    
                _logger.LogError(ex, " Error in creating discussion");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPatch("{id}")]
        public async Task<IActionResult> EditDiscussion(int id, DiscussionDto discussionDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new {Message = "Model not valid"});
                }
                var discussion = await _context.Discussions
                    .Include(d => d.DiscussionWithTags)
                    .FirstOrDefaultAsync(d => d.ID == id, token);
                if (discussion == null)
                {
                    return NotFound(new { Message = "Discussion not found" });
                }
                if(discussion.UserId != userId)
                {
                    return Forbid();
                }

                var sanitizer = new HtmlSanitizer();

                var cleanDiscussionDto = new DiscussionDto
                {
                    Title = sanitizer.Sanitize(discussionDto.Title),
                    Content = sanitizer.Sanitize(discussionDto.Content),
                    UserId = userId,
                    IsUrgent = discussionDto.IsUrgent,
                    PreferAnonimity = discussionDto.PreferAnonimity,
                    Tags = discussionDto.Tags
                };

                _mapper.Map(cleanDiscussionDto, discussion);
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
                                Title = sanitizer.Sanitize(item.Title),
                                Description = sanitizer.Sanitize(item.Description),
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
                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi rifreskimin e diskutimit {discussion.Title} me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);


                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && !c.IsRead).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new { Message = "Discussion updated successfully" });

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in edditing discussion");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpPatch("changeAnonimity/{id}")]
        public async Task<IActionResult> ChangeAnonimity(int id, [FromQuery] DiscussionAnonimityStatus anonimityStatus, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }


                var discussion = await _context.Discussions.FindAsync(id, token);
                if(discussion == null)
                {
                    return NotFound(new {Message = "Discussion not found!"});
                }

                if(discussion.UserId != userId)
                {
                    return Forbid();
                }

                discussion.PreferAnonimity = anonimityStatus;
                discussion.LastModified = DateTime.UtcNow;

                _context.Discussions.Update(discussion);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi ndryshimin e anonimitetit te diskutimit {discussion.Title} me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && !c.IsRead).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new { Message = "Anonimity Changed" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in changing anonimity");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("AdminDelete/{id}")]
        public async Task<IActionResult> DeleteDiscussionAdmin(int id, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var discussion = await _context.Discussions.FindAsync(id, token);
                if (discussion == null)
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

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscussion(int id, CancellationToken token)
        {
            
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue("Username");
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var discussion = await _context.Discussions.FindAsync(id, token);
                if(discussion == null)
                {
                    return NotFound(new { Message = "No discussion found" });
                }

                if (discussion.UserId != userId)
                {
                    return Forbid();
                }

                _context.Discussions.Remove(discussion);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi heqjen e diskutimit {discussion.Title} me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && !c.IsRead).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

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
