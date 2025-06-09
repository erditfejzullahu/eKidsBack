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
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstructorsController : ControllerBase
    {
        private readonly ILogger<InstructorsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ISorterService<InstructorCourses> _sortService;
        private readonly IManageInstructorContentService _instructorContentService;
        private readonly IHubContext<NotificationsHub> _notificationsHub;

        public InstructorsController(ISorterService<InstructorCourses> sorterService, IHubContext<NotificationsHub> notificationsHub, IManageInstructorContentService instructorContentService, IFileUploadService fileUploadService, ILogger<InstructorsController> logger, ApplicationDbContext context)
        {
            _notificationsHub = NotificationsHub;
            _logger = logger;
            _context = context;
            _fileUploadService = fileUploadService;
            _sortService = sorterService;
            _instructorContentService = instructorContentService;
        }

        [Authorize]
        [HttpPost("BecomeInstructor")]
        public async Task<IActionResult> BecomeInstructor([FromBody] CreateInstructor instructorDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int userIdAuthed))
                {
                    return Unauthorized();
                }
                var user = await _context.Users.FindAsync(userIdAuthed, token);
                if(user == null)
                {
                    return NotFound();
                }

                foreach (var social in instructorDto.Socials)
                {
                    if(string.IsNullOrWhiteSpace(social.Label) || string.IsNullOrWhiteSpace(social.Link))
                    {
                        return BadRequest(new { Message = "no null data" });
                    }
                }

                var sanitizer = new HtmlSanitizer();
                if (instructorDto.Socials != null)
                {
                    foreach (var social in instructorDto.Socials)
                    {
                        // Example: Sanitize URLs or text fields in the Socials object
                        social.Label = sanitizer.Sanitize(social.Label?.Trim() ?? "");
                        social.Link = sanitizer.Sanitize(social.Link?.Trim() ?? ""); // Or use Uri.IsWellFormedUriString
                    }
                }

                var serializeSocials = JsonSerializer.Serialize(instructorDto.Socials);

                var cleanBio = sanitizer.Sanitize(instructorDto.Bio?.Trim() ?? "");
                var cleanExpertise = sanitizer.Sanitize(instructorDto.Expertise?.Trim() ?? "");

                var newInstructor = new Instructors
                {
                    UserId = userIdAuthed,
                    Expertise = cleanExpertise,
                    Bio = cleanBio,
                    Socials = serializeSocials,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Instructors.AddAsync(newInstructor, token);
                user.Role = "Instructor";
                user.LastModified = DateTime.UtcNow;
                _context.Users.Update(user);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = userIdAuthed,
                    Information = $"Informacion mbi krijimin e llogarise suaj ne rolin e Instruktorit me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification);

                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);

                var connectionId = ConnectionMapping.GetConnectionId(user.Username);
                if(connectionId != null)
                {
                    var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == user.ID && !c.IsRead).CountAsync(token);
                    await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                }

                return Ok(new { Message = "User became instructor" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in becoming an instructor");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpPost("CreateCourse")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto courseDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int userIdAuthed))
                {
                    return Unauthorized();
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var user = await _context.Users
                    .Select(c => new
                    {
                        c.ID,
                        InstructorId = c.Instructor.ID,
                        c.Username
                    })
                    .FirstOrDefaultAsync(c => c.ID == userIdAuthed);
                if(user == null)
                {
                    return NotFound(new {Message = "No user"});
                }

                if(courseDto.SectionTitles.Count != courseDto.SectionLessons.Count)
                {
                    return BadRequest(new { Message = "Not same lengths" });
                }

                var categories = await _context.Categories.AsNoTracking().Select(c => c.ID).ToListAsync();

                if (!categories.Contains(courseDto.CategoryId))
                {
                    return BadRequest(new { Message = "No valid id provided" });
                }

                string? imageUrl = string.Empty;
                if (!string.IsNullOrEmpty(courseDto.Image))
                {
                    var relativeUrl = await _fileUploadService.UploadFile(courseDto.Image, FileCategory.Uploads);
                    imageUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                }

                var sanitizer = new HtmlSanitizer();

                courseDto.TopicsCovered = courseDto.TopicsCovered
                    .Select(topic => sanitizer.Sanitize(topic?.Trim() ?? ""))
                    .ToList();

                string topics = JsonSerializer.Serialize(courseDto.TopicsCovered);

                var newCourse = new InstructorCourses
                {
                    InstructorId = user.InstructorId,
                    Name = sanitizer.Sanitize(courseDto.Name.Trim()),
                    Description = sanitizer.Sanitize(courseDto.Description.Trim()),
                    CategoryId = courseDto.CategoryId,
                    TopicsCovered = topics,
                    Level = courseDto.Level,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    InstructorCourseSections = new List<InstructorCourseSections>()
                };

                for (int i = 0; i < courseDto.SectionTitles.Count; i++)
                {
                    var newSection = new InstructorCourseSections
                    {
                        Course_Id = newCourse.ID,
                        Title = sanitizer.Sanitize(courseDto.SectionTitles[i].Trim()),
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        InstructorLessons = new List<InstructorLessons>()
                    };

                    foreach (var lessonTitle in courseDto.SectionLessons[i])
                    {
                        var newLesson = new InstructorLessons
                        {
                            Title = sanitizer.Sanitize(lessonTitle.Trim()),
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        newSection.InstructorLessons.Add(newLesson);
                    }
                    newCourse.InstructorCourseSections.Add(newSection);
                }

                await _context.InstructorCourses.AddAsync(newCourse, token);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                var notification = new Notifications
                {
                    ReceiverId = user.ID,
                    Information = $"Njoftim mbi krijimin e kursit {newCourse.Name} me {DateTime.Now.ToString("f", cultureInfo)}",
                    Type = Shared.Enums.NotificationsType.CustomInformaionOrPromotionsSendToAll,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                await _context.Notifications.AddAsync(notification, token);

                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);

                var connectionId = ConnectionMapping.GetConnectionId(user.Username);
                if (connectionId != null)
                {
                    var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == user.ID && !c.IsRead).CountAsync(token);
                    await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                }

                return Ok(new { Message = "Course added successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in creating course");
                return BadRequest();
            }
        }

        //kjo osht kur te fillon ni kurs qe e ofron instruktori
        [Authorize]
        [HttpPost("StartCourse")]
        public async Task<IActionResult> EnrollCourse([FromBody] EnrollCourseDto enrollCourse, CancellationToken token)
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

                var ifInstructor = await _context.Instructors.Where(c => c.UserId == userId).FirstOrDefaultAsync();
                if(ifInstructor != null)
                {
                    return Ok(new { Message = "It is instructor, no need for progress" });
                }

                var getCourse = await _context.InstructorCourses.AsNoTracking().FirstOrDefaultAsync(c => c.ID == enrollCourse.CourseId);
                if(getCourse == null)
                {
                    return NotFound(new { Message = "No course found" });
                }

                var getStudentAvailable = await _context.InstructorStudents.AnyAsync(c => c.UserId == userId && c.InstructorId == enrollCourse.InstructorId);
                var getLessonProgress = await _context.StudentCourseLessonProgress.AnyAsync(c => c.UserId == userId && c.CourseId == enrollCourse.CourseId);

                CultureInfo cultureInfo = new CultureInfo("sq-AL");

                if(getStudentAvailable != false && getLessonProgress != false)
                {
                    return Ok("course is already started");
                }

                if (getStudentAvailable == false)
                {
                    var newEnrollment = new InstructorStudents
                    {
                        UserId = userId,
                        CourseId = enrollCourse.CourseId,
                        InstructorId = enrollCourse.InstructorId,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    await _context.InstructorStudents.AddAsync(newEnrollment, token);

                    var notification = new Notifications
                    {
                        ReceiverId = userId,
                        Information = $"Njoftim mbi fillimin e kursit {getCourse.Name} me {DateTime.Now.ToString("f", cultureInfo)}",
                        Type = Shared.Enums.NotificationsType.ProgressTrackingNotification,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                    };
                    await _context.Notifications.AddAsync(notification, token);
                }

                
                if(getLessonProgress == false)
                {
                    var lessonProgressList = new List<StudentCourseLessonProgress>();

                    foreach (var sections in getCourse.InstructorCourseSections)
                    {
                        foreach (var lesson in sections.InstructorLessons)
                        {
                            lessonProgressList.Add(new StudentCourseLessonProgress
                            {
                                UserId = userId,
                                CourseId = enrollCourse.CourseId,
                                LessonId = lesson.ID,
                                HasJoined = false,
                                IsCompleted = false,
                                JoinedTime = null,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            });
                        }
                    }
                    await _context.StudentCourseLessonProgress.AddRangeAsync(lessonProgressList, token);
                }
                if (!string.IsNullOrEmpty(username))
                {
                    var connectionId = ConnectionMapping.GetConnectionId(username);
                    if(connectionId != null)
                    {
                        var unreadNotifications = await _context.Notifications.AsNoTracking().Where(c => c.ReceiverId == userId && !c.IsRead).CountAsync(token);
                        await _notificationsHub.Clients.Client(connectionId).SendAsync("UnreadNotifications", unreadNotifications);
                    };
                }
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);


                return Ok(new { Message = "Enrolled successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in enrollin course");
                return BadRequest();
            }
        }


        [Authorize(Roles = "Instructor")]
        [HttpGet("GetInstructorLessonsBasedOfCoursesMeetingAdd")]
        public async Task<IActionResult> GetInstructorLessonsBasedOfCourse([FromQuery] int courseId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(userId == null)
                {
                    return Unauthorized();
                }

                //var user = await _context.Users.Select(c => new
                //{
                //    c.ID,
                //    InstructorId = c.Instructor.ID,
                //}).FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));

                //if(user == null)
                //{
                //    return NotFound(new { Message = "no user found" });
                //}
                var lessons = await _context.InstructorCourses
                    .Where(c => c.ID == courseId)
                    .SelectMany(c => c.InstructorCourseSections)
                    .SelectMany(s => s.InstructorLessons)
                    .Select(l => new
                    {
                        LessonId = l.ID,
                        l.Title
                    })
                    .ToListAsync();

                return Ok(lessons);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lessons");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpGet("GetInstructorCoursesForMeetingAdd")]
        public async Task<IActionResult> GetAllCoursesForMeetingAdd()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(userId == null)
                {
                    return Unauthorized();
                }
                var user = await _context.Users
                    .Select(c => new
                    {
                        c.ID,
                        InstructorId = c.Instructor.ID
                    })
                    .FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));

                if(user == null)
                {
                    return NotFound(new {Message = "No user found"});
                }

                var courses = await _context.InstructorCourses.AsNoTracking().Where(c => c.InstructorId == user.InstructorId).ToListAsync();
                if(courses.Count == 0)
                {
                    return NotFound(new {Message = "No courses found"});
                }

                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetInstructorCoursesCreatedById/{instructorId}")]
        public async Task<IActionResult> GetInstructorCoursesById(int instructorId)
        {
            try
            {
                var courses = await _context.InstructorCourses.Where(c => c.InstructorId == instructorId).Select(c => new { c.ID, c.Name, c.CategoryId, c.CreatedAt }).ToListAsync();
                if(courses.Count == 0)
                {
                    return NotFound();
                }
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses");
                return BadRequest();
            }
        }


        //fixes bug have to add them into single query // it is for profile part
        [Authorize(Roles = "Instructor")]
        [HttpGet]
        public async Task<IActionResult> GetInstructorById()
        {
            
            try
            {
                var userIdAuthenticated = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdAuthenticated) || !Int32.TryParse(userIdAuthenticated, out int userId))
                {
                    return Unauthorized();
                }

                var instructor = await _context.Users
                    .Where(c => c.ID == userId)
                    .Select(c => new
                    {
                        c.ID,
                        InstructorId = c.Instructor.ID,
                        c.Instructor.Expertise,
                        c.Instructor.Bio,
                        c.Instructor.Socials,
                        Name = c.Firstname + " " + c.Lastname,
                        c.Username,
                        c.Email,
                        c.Age,
                        c.ProfilePictureUrl,
                        Courses = c.Instructor.InstructorCourses.Count,
                        Friends = c.Friends.ToList(),
                        Meetings = c.Instructor.OnlineMeetings.Count,
                        Students = c.Instructor.InstructorStudents.Count
                    })
                    .FirstOrDefaultAsync();


                if (instructor == null)
                {
                    return NotFound();
                }
                return Ok(instructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor by id");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetInstructorsCourses")]
        public async Task<IActionResult> GetInstructorCourses([FromQuery] SortQueryDto sortQueryDto, [FromQuery] PaginationDto paginationDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int authId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users.FindAsync(authId);
                if (user == null)
                {
                    return NotFound();
                }

                var unSorted = _context.InstructorCourses.AsNoTracking();
                var countCourses = await unSorted.CountAsync();

                var sortedQuery = sortQueryDto.IsEmpty() ? unSorted.OrderByDescending(c => c.CreatedAt) : _sortService.SortData(unSorted, sortQueryDto);

                var courses = await sortedQuery
                    //.Include(c => c.Instructor).ThenInclude(c => c.User)
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new
                    {
                        c.ID,
                        c.InstructorId,
                        InstructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        c.Instructor.User.ProfilePictureUrl,
                        c.Image,
                        c.Name,
                        c.Level,
                        c.Description,
                        c.CategoryId,
                        c.Instructor,
                        EnrolledStudents = c.InstructorStudents.Where(s => s.CourseId == c.ID).Count(),
                        Enrolled = user.Role == "Instructor" ? false : c.InstructorStudents.Any(s => s.UserId == user.ID),
                        c.CreatedAt,
                    })
                    .ToListAsync();

                if(courses.Count == 0)
                {
                    return NotFound(new { Message = "No courses found" });
                }
                bool hasMore = (paginationDto.Skip + courses.Count) < countCourses;

                return Ok(new { courses, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses");
                return BadRequest();
            }
        }


        //PROGRESS OF PARTICIPATION IN MEETINGS IN COURSE ENROLLMENTS // progresi i studentav ntakiem online
        [Authorize]
        [HttpGet("GetInstructorsCoursesUserProgress")]
        public async Task<IActionResult> GetInstructorsCoursesUserProgress()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int user))
                {
                    return Unauthorized();
                }

                var courseProgresses = await _context.StudentCourseLessonProgress
                    .Where(c => c.UserId == user)
                    .GroupBy(c => c.CourseId)
                    .Select(g => new
                    {
                        Course = new
                        {
                            g.First().Courses.ID,
                            g.First().Courses.InstructorId,
                            InstructorName = g.First().Courses.Instructor.User.Firstname + " " + g.First().Courses.Instructor.User.Lastname,
                            g.First().Courses.Instructor.User.ProfilePictureUrl,
                            g.First().Courses.Image,
                            g.First().Courses.Level,
                            g.First().Courses.Name,
                            g.First().Courses.Description,
                            g.First().Courses.CategoryId,
                            EndrolledStudents = g.First().Courses.InstructorStudents.Where(s => s.CourseId == g.First().Courses.ID).Count(),
                        },
                        Lessons = g.Select(t => new
                        {
                            t.Lessons.ID,
                            t.Lessons.Title,
                            t.IsCompleted,
                            RouteTo = g.First().Courses.OnlineMeetings.Where(om => om.LessonId == t.Lessons.ID).FirstOrDefault()
                        }),
                        TotalLessons = g.Count(),
                        CompletedLessons = g.Count(c => c.IsCompleted),
                        CompletionPercentage = (int)((g.Count(c => c.IsCompleted) * 100.0 / g.Count()))
                    })
                    .ToListAsync();

                return Ok(courseProgresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for user courses enrolled");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("TutorCourses/{id}")]
        public async Task<IActionResult> GetTutorCourses(int id, [FromQuery] SortQueryDto sortQueryDto, [FromQuery] PaginationDto paginationDto)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                paginationDto.Validate();
                var baseQuery = _context.Instructors
                    .Where(u => u.ID == id)
                    .SelectMany(u => u.InstructorCourses);

                var sortedQuery = sortQueryDto.IsEmpty()
                    ? baseQuery.OrderByDescending(c => c.CreatedAt)
                    : _sortService.SortData(baseQuery, sortQueryDto);

                int totalCount = await sortedQuery.CountAsync();

                var courses = await sortedQuery
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new
                    {
                        c.ID,
                        c.Image,
                        c.Name,
                        c.Level,
                        c.Description,
                        c.CategoryId,
                        InstructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        c.Instructor.User.ProfilePictureUrl,
                        InstructorId = c.Instructor.ID,
                        EnrolledStudents = c.InstructorStudents.Count,
                        Enrolled = c.InstructorStudents.Any(s => s.UserId == userId),
                        c.CreatedAt
                    })
                    .ToListAsync();

                var instructor = await _context.Instructors.Where(c => c.ID == id).Select(c => new
                {
                    c.User.ID,
                    c.User.ProfilePictureUrl,
                    Name = c.User.Firstname + " " + c.User.Lastname,
                    InstructorId = c.ID
                }).FirstOrDefaultAsync();

                if(instructor == null)
                {
                    return NotFound();
                }

                bool hasMore = (paginationDto.Skip + courses.Count) < totalCount;


                return Ok(new
                {
                    Instructor = instructor,
                    Courses = courses,
                    HasMore = hasMore,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tutor courses");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("Course/{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int userAuthed))
            {
                return Unauthorized();
            }

            try
            {
                var course = await _context.InstructorCourses
                    .Where(c => c.ID == id)
                    .Select(c => new
                    {
                        CourseId = c.ID,
                        //Enrolled = c.InstructorStudents.Any(x => x.UserId == userAuthed && x.CourseId == c.ID),
                        c.InstructorId,
                        CourseName = c.Name,
                        CourseDescription = c.Description,
                        c.TopicsCovered,
                        c.Level,
                        c.Image,
                        IntructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        InstructorProfilePicture = c.Instructor.User.ProfilePictureUrl,
                        InstructorBio = c.Instructor.Bio,
                        InstructorExpertise = c.Instructor.Expertise,
                        Sections = c.InstructorCourseSections.Select(ic => new
                        {
                            ic.ID,
                            ic.Title,
                            Lessons = ic.InstructorLessons.Select(il => new
                            {
                                il.ID,
                                il.Title,
                                il.Content,
                                il.Video_Url,
                                il.CreatedAt,
                                il.LastModified,
                                Meeting = il.OnlineMeetings.Select(om => new
                                {
                                    om.ID,
                                    Status = om.Status == MeetingStatus.Scheduled && om.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                                    : om.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                                    : om.Status == MeetingStatus.Scheduled && om.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                                    : om.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                                }).FirstOrDefault()
                            }).ToList()
                        }).ToList(),
                        Routes = c.InstructorStudents.Any(x => x.UserId == userAuthed && x.CourseId == c.ID) 
                        ? new
                        {
                            Enrolled = true,
                            RouteTo = (object)c.OnlineMeetings.Where(x => x.CourseId == c.ID &&
                            x.LessonId == c.CourseLessonProgresses.Where(cl => cl.CourseId == c.ID && !cl.IsCompleted).OrderBy(cl => cl.LessonId).FirstOrDefault().LessonId).FirstOrDefault(),
                        } 
                        : new 
                        {
                            Enrolled = false,
                            RouteTo = (object)c.OnlineMeetings.Where(x => x.CourseId == c.ID).OrderBy(cl => cl.LessonId).FirstOrDefault()
                        },
                            //c.OnlineMeetings.Where(x => x.CourseId == c.ID && 
                            //x.LessonId == c.CourseLessonProgresses.Where(cl => cl.CourseId == c.ID).OrderBy(cl => cl.LessonId).FirstOrDefault(cl => !cl.IsCompleted).LessonId).FirstOrDefault() ?? null,
                        //RouteTo = c.CourseLessonProgresses.Where(d => d.CourseId == c.ID).FirstOrDefault(d => d.IsCompleted == false) ?? null
                    })
                    .FirstOrDefaultAsync();
                if(course == null)
                {
                    return NotFound(new {Message = "No course found"});
                }
                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving course");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin,Student")]
        [HttpGet("GetInstructorData/{instructorId}")]
        public async Task<IActionResult> GetInstructorDataById(int instructorId)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var instructor = await _context.Instructors
                    .Where(c => c.ID == instructorId)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Select(c => new
                    {
                        InstructorId = c.ID,
                        c.UserId,
                        InstructorName = c.User.Firstname + " " + c.User.Lastname,
                        c.User.ProfilePictureUrl,
                        c.User.Email,
                        c.Expertise,
                        InstructorUsername = c.User.Username,
                        c.Bio,
                        InstructorStudents = c.InstructorStudents.Select(std => new
                        {
                            std.User.ID,
                            Name = std.User.Firstname + " " + std.User.Lastname,
                            std.User.ProfilePictureUrl,
                            std.User.Username,
                            std.User.Email
                        }).ToList(),
                        InstructorCourses = c.InstructorCourses.Select(crs => new
                        {
                            crs.ID,
                            crs.InstructorId,
                            crs.Image,
                            crs.Name,
                            crs.Level,
                            crs.Description,
                            crs.CategoryId,
                            EnrolledStudents = c.InstructorStudents.Where(s => s.CourseId == crs.ID).Count(),
                            //Enrolled = c.InstructorStudents.Any(s => s.UserId == userId),
                            c.CreatedAt,
                        }).ToList(),
                        OnlineMeetings = c.OnlineMeetings.Select(om => new
                        {
                            om.ID,
                            Course = om.Course ?? null,
                            Lesson = om.Lesson ?? null,
                            om.Title,
                            om.Description,
                            om.MeetingUrl,
                            om.ScheduleDateTime,
                            DurationTime = om.DurationTime ?? null,
                            Status = om.Status == MeetingStatus.Scheduled && om.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                            : om.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                            : om.Status == MeetingStatus.Scheduled && om.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                            : om.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                            Participants = om.OnlineMeetingsParticipants.Count(),
                            c.CreatedAt
                        }).ToList(),
                        IsYourInstructor = c.InstructorStudents.Any(d => d.UserId == userId),
                        WhenBecameInstructor = c.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if(instructor == null)
                {
                    return NotFound(new {Message = "No instructor found or invalid data"});
                }

                return Ok(instructor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor data");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetAllInstructors")]
        public async Task<IActionResult> GetAllInstructors([FromQuery] SortQueryDto sortQueryDto, [FromQuery] PaginationDto paginationDto)
        {
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var unSorted = _context.Instructors.AsNoTracking();
                var totalCount = await unSorted.CountAsync();
                if (!string.IsNullOrEmpty(sortQueryDto.SortByName))
                {
                    if (string.Equals(sortQueryDto.SortNameOrder, "desc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderByDescending(c => c.User.Firstname);
                    }
                    else if(string.Equals(sortQueryDto.SortNameOrder, "asc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderBy(c => c.User.Firstname);
                    }
                }

                if (!string.IsNullOrEmpty(sortQueryDto.SortByDate))
                {
                    if(string.Equals(sortQueryDto.SortDateOrder, "desc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderByDescending(c => c.CreatedAt);
                    }else if(string.Equals(sortQueryDto.SortDateOrder, "asc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderBy(c => c.CreatedAt);
                    }
                }

                if (!string.IsNullOrEmpty(sortQueryDto.SortByViews))
                {
                    if(string.Equals(sortQueryDto.SortViewOrder, "desc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderByDescending(c => c.InstructorCourses.Count());
                    }
                    else if(string.Equals(sortQueryDto.SortViewOrder, "asc", StringComparison.OrdinalIgnoreCase))
                    {
                        unSorted = unSorted.OrderBy(c => c.InstructorCourses.Count());
                    }
                }

                var instructors = await unSorted
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new
                    {
                        InstructorId = c.ID,
                        c.UserId,
                        InstructorName = c.User.Firstname + " " + c.User.Lastname,
                        c.User.ProfilePictureUrl,
                        c.User.Email,
                        c.Expertise,
                        c.Bio,
                        InstructorStudents = c.InstructorStudents.Count(),
                        InstructorCourses = c.InstructorCourses.Count(),
                        IsYourInstructor = c.InstructorStudents.Any(d => d.UserId == userId),
                        WhenBecameInstructor = c.CreatedAt
                    })
                    .ToListAsync();

                if(instructors.Count == 0)
                {
                    return NotFound(new {Message = "No instructors found"});
                }
                bool hasMore = (paginationDto.Skip + instructors.Count) < totalCount;
                return Ok(new { instructors, hasMore });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all isntructors");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpGet("GetInstructorManageContentData")]
        public async Task<IActionResult> ManageInstructorData([FromQuery] InstructorsManageContentType manageType, [FromQuery] SortQueryDto sortQueryDto, [FromQuery] PaginationDto paginationDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out var authId))
                {
                    return Unauthorized();
                }


                var user = await _context.Users
                    .Where(u => u.ID == authId)
                    .Select(u => new InstructorManageUserDto
                    {
                        ID = u.ID,
                        InstructorId = u.Instructor.ID
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                var query = await _instructorContentService.RetrieveInstructorActivities(user, manageType, sortQueryDto, paginationDto);

                if(query.Item1.Count == 0)
                {
                    return NotFound(new {Message = "No data found"});
                }
                //bool hasMore = (paginationDto.Skip + categories.Count) < totalCount;


                return Ok(new {data = query.Item1, query.hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("CourseDeleteAdmin/{id}")]
        public async Task<IActionResult> DeleteCourseAdmin(int id)
        {
            try
            {
                var course = await _context.InstructorCourses.FindAsync(id);
                if(course == null)
                {
                    return NotFound();
                }
                _context.InstructorCourses.Remove(course);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpDelete("CourseDelete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var course = await _context.InstructorCourses.FindAsync(id);
                if (course == null)
                {
                    return NotFound();
                }
                if(course.Instructor.User.ID != userId)
                {
                    return Forbid();
                }
                _context.InstructorCourses.Remove(course);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Course deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course");
                return BadRequest();
            }
        }

        
    }
}
