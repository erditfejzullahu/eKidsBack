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


        public UsersController(IRepository<Users> userRepository, IRepository<Usermeta> usermetaRepository, IWebHostEnvironment environment, ILogger<UsersController> logger, ITokenService tokenService, IRepository<Categories> categoryRepository)
        {
            _userRepository = userRepository;
            _usermetaRepository = usermetaRepository;
            _environment = environment;
            _logger = logger;
            _tokenService = tokenService;
            _categoryRepository = categoryRepository;
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
                    user.Payment
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
                })
                .ToListAsync(token);
            if(users == null)
            {
                return BadRequest("No users or smth error");
            }
            return Ok(users);
        }
 
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUser userDto)
        {
            var user = await _userRepository.Get(id, default);
            
            if(user == null)
            {
                return NotFound();
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            user.Firstname = userDto.Firstname;
            user.Lastname = userDto.Lastname;
            user.Username = userDto.Username;
            user.Password = hashedPassword;
            user.Age = userDto.Age;
            user.LastModified = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveAsync(default);

            return Ok(user);
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

            var dataUriPattern = new Regex(@"^data:(?<mimeType>[\w/\-]+)(;charset=[\w\-]+)?(;base64)?,(?<data>.*)$");

            var match = dataUriPattern.Match(picDto.Base64Profile);
            if (!match.Success)
            {
                return BadRequest("Invalid base64 data.");
            }

            var mimeType = match.Groups["mimeType"].Value;
            var base64Data = match.Groups["data"].Value;

            var mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "image/jpeg", "jpg" },
                { "image/png", "png" },
                { "image/gif", "gif" },
                { "image/bmp", "bmp" },
                { "image/webp", "webp" }
            };

            if (!mimeTypeMappings.TryGetValue(mimeType, out string extension))
            {
                return BadRequest("Invalid file extension.");
            }

            string fileName = $"{Guid.NewGuid()}.{extension}";
            byte[] bytes = Convert.FromBase64String(base64Data);


            var uploadsFolderPath = Path.Combine(_environment.WebRootPath, "images/profiles");
            try
            {
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                string filePath = Path.Combine(uploadsFolderPath, fileName);
                System.IO.File.WriteAllBytes(filePath, bytes);
                var url = $"{Request.Scheme}://{Request.Host}/images/profiles/{fileName}";

                user.ProfilePictureUrl = url;
                user.LastModified = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveAsync(default);

                return Ok(new { FileUrl = url });
            }
            catch(Exception ex)
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

            user.PackageID = packageDto.PackageID;
            user.LastModified = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveAsync(default);

            return Ok(user);
        }


    }
}
