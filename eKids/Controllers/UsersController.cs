using AutoMapper;
using BCrypt.Net;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Validators;
using FluentValidation;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Globalization;
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
        private readonly IRepository<Courses> _courseRepository;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateUser> _userValidator;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordResetService _passwordResetService;

        public UsersController(IRepository<Users> userRepository,
                               IPasswordResetService passwordResetService,
                               IRepository<Courses> courseRepository,
                               IRepository<Usermeta> usermetaRepository,
                               IWebHostEnvironment environment,
                               ILogger<UsersController> logger,
                               ITokenService tokenService,
                               IRepository<Categories> categoryRepository,
                               IFileUploadService fileUploadService,
                               IMapper mapper,
                               IValidator<UpdateUser> userValidator,
                               ApplicationDbContext context
                               )
        {
            _passwordResetService = passwordResetService;
            _fileUploadService = fileUploadService;
            _context = context;
            _userValidator = userValidator;
            _userRepository = userRepository;
            _usermetaRepository = usermetaRepository;
            _environment = environment;
            _courseRepository = courseRepository;
            _logger = logger;
            _tokenService = tokenService;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("Check-Authorization")]
        public async Task<IActionResult> CheckAuthorization()
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                return Ok(new {Message = "Authorized"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no authorization");
                return BadRequest();
            }
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> PasswordForgot([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Model invalid" });
                }
                var response = new { message = "Nese emaili juaj egziston ne sistemin tone, do te merrni nje link te ndryshimit te fjalekalimit tuaj!" };
                var token = await _passwordResetService.GeneratePasswordResetTokenAsync(forgotPasswordDto.Email);
                if (token == null) return Ok(response);
                var resetLink = $"frontendUrl/reset-password?email={forgotPasswordDto.Email}&token={token}";

                //send via email...
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error creating password forget");
                return BadRequest();
            }
        }

        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromQuery] string email, [FromQuery] string token)
        {
            try
            {
                if(string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { Message = "Missing fields" });
                }

                var isValid = await _passwordResetService.ValidatePasswordResetTokenAsync(email, token);
                return Ok(new { valid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return BadRequest();
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDto resetDto, CancellationToken token)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Invalid data" });
                }
                var success = await _passwordResetService.ResetPasswordAsync(resetDto.Email, resetDto.Token, resetDto.NewPassword, token);
                if (!success)
                {
                    return BadRequest(new { Message = "Invalid or expired token" });
                }
                var user = await _context.Users.Where(c => c.Email == resetDto.Email).FirstOrDefaultAsync();
                if(user != null)
                {
                    CultureInfo albanianCulture = new CultureInfo("sq-AL");

                    var resetNotification = new Notifications
                    {
                        ReceiverId = user.ID,
                        Information = $"Njofim mbi rifreskimin e fjalekalimit tuaj me {DateTime.Now.ToString("f", albanianCulture)}",
                        Type = Shared.Enums.NotificationsType.PasswordReset,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    await _context.Notifications.AddAsync(resetNotification, token);
                    await _context.SaveChangesAsync(token);
                    await transaction.CommitAsync(token);
                }
                return Ok(new { Message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error resetting assword");
                return BadRequest();
            }
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

                var checkUser = await _userRepository
                    .GetAll()
                    .AsNoTracking()
                    .Include(u => u.UserMeta)
                    .Include(u => u.Payments)
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);
                if (checkUser == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, checkUser.Password))
                {
                    return Unauthorized(new { Message = "Login incorrect!" });
                }

                CultureInfo albanianCulture = new CultureInfo("sq-AL");

                var loginNotification = new Notifications
                {
                    ReceiverId = checkUser.ID,
                    Information = $"Njofim mbi kycjen ne llogarine tuaj me {DateTime.Now.ToString("f", albanianCulture)}",
                    Type = Shared.Enums.NotificationsType.LoginActivity,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(loginNotification, cancToken);

                var existingCommit = await _context.Commits.Where(c => c.UserId == checkUser.ID && c.Date == DateOnly.FromDateTime(DateTime.UtcNow.Date)).FirstOrDefaultAsync(cancToken);
                if (existingCommit == null)
                {
                    var newCommit = new Commits
                    {
                        UserId = checkUser.ID,
                        Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                        Count = 1,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                    };
                    await _context.Commits.AddAsync(newCommit, cancToken);
                }
                else
                {
                    existingCommit.Count += 1;
                    existingCommit.LastModified = DateTime.UtcNow;
                    _context.Commits.Update(existingCommit);
                }
                    await _context.SaveChangesAsync(cancToken);

                var response = new
                {
                    checkUser.ID,
                    checkUser.Firstname,
                    checkUser.Lastname,
                    checkUser.Email,
                    checkUser.UserMeta,
                    checkUser.Payments,
                    checkUser.Username,
                    checkUser.ProfilePictureUrl
                };

                //var user = await _userRepository.GetAll()
                //.Include(u => u.UserMeta)
                //.Include(u => u.Package)
                //.Include(u => u.Payment)
                //.Select(user => new
                //{
                //    user.ID,
                //    user.Firstname,
                //    user.Lastname,
                //    user.Email,
                //    user.Package,
                //    user.UserMeta,
                //    user.Payment,
                //    user.Username,
                //    user.ProfilePictureUrl
                //})
                //.FirstOrDefaultAsync(u => u.Username == loginDto.Username, cancToken);

                var token = await _tokenService.GenerateTokens(checkUser.ID.ToString(), cancToken);

                return Ok(new { Token = token, userdata = checkUser });

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

        private static string SanitizeEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address;
            }
            catch
            {
                throw new ArgumentException("Invalid email format");
            }
        }

        [HttpPost("/register")]
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
            var sanitize = new HtmlSanitizer();
            sanitize.AllowedTags.Clear();
            sanitize.AllowedAttributes.Clear();
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var user = new Users
            {
                Firstname = sanitize.Sanitize(userDto.Firstname.Trim()),
                Lastname = sanitize.Sanitize(userDto.Lastname.Trim()),
                Username = sanitize.Sanitize(userDto.Username.Trim()),
                Password = hashedPassword,
                Email = SanitizeEmail(userDto.Email.Trim().ToLower()),
                Age = userDto.Age,
                ProfilePictureUrl = null,
                Role = userDto.Role,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            await _context.Users.AddAsync(user);

            CultureInfo albanianCulture = new CultureInfo("sq-AL");

            var newNotification = new Notifications
            {
                ReceiverId = user.ID,
                Information = $"Njoftim mbi regjistrimin e suksesshem te llogarise tuaj me {DateTime.Now.ToString("f", albanianCulture)}",
                Type = Shared.Enums.NotificationsType.RegisteredAccount,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };
            await _context.Notifications.AddAsync(newNotification);
            await _context.SaveChangesAsync();

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

        [Authorize(Roles = "Admin")]
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

        [Authorize]
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
                                    .Include(a => a.Payments)
                                    .ThenInclude(p => p.Package)
                                    .Select(getUser => new
                                    {
                                        getUser.ID,
                                        getUser.Firstname,
                                        getUser.Lastname,
                                        getUser.Email,
                                        getUser.Age,
                                        getUser.UserMeta,
                                        getUser.Payments,
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

        [Authorize]
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
                    users.UserMeta,
                    users.Payments,
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

        //[Authorize]
        [HttpPut("UpdatePersonalData")]
        public async Task<IActionResult> UpdateUserPersonal([FromBody] UpdateUser userDto, CancellationToken token)
        {
            await using var transacton = await _context.Database.BeginTransactionAsync(token);
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Model invalid" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int userIdAuth))
                {
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(userIdAuth, token);

                if (user == null)
                {
                    return NotFound();
                }


                if (!string.IsNullOrEmpty(userDto.Password) || !string.IsNullOrEmpty(userDto.ConfirmPassword))
                {
                    if (userDto.Password != userDto.ConfirmPassword)
                    {
                        return BadRequest(new { Message = "Password and confirmation don't match" });
                    }

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

                if (!string.IsNullOrEmpty(userDto.Email) && (user.Email != userDto.Email))
                {
                    //logic for verification of email.
                    user.Email = userDto.Email;
                }

                _mapper.Map(userDto, user);
                user.LastModified = DateTime.UtcNow;
                _context.Users.Update(user);

                var userMetas = await _context.UserMeta.Where(c => c.UserID == userIdAuth).ToListAsync(token);
                var phoneMeta = userMetas.FirstOrDefault(c => c.MetaKey == "Phone");
                phoneMeta.MetaValue = userDto.Phone;
                phoneMeta.LastModified = DateTime.UtcNow;
                _context.UserMeta.Update(phoneMeta);

                await _context.SaveChangesAsync(token);
                await transacton.CommitAsync(token);
                return Ok(new { Message = "Data updated successfully!" });
            }
            catch (Exception ex)
            {
                await transacton.RollbackAsync(token);
                _logger.LogError(ex, $"Error in updating user personal details");
                var errorMessage = new
                {
                    Message = "Error updating data!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }

        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize]
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
                if(Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    user.ProfilePictureUrl = url;
                    user.LastModified = DateTime.UtcNow;

                    _userRepository.Update(user);
                    await _userRepository.SaveAsync(default);
                    return Ok(new { FileUrl = url });
                }
                else
                {
                    return BadRequest(new { Message = "Bad uri" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", id);
                return StatusCode(500, "Internal server error while updating profile picture.");
            }
    
        }

        //to fix or to be deleted
        [Authorize(Roles = "Admin")]
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

        [Authorize]
        [HttpGet("/api/Users/GetUserById/{userId}")]
        public async Task<IActionResult> GetUserById(int userId, CancellationToken token)
        {
            try
            {
                var user = await _context
                    .Users
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Where(c => c.ID == userId)
                    .Select(c => new
                    {
                        c.ID,
                        c.Firstname, 
                        c.Lastname,
                        c.Email,
                        c.UserMeta,
                        InstructorId = c.Instructor != null ? c.Instructor.ID : (int?)null,
                        c.Username,
                        c.Role,
                        c.Payments,
                        c.Friends,
                        c.ProfilePictureUrl,
                        Quizzes = c.Quizzes.Where(uq => uq.UserId == userId).Select(uq => new
                        {
                            uq.ID,
                            uq.QuizName,
                            uq.QuizDescription,
                            uq.QuizCategory,
                            uq.CreatedAt,
                            uq.UserId,
                            uq.ViewCount,
                            //QuizIsCompleted = uq.QuizzesCompleted.Where(qc => qc.QuizId == uq.ID && qc.Completed == true).Count(),
                            QuizIsCompleted = uq.QuizzesCompleted.Count(qc => qc.Completed),
                            Mistakes = uq.QuizzesCompleted.Select(mis => mis.Mistakes).FirstOrDefault()
                        }).ToList(),
                        CourseCompleted = c.CourseCompleted.Where(cc => cc.UserId == userId).Select(cc => new
                        {
                            cc.ID,
                            cc.CourseId,
                            cc.Testimonial,
                            cc.UserId,
                            cc.CreatedAt,
                            cc.LastModified,
                            Course = new
                            {
                                cc.Course.ID,
                                cc.Course.CourseName,
                                cc.Course.UserId,
                                cc.Course.CourseDescription,
                                cc.Course.CourseFeaturedImage,
                                cc.Course.CourseCategory,
                            }
                        }).ToList(),
                        QuizzesCompleted = c.QuizzesCompleted.Where(q => q.UserId == userId).Select(q => new
                        {

                            q.ID,
                            q.Quiz,
                            q.Completed,
                            q.Duration,
                            q.CreatedAt,
                            q.QuizId,
                            q.UserId,
                            q.Mistakes,
                        }).ToList(),
                        CourseCreated = c.CoursesCreated.Select(ck => new
                        {
                            ck.ID,
                            ck.CourseName,
                            ck.CourseCategory,
                            ck.UserId,
                            ck.CourseFeaturedImage,
                            ck.CourseDescription,
                            ck.CourseEnrolled,
                            ck.ViewCount,
                            ck.CreatedAt,
                        }).ToList(),
                        UserInformation = c.UserInformations == null ? null : new 
                        {
                            c.UserInformations.Birthday,
                            c.UserInformations.SoftSkills,
                            c.UserInformations.UserId,
                            c.UserInformations.Skills,
                            c.UserInformations.Profession,
                            UserEducation = c.UserInformations.UserEducations.Where(ue => ue.UserId == userId).Select(ue => new
                            {
                                ue.Place_Name,
                                ue.UserId,
                                ue.School_Degree,
                                ue.Field,
                                ue.UserInformationId,
                                ue.Start_Year,
                                ue.End_Year,
                            }).ToList() ?? null,
                            UserJobs = c.UserInformations.UserJobs.Where(uj => uj.UserId == userId).Select(uj => new
                            {
                                uj.Job_Place,
                                uj.Job_Title,
                                uj.UserId,
                                uj.Start_Year,
                                uj.UserInformationId,
                                uj.End_Year,
                            }).ToList() ?? null
                        },
                        CommitsData = c.Commits.Where(c => c.UserId == userId).Select(cm => new
                        {
                            cm.Date,
                            cm.Count,
                        })
                    })
                    .FirstOrDefaultAsync(token);

                if(user == null)
                {
                    return NotFound(new { Message = "No user Found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving user with id: {userId}");
                return BadRequest(new { Message = "Error in retriving user" });
            }
        }

        [Authorize]
        [HttpPut("/api/Users/IncreaseCommitment")]
        public async Task<IActionResult> IncreaseCommitment(CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var commitment = await _context.Commits
                    .Where(c => c.UserId == userId && c.Date == DateOnly.FromDateTime(DateTime.UtcNow.Date))
                    .FirstOrDefaultAsync(token);
                if (commitment == null)
                {
                    var newCommit = new Commits
                    {
                        UserId = userId,
                        Count = 1,
                        Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    await _context.Commits.AddAsync(newCommit, token);
                    await _context.SaveChangesAsync(token);
                    return Ok(new { Message = "Commitment created" });
                }
                else
                {
                    commitment.Count += 1;
                    commitment.LastModified = DateTime.UtcNow;
                    _context.Commits.Update(commitment);
                    await _context.SaveChangesAsync(token);
                    return Ok(new { Message = "Commitment updated" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in increasing commitment");
                return BadRequest(new { Message = "Error in incresing commitment" });
            }
        }

        [Authorize]
        [HttpGet("/api/Users/GetCommitments/{userId}")]
        public async Task<IActionResult> GetCommitments(int userId, CancellationToken token)
        {
            try
            {
                var commitments = await _context.Commits.AsNoTracking()
                    .Where(c => c.UserId == userId)
                    .Select(c => new
                    {
                        c.Date,
                        c.Count,
                    })
                    .ToListAsync(token);
                return Ok(commitments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving user commitments with userid: {userId}");
                return BadRequest(new { Message = "Error in retriving user commitments" });
            }
        }

        [Authorize]
        [HttpGet("/api/Users/GetAllUsersStatistics")]
        public async Task<IActionResult> GetAllUsersStatistics([FromQuery] string ?searchParam, CancellationToken token)
        {
            try
            {
                var query = _context.Users.AsNoTracking();
                if (!string.IsNullOrEmpty(searchParam))
                {
                    query = query.Where(s =>
                    EF.Functions.Contains(s.Firstname, $"\"{searchParam}*\"") ||
                    EF.Functions.Contains(s.Lastname, $"\"{searchParam}*\"") ||
                    EF.Functions.Contains(s.Username, $"\"{searchParam}*\"")
                    );
                }
                
                var userStatistics = await query
                    .Select(c => new
                    {
                        c.ID,
                        Name = c.Firstname + " " + c.Lastname,
                        c.Username,
                        c.Email,
                        c.UserMeta,
                        CoursesCreated = c.CoursesCreated.Count(),
                        LessonsCompleted = c.UserProgress.Where(lc => lc.IsCompleted != false).Count(),
                        QuizzesCreated = c.Quizzes.Count(),
                        BlogsCreated = c.Blogs.Count(),
                        Commitmens = c.Commits.Where(cm => cm.UserId == c.ID).Sum(commitment => commitment.Count)
                    })
                    .ToListAsync(token);

                return Ok(userStatistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriing all users statistics");
                return BadRequest(new { Message = "Error in retriving all user statistics" });
                throw;
            }
        }

        

    }
}
