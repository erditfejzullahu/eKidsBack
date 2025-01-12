using AutoMapper;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public UserInformationController(
            IRepository<UserInformations> userInformationRepository,
            IRepository<Users> userRepository,
            ILogger<UserInformationController> logger,
            IMapper mapper,
            IRepository<UserEducations> userEducationRepository,
            IRepository<UserJobs> userJobsRepository)
        {
            _userInformationRepository = userInformationRepository;
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _userEducationRepository = userEducationRepository;
            _userJobsRepository = userJobsRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInformations([FromQuery] int userId, CancellationToken token)
        {
            try
            {
                var userInformation = await _userInformationRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, token);
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

        [HttpPost]
        public async Task<IActionResult> CreateUserInformation(UserInformationsDto infoDto, CancellationToken token)
        {
            try
            {
                var userInformation = new UserInformations
                {
                    UserId = infoDto.UserId,
                    Birthday = infoDto.Birthday,
                    SoftSkills = infoDto.SoftSkills,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                _userInformationRepository.Add(userInformation);
                await _userInformationRepository.SaveAsync(token);

                foreach (var jobInfo in infoDto.UserJobs)
                {
                    var userJob = new UserJobs
                    {
                        Job_Place = jobInfo.Job_Place,
                        Job_Title = jobInfo.Job_Title,
                        Start_Year = jobInfo.Start_Year.Value,
                        End_Year = jobInfo.End_Year,
                        UserInformationId = userInformation.ID,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    _userJobsRepository.Add(userJob);
                    await _userJobsRepository.SaveAsync(token);
                }

                foreach (var educationInfo in infoDto.UserEducations)
                {
                    var userEducations = new UserEducations
                    {
                        Place_Name = educationInfo.Place_Name,
                        School_Degree = educationInfo.SchoolDegree,
                        Field = educationInfo.Field,
                        Start_Year = educationInfo.Start_Year.Value,
                        End_Year = educationInfo.End_Year,
                        UserInformationId = userInformation.ID,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    _userEducationRepository.Add(userEducations);
                    await _userEducationRepository.SaveAsync(token);
                }

                return Ok(new { Message = "Success in adding user information" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating userInformation");
                return BadRequest(new { Message = "Error in creating user information" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateUserInformation(int id, UserInformationsDto infoDto, CancellationToken token)
        {
            try
            {
                var userInformation = await _userInformationRepository.Get(id, token);
                if (userInformation == null)
                {
                    return NotFound(new { Message = "No user information found" });
                }
                _mapper.Map(infoDto, userInformation);
                userInformation.LastModified = DateTime.UtcNow;
                _userInformationRepository.Update(userInformation);
                await _userInformationRepository.SaveAsync(token);
                return Ok(new { Message = "Data is updated successfully" });
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating userInformation with ID: {id}");
                return BadRequest(new { Message = "Error in updating user information" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserInformation(int id, CancellationToken token)
        {
            try
            {
                var userInformation = await _userInformationRepository.Get(id, token);
                if(userInformation == null)
                {
                    return NotFound(new { Message = "user information not found" });
                }
                await _userInformationRepository.Delete(userInformation.ID, token);
                await _userInformationRepository.SaveAsync(token);
                return Ok(new { Message = "Error in deleting userinformation" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting user information with id: {id}");
                return BadRequest(new { Message = "Error in deleting user information" });
            }
        }
    }
}
