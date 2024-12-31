using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserFriendsController : ControllerBase
    {
        private readonly IRepository<CloseFriends> _closeRepository;
        private readonly IRepository<Users> _usersRepository;
        private readonly IRepository<Friends> _friendsRepository;
        private readonly IRepository<Friendships> _friendShipsRepository;

        private readonly ILogger<UserFriendsController> _logger;

        public UserFriendsController(
            IRepository<CloseFriends> closeRepository,
            ILogger<UserFriendsController> logger,
            IRepository<Users> usersRepository,
            IRepository<Friends> friendsRepository)
        {
            _closeRepository = closeRepository;
            _logger = logger;
            _usersRepository = usersRepository;
            _friendsRepository = friendsRepository;
        }

        [HttpPost("/api/UserFriends/MakeCloseFriend")]
        public async Task<IActionResult> MakeBestFriend([FromBody] CloseFriendDto friendDto, CancellationToken token)
        {
            try
            {
                if(friendDto == null)
                {
                    return BadRequest(new { Message = "Data missing" });
                }

                var closeFriend = new CloseFriends
                {
                    UserId = friendDto.UserId,
                    CloseFriendId = friendDto.CloseFriendId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                _closeRepository.Add(closeFriend);
                await _closeRepository.SaveAsync(token);
                return Ok(closeFriend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making close friend");
                return BadRequest(new { Message = "Error in making close friend" });
            }
        }

        [HttpPost("/api/UserFriends/MakeFriend")]
        public async Task<IActionResult> MakeFriend(FriendDto friendDto, CancellationToken token)
        {
            try
            {
                if(friendDto == null)
                {
                    return BadRequest(new { Message = "No data" });
                }
                var friend = new Friends
                {
                    UserId = friendDto.UserId,
                    FriendId = friendDto.FriendId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _friendsRepository.Add(friend);
                await _friendsRepository.SaveAsync(token);
                return Ok(friend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making friend");
                return BadRequest(new { Message = "Error in making friend" });
            }

        }

        [HttpGet("/api/UserFriends/GetAllByUser/{userId}")]
        public async Task<IActionResult> GetAllCloseFriends(int userId, [FromQuery] UsersRelationshipTypes types, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                IQueryable<object>? result = null;

                switch (types)
                {
                    case UsersRelationshipTypes.All:
                        result = _usersRepository
                            .GetAll()
                            .AsNoTracking()
                            .Select(c => new
                            {
                                c.ID,
                                c.Firstname,
                                c.Lastname,
                                c.Email,
                                c.Age,
                                c.ProfilePictureUrl,
                                c.CreatedAt,
                                c.Username
                            });
                        break;
                    case UsersRelationshipTypes.Friends:
                        result = _friendsRepository
                            .GetAll()
                            .AsNoTracking()
                            .Where(c => c.UserId == userId)
                            .Include(c => c.Friend)
                            .Select(c => new
                            {
                                c.ID,
                                c.UserId,
                                c.FriendId,
                                c.CreatedAt,
                                UserData = new
                                {
                                    c.Friend.ID,
                                    c.Friend.Firstname,
                                    c.Friend.Lastname,
                                    c.Friend.Email,
                                    c.Friend.Age,
                                    c.Friend.ProfilePictureUrl,
                                    c.Friend.CreatedAt,
                                    c.Friend.Username,
                                }
                            });                            
                        break;
                    case UsersRelationshipTypes.CloseFriends:
                        result = _closeRepository
                            .GetAll()
                            .AsNoTracking()
                            .Where(c => c.UserId == userId)
                            .Include(c => c.CloseFriend)
                            .Select(c => new
                            {
                                c.ID,
                                c.User,
                                c.CloseFriend,
                                c.CreatedAt,
                                UserData = new
                                {
                                    c.CloseFriend.ID,
                                    c.CloseFriend.Firstname,
                                    c.CloseFriend.Lastname,
                                    c.CloseFriend.Email,
                                    c.CloseFriend.Age,
                                    c.CloseFriend.ProfilePictureUrl,
                                    c.CloseFriend.CreatedAt,
                                    c.CloseFriend.Username,
                                }
                            });
                        break;
                    default:
                        break;
                }
                var paginatedQuery = result.Skip(paginationDto.Skip).Take(paginationDto.Take);
                if(await paginatedQuery.AnyAsync(token))
                {
                    var response = await paginatedQuery.ToListAsync(token);
                    return Ok(response);
                }
                else
                {
                    return NotFound(new { Message = "No data found" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving all close friends with user id: {userId}");
                return BadRequest(new { Message = "Error in retriving close friends" });
            }
        }


        [HttpDelete("/api/UserFriends/DeleteCloseFriend/{id}")]
        public async Task<IActionResult> DeleteCloseFriend(int id, CancellationToken token)
        {
            try
            {
                var closefriend = await _closeRepository.Get(id, token);
                if(closefriend == null)
                {
                    return NotFound(new { Message = "CloseFriend not found" });
                }
                await _closeRepository.Delete(closefriend.ID, token);
                await _closeRepository.SaveAsync(token);
                return Ok(new { Message = "Close friend deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting close friend with column ID: {id}");
                return BadRequest(new { Message = "Error in deleting close friend" });
            }
        }

        [HttpDelete("/api/UserFriends/DeleteFriend/{id}")]
        public async Task<IActionResult> DeleteFriend(int id, CancellationToken token)
        {
            try
            {
                var friend = await _friendsRepository.Get(id, token);
                if(friend == null)
                {
                    return NotFound(new { Message = "No friend found" });
                }
                await _friendsRepository.Delete(friend.ID, token);
                await _friendsRepository.SaveAsync(token);
                return Ok(new { Message = "Friend Deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting friend with id: {id}");
                return BadRequest(new { Message = "Error deleting friend" });
            }
        }
    }
}
