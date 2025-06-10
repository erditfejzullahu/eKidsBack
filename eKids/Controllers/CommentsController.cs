using Database.DTOs;
using Database.Models;
using Database.Repository;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Xml.Linq;

namespace eKids.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CommentsController : Controller
    {

        private readonly IRepository<Comments> _commentsRepository;
        private readonly ILogger<CommentsController> _logger;
        private readonly ICommentService _commentService;
        private readonly ICommentLikesService _commentLikesService;

        public CommentsController(IRepository<Comments> commentsRepository, ILogger<CommentsController> logger, ICommentService commentService, ICommentLikesService commentLikesService)
        {
            _commentsRepository = commentsRepository;
            _logger = logger;
            _commentService = commentService;
            _commentLikesService = commentLikesService; 
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateComments commentsDto, CancellationToken token)
        {
            if (commentsDto == null)
            {
                return BadRequest(new { Message = "Comment data is missing" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid comment data", Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
            }

            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var sanitizer = new HtmlSanitizer();
                var newComment = new Comments
                {
                    LessonId = commentsDto.LessonId,
                    ParentId = commentsDto.ParentId,
                    UserId = userId,
                    Comment_Content = sanitizer.Sanitize(commentsDto.Comment_Content.Trim()),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _commentsRepository.Add(newComment);
                await _commentsRepository.SaveAsync(token);

                return Ok(newComment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating comment");
                return StatusCode(500, new { Message = "An error occurred while creating the comment" });
            }
        }

        [Authorize]
        [HttpGet("/api/Comments/GetAll/{id}")]
        public async Task<IActionResult> GetAllComments(int id, [FromQuery] string type, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(type) || (type != "lesson" && type != "user"))
                {
                    return BadRequest(new {Message="Type error"});
                }

                var allComments = await _commentService.GetCommentsAsync(id, type, userId);

                if (allComments.Count == 0)
                {
                    return BadRequest(new { Message = "No comments made!" });
                }

                return Ok(allComments);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retrieving all comments");
                return BadRequest(new { Message = "Error in retrieving comments!" });
            }
        }

        [Authorize]
        [HttpPatch("/api/Comments/Like/{id}")]
        public async Task<IActionResult> UpdateLike(int id, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var comment = await _commentsRepository.Get(id, token);

                if(comment == null)
                {
                    return BadRequest(new { Message = "No comments found!" });
                }

                var userLike = await _commentLikesService.GetUserCommentLikeAsync(id, userId, token);

                if(userLike != null)
                {
                    await _commentLikesService.RemoveUserLikeAsync(userLike, token);
                    comment.Likes = Math.Max(0, comment.Likes - 1);
                    await _commentsRepository.SaveAsync(token);
                    return Ok(new { Message = "Like removed", comment.Likes });

                }
                else
                {
                    await _commentLikesService.AddUserLikeAsync(id, userId, token);
                    comment.Likes += 1;
                    await _commentsRepository.SaveAsync(token);
                    return Ok(new { Message = "Like added", comment.Likes });

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro in updating like with commentID: {id}");
                return BadRequest(new { Message = "Error updating like" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("AdminDelete/{id}")]
        public async Task<IActionResult> DeleteCommentAdmin(int id, CancellationToken token)
        {
            try
            {
                var comment = await _commentsRepository.Get(id, token);
                if (comment == null)
                {
                    return BadRequest(new { Message = "No comment found!" });
                }

                await _commentsRepository.Delete(comment.ID, token);
                await _commentsRepository.SaveAsync(token);

                return Ok(new { Message = "Comment deleted!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting comment wit ID: {id}");
                return BadRequest(new { Message = "Error deleting comment!" });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var comment = await _commentsRepository.Get(id, token);
                if(comment == null)
                {
                    return BadRequest(new { Message = "No comment found!" });
                }

                if(comment.UserId != userId)
                {
                    return Forbid();
                }

                await _commentsRepository.Delete(comment.ID, token);
                await _commentsRepository.SaveAsync(token);

                return Ok(new { Message = "Comment deleted!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting comment wit ID: {id}");
                return BadRequest(new { Message = "Error deleting comment!" });
            }
        }
    }
}
