using AutoMapper;
using BCrypt.Net;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Validators;
using FluentValidation;
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
        private readonly IValidator<UpdateUser> _userValidator;

        public UsersController(IRepository<Users> userRepository,
                               IRepository<Usermeta> usermetaRepository,
                               IWebHostEnvironment environment,
                               ILogger<UsersController> logger,
                               ITokenService tokenService,
                               IRepository<Categories> categoryRepository,
                               IFileUploadService fileUploadService,
                               IMapper mapper,
                               IValidator<UpdateUser> userValidator
                               )
        {
            _fileUploadService = fileUploadService;
            _userValidator = userValidator;
            _userRepository = userRepository;
            _usermetaRepository = usermetaRepository;
            _environment = environment;
            _logger = logger;
            _tokenService = tokenService;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, CancellationToken cancToken)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return BadRequest(new { message = "Username and password are required." });
                }

                var checkUser = await _userRepository.GetAll().FirstOrDefaultAsync(u => u.Username == loginDto.Username);
                if (checkUser == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, checkUser.Password))
                {
                    return Unauthorized(new { Message = "Login incorrect!" });
                }

                var user = await _userRepository.GetAll()
                .Include(u => u.UserMeta)
                .Include(u => u.Package)
                .Include(u => u.Payment)
                .Select(user => new
                {
                    user.ID,
                    user.Firstname,
                    user.Lastname,
                    user.Email,
                    user.Package,
                    user.UserMeta,
                    user.Payment,
                    user.Username,
                    user.ProfilePictureUrl
                })
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username, cancToken);

                var token = await _tokenService.GenerateTokens(user.ID.ToString(), cancToken);

                return Ok(new { Token = token, userdata = user });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in logging user in");
                return BadRequest("Error in logging in");
            }
              
        }


        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var userToken = await _tokenService.ValidateRefreshTokenAsync(request.Token, cancellationToken);
                if (userToken == null)
                {
                    return Unauthorized();
                }
                // Invalidate the old refresh token
                await _tokenService.InvalidateRefreshToken(request.Token, cancellationToken);

                // Generate new tokens
                var tokens = await _tokenService.GenerateTokens(userToken, cancellationToken);
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in refreshing token");
                return BadRequest(new { Message = "Error in refreshing" });
            }
            // Validate the refresh token
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
        //[Authorize(Roles = "Admin")]
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
        //[Authorize(Roles = "Admin")]
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
       // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromQuery] string? type, [FromBody] UpdateUser userDto)
        {

            try
            {
                var user = await _userRepository.Get(id, default);

                if (user == null)
                {
                    return NotFound();
                }

                if(type == "PasswordChange")
                {
                    var userValidator = await _userValidator.ValidateAsync(userDto);
                    if (!userValidator.IsValid)
                    {
                        return BadRequest(userValidator.Errors.Select(error => new
                        {
                            Field = error.PropertyName,
                            Error = error.ErrorMessage
                        }));
                    }
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
                    user.Password = hashedPassword;
                }


                if (!string.IsNullOrEmpty(userDto.Email) && user.Email != userDto.Email)
                {
                    user.Email = userDto.Email;
                }

                _mapper.Map(userDto, user);

                user.LastModified = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveAsync(default);
                return Ok(new {Message = "Data updated successfully!"});
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
        //[Authorize]
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
                string relativeUrl = await _fileUploadService.UploadFile(picDto.Base64Profile, FileCategory.Profile);
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
