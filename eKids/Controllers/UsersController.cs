using AutoMapper;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IRepository<Users> _userRepository;
        private readonly IRepository<Usermeta> _usermetaRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UsersController> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRepository<Categories> _categoryRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly IMapper _mapper;

        public UsersController(IRepository<Users> userRepository,
                               IRepository<Usermeta> usermetaRepository,
                               IWebHostEnvironment environment,
                               ILogger<UsersController> logger,
                               ITokenService tokenService,
                               IRepository<Categories> categoryRepository,
                               IFileUploadService fileUploadService,
                               IMapper mapper
                               )
        {
            _fileUploadService = fileUploadService;
            _userRepository = userRepository;
            _usermetaRepository = usermetaRepository;
            _environment = environment;
            _logger = logger;
            _tokenService = tokenService;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.Username == loginDto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password)) {
                return Unauthorized(new { message = "Login details incorrect!" });
            }

            var token = _tokenService.GenerateTokens(user.ID.ToString());

            var userData = await _userRepository.GetAll()
                .Include(i => i.UserMeta)
                .Include(u => u.Package)
                .Include(a => a.Payment)
                .Select(user => new
                {
                    user.ID,
                    user.Firstname,
                    user.Lastname,
                    user.Email,
                    user.Package,
                    user.UserMeta,
                    user.Payment,
                    user.Username
                })
                .FirstOrDefaultAsync(id => id.ID == user.ID);

            return Ok(new { Token = token, userdata = userData });
              
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            // Validate the refresh token
            var userToken = _tokenService.ValidateRefreshToken(request.Token);
            if (userToken == null)
            {
                return Unauthorized();
            }

            // Invalidate the old refresh token
            await _tokenService.InvalidateRefreshToken(request.Token, cancellationToken);

            // Generate new tokens
            var tokens = _tokenService.GenerateTokens(userToken);
            return Ok(tokens);
        }


        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUser userDto)
        {
            if (userDto == null)
            {
                return BadRequest("User data is null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var user = new Users
            {
                Firstname = userDto.Firstname,
                Lastname = userDto.Lastname,
                Username = userDto.Username,
                Password = hashedPassword,
                Email = userDto.Email,
                Age = userDto.Age,
                PackageID = 1, // LOGIC: sepse 1shi osht free e kur osht 1 ka access ne do gjera dhe del paketa per pages ne intervale kohore
                ProfilePictureUrl = userDto.ProfilePictureUrl,
                Role = "Student",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

             _userRepository.Add(user);
            await _userRepository.SaveAsync(default);

            var userMetaList = new List<Usermeta>
            {
                new Usermeta { UserID = user.ID, MetaKey = "UserRole", MetaValue = "Student" },
                new Usermeta { UserID = user.ID, MetaKey = "Nickname", MetaValue =  $"{user.Firstname} {user.Lastname}" },
                new Usermeta { UserID = user.ID, MetaKey = "ColorsPreferred", MetaValue = "Light"},
                new Usermeta { UserID = user.ID, MetaKey = "Phone", MetaValue = "Pa Numer"},
                new Usermeta { UserID = user.ID, MetaKey = "LessonsCompleted", MetaValue = "0"},
                new Usermeta { UserID = user.ID, MetaKey = "PreferredLearningCategory", MetaValue = "undefined"}
            };

            foreach (var specifiedMeta in userMetaList)
            {
                _usermetaRepository.Add(specifiedMeta);
            }

            await _usermetaRepository.SaveAsync(default);

            return Ok(user);

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userRepository.Get(id, default);
            if(user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }


        [HttpGet("info/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllInfo(int id, CancellationToken token)
        {

            var getUser = await _userRepository.Get(id, token);

            if (getUser == null)
            {
                return NotFound();
            }

            var user = await _userRepository.GetAll()
                                    .Include(i => i.UserMeta)
                                    .Include(u => u.Payment)
                                    .Include(a => a.Package)
                                    .Select(getUser => new
                                    {
                                        getUser.ID,
                                        getUser.Firstname,
                                        getUser.Lastname,
                                        getUser.Email,
                                        getUser.Age,
                                        getUser.Package,
                                        getUser.UserMeta,
                                        getUser.Payment,
                                        getUser.Username, 
                                        getUser.ProfilePictureUrl
                                    })
                                    .FirstOrDefaultAsync(u => u.ID == getUser.ID, token);
                                    

            

            var categories = await _categoryRepository.GetAll().ToListAsync();

            var response = new
            {
                Data = new
                {
                    UserData = user,
                    Categories = categories
                }
            };
            return Ok(response);
            
        }

        [HttpGet("allUsers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers(CancellationToken token)
        {
            var users = await _userRepository.GetAll()
                .Select(users => new 
                {
                    users.Firstname,
                    users.Lastname, 
                    users.Email,
                    users.Role,
                    users.Age,
                    users.ProfilePictureUrl,
                    users.Package,
                    users.UserMeta,
                    users.Payment,
                    users.ID,
                    users.Username
                })
                .ToListAsync(token);
            if(users == null)
            {
                return BadRequest("No users or smth error");
            }
            return Ok(users);
        }
 
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUser userDto)
        {

            try
            {
                var user = await _userRepository.Get(id, default);

                if (user == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
                    user.Password = hashedPassword;
                }

                if(!string.IsNullOrEmpty(userDto.Email))
                {
                    //LOGIC FOR EMAIL VERIFICATION THEN UPDATE
                    user.Email = userDto.Email;
                }

                _mapper.Map(userDto, user);

                user.LastModified = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveAsync(default);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating user with ID: {id}");
                var errorMessage = new
                {
                    Message = "Error updating data!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id, CancellationToken token)
        {
            var user = await _userRepository.Get(id, default);
            
            if(user == null)
            {
                return NotFound();
            }

            await _userRepository.Delete(user.ID, token);
            await _userRepository.SaveAsync(default);

            return Ok(user);
        }

        [HttpPut("{id}/profile-picture")]
        [Authorize]
        public async Task<IActionResult> UpdatePicture(int id, [FromBody] UpdateProfilePic picDto)
        {


            if (picDto == null || string.IsNullOrEmpty(picDto.Base64Profile))
            {
                return BadRequest("Missing base64data");
            }

            var user = await _userRepository.Get(id, default);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                string relativeUrl = await _fileUploadService.UploadFile(user.ProfilePictureUrl, FileCategory.Profile);
                var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

                user.ProfilePictureUrl = url;
                user.LastModified = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveAsync(default);

                return Ok(new { FileUrl = url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", id);
                return StatusCode(500, "Internal server error while updating profile picture.");
            }
    
        }

        [HttpPut("{id}/package")]
        public async Task<IActionResult> UpdateUserPackage(int id, [FromForm] UpdateUserPackageID packageDto)
        {
            var user = await _userRepository.Get(id, default);
            if (user == null)
            {
                return NotFound();
               
            }

            try
            {
                user.PackageID = packageDto.PackageID; 
                user.LastModified = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveAsync(default);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating package with user id {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error updating package!" });
            }

        }


    }
}
