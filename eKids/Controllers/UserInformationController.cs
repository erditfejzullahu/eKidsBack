using AutoMapper;
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
    public class UserInformationController : ControllerBase
    {
        private readonly IRepository<UserInformations> _userInformationRepository;
        private readonly IRepository<UserEducations> _userEducationRepository;
        private readonly IRepository<UserJobs> _userJobsRepository;
        private readonly ILogger<UserInformationController> _logger;
        private readonly IRepository<Users> _userRepository;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationsHub> _notificationsHub;
        //private readonly ApplicationDbContext _context;

        public UserInformationController(
            IHubContext<NotificationsHub> notificationsHub,
            ApplicationDbContext context,
            IRepository<UserInformations> userInformationRepository,
            IRepository<Users> userRepository,
            ILogger<UserInformationController> logger,
            IMapper mapper,
            IRepository<UserEducations> userEducationRepository,
            IRepository<UserJobs> userJobsRepository
            //ApplicationDbContext context
            )
        {
            _notificationsHub = notificationsHub;
            _context = context;
            _userInformationRepository = userInformationRepository;
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _userEducationRepository = userEducationRepository;
            _userJobsRepository = userJobsRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("AdminGet")]
        public async Task<IActionResult> GetUserInformationsAdmin([FromQuery] int userId, CancellationToken token)
        {
            try
            {
                var userInformation = await _context.UserInformations
                    .AsNoTracking()
                    .Include(c => c.UserJobs)
                    .Include(c => c.UserEducations)
                    .AsSplitQuery()
                    .Where(c => c.UserId == userId)
                    .FirstOrDefaultAsync(token);
                if (userInformation == null)
                {
                    return NotFound(new { Message = "userinformation not found" });
                }
                return Ok(userInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving user information for userID: {userId}");
                return BadRequest(new { Message = "Error in retriving user information" });
            }
        }

        [Authorize]
        [HttpGet]
        [ResponseCache(Duration = 30)]
        public async Task<IActionResult> GetUserInformations(CancellationToken token)
        {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
            try
            {

                var userInformation = await _context.UserInformations
                    .AsNoTracking()
                    .Include(c => c.UserJobs)
                    .Include(c => c.UserEducations)
                    .AsSplitQuery()
                    .Where(c => c.UserId == userId)
                    .FirstOrDefaultAsync(token);
                if(userInformation == null){
                    return NotFound(new { Message = "userinformation not found" });
                }
                return Ok(userInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving user information for userID: {userId}");
                return BadRequest(new { Message = "Error in retriving user information" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateUserInformation(UserInformationsDto infoDto, CancellationToken token)
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

                var userInformation = new UserInformations
                {
                    UserId = userId,
                    Birthday = infoDto.Birthday,
                    SoftSkills = infoDto.SoftSkills,
                    Profession = infoDto.Profession,
                    Skills = infoDto.Skills,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.UserInformations.AddAsync(userInformation);

                if(infoDto.UserJobs.Count != 0)
                {
                    var usersJob = infoDto.UserJobs.Select(c => new UserJobs
                    {
                        Job_Place = c.Job_Place,
                        Job_Title = c.Job_Title,
                        Start_Year = c.Start_Year,
                        End_Year = c.End_Year,
                        UserInformationId = userInformation.ID,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                    await _context.UserJobs.AddRangeAsync(usersJob, token);
                }

                if(infoDto.UserEducations.Count != 0)
                {
                    var usersEducation = infoDto.UserEducations.Select(c => new UserEducations
                    {
                        Place_Name = c.Place_Name,
                        School_Degree = c.SchoolDegree,
                        Field = c.Field,
                        Start_Year = c.Start_Year,
                        End_Year = c.End_Year,
                        UserInformationId = userInformation.ID,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                    await _context.UserEducations.AddRangeAsync(usersEducation, token);
                }
                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var newNotification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi krijimin e informacioneve shtese tuaja personale ne llogarine tuaj me date {DateTime.Now.ToString("f", albanianCulture)}",
                    IsRead = false,
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(newNotification);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if(connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.UserId == userId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new { Message = "Success in adding user information" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in creating userInformation");
                return BadRequest(new { Message = "Error in creating user information" });
            }
        }

        [Authorize]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUserInformation(int id, UserInformationsDto infoDto, CancellationToken token)
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

                var userInformation = await _context.UserInformations.Include(c => c.UserEducations).Include(c => c.UserJobs).Where(c => c.ID == id).FirstOrDefaultAsync(token);
                if (userInformation == null)
                {
                    return NotFound(new { Message = "No user information found" });
                }

                if(userInformation.UserId != userId)
                {
                    return Forbid();
                }

                if (infoDto.Birthday.HasValue)
                {
                    userInformation.Birthday = infoDto.Birthday;
                }
                _mapper.Map(infoDto, userInformation);
                userInformation.LastModified = DateTime.UtcNow;
                _context.UserInformations.Update(userInformation);

                if (infoDto.UserJobs != null)
                {
                    foreach (var jobDto in infoDto.UserJobs)
                    {
                        var userJob = userInformation.UserJobs.FirstOrDefault(uj => uj.ID == jobDto.ID);
                        if(userJob == null)
                        {
                            var newJob = new UserJobs
                            {
                                Job_Place = jobDto.Job_Place,
                                Job_Title = jobDto.Job_Title,
                                Start_Year = jobDto.Start_Year,
                                End_Year = jobDto.End_Year,
                                UserInformationId = userInformation.ID,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };
                            await _context.UserJobs.AddAsync(newJob);
                        }
                        else
                        {
                            _mapper.Map(jobDto, userJob);
                            _context.UserJobs.Update(userJob);
                        }
                    }
                }

                if(infoDto.UserEducations != null)
                {
                    foreach (var educationDto in infoDto.UserEducations)
                    {
                        var userEducation = userInformation.UserEducations.FirstOrDefault(ue => ue.ID == educationDto.ID);
                        if(userEducation == null)
                        {
                            var newEducation = new UserEducations
                            {
                                Place_Name = educationDto.Place_Name,
                                School_Degree = educationDto.SchoolDegree,
                                Field = educationDto.Field,
                                Start_Year = educationDto.Start_Year,
                                End_Year = educationDto.End_Year,
                                UserInformationId = userInformation.ID,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };
                            await _context.UserEducations.AddAsync(newEducation);
                        }
                        else
                        {
                            _mapper.Map(educationDto, userEducation);
                            _context.UserEducations.Update(userEducation);
                        }
                    }
                }

                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi rifreskimin e informacioneve tuaja personale ne llogarine tuaj me {DateTime.Now.ToString("f", albanianCulture)}",
                    IsRead = false,
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.UserId == userId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }

                return Ok(new { Message = "Data is updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, $"Error in updating userInformation with ID: {id}");
                return BadRequest(new { Message = "Error in updating user information" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteAdmin/{id}")]
        public async Task<IActionResult> DeleteUserInformationAdmin(int id, CancellationToken token)
        {
            try
            {
                var userInformation = await _context.UserInformations.FirstOrDefaultAsync(c => c.ID == id);
                if (userInformation == null)
                {
                    return NotFound(new { Message = "user information not found" });
                }

                _context.UserInformations.Remove(userInformation);
                await _context.SaveChangesAsync(token);
                return Ok(new { Message = "Deleted userinformation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting user information with id: {id}");
                return BadRequest(new { Message = "Error in deleting user information" });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserInformation(int id, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var userInformation = await _context.UserInformations.FirstOrDefaultAsync(c => c.ID == id);
                if(userInformation == null)
                {
                    return NotFound(new { Message = "user information not found" });
                }
                if (userInformation.UserId != userId)
                {
                    return Forbid();
                }
                _context.UserInformations.Remove(userInformation);

                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userId,
                    Information = $"Njoftim mbi heqjen e informacioneve tuaja personale ne llogarine tuaj me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification);

                await _context.SaveChangesAsync(token);
                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if (connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.UserId == userId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    }
                }
                return Ok(new { Message = "Deleted userinformation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting user information with id: {id}");
                return BadRequest(new { Message = "Error in deleting user information" });
            }
        }
    }
}
