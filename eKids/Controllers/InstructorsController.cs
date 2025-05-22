using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public InstructorsController(IFileUploadService fileUploadService, ILogger<InstructorsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            _fileUploadService = fileUploadService;
        }

        [HttpPost("BecomeInstructor")]
        public async Task<IActionResult> BecomeInstructor([FromBody] CreateInstructor instructorDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = await _context.Users.FindAsync(instructorDto.UserId, token);
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
                var serializeSocials = JsonSerializer.Serialize(instructorDto.Socials);

                var newInstructor = new Instructors
                {
                    UserId = instructorDto.UserId,
                    Expertise = instructorDto.Expertise,
                    Bio = instructorDto.Bio,
                    Socials = serializeSocials,
                    LastModified = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Instructors.AddAsync(newInstructor, token);
                user.Role = "Instructor";
                user.LastModified = DateTime.UtcNow;
                _context.Users.Update(user);

                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);

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
                if(userId == null)
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
                        InstructorId = c.Instructor.ID
                    })
                    .FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));
                if(user == null)
                {
                    return NotFound(new {Message = "No user"});
                }

                if(courseDto.SectionTitles.Count != courseDto.SectionLessons.Count)
                {
                    return BadRequest(new { Message = "Not same lengths" });
                }

                string? imageUrl = string.Empty;
                if (!string.IsNullOrEmpty(courseDto.Image))
                {
                    var relativeUrl = await _fileUploadService.UploadFile(courseDto.Image, FileCategory.Uploads);
                    imageUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                }

                string topics = JsonSerializer.Serialize(courseDto.TopicsCovered);

                var newCourse = new InstructorCourses
                {
                    InstructorId = user.InstructorId,
                    Name = courseDto.Name,
                    Description = courseDto.Description,
                    CategoryId = courseDto.CategoryId,
                    TopicsCovered = topics,
                    Level = courseDto.Level,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    InstructorCourseSections = new List<InstructorCourseSections>()
                };

                for (int i = 0; i < courseDto.SectionTitles.Count; i++)
                {
                    var newSection = new InstructorCourseSections
                    {
                        Course_Id = newCourse.ID,
                        Title = courseDto.SectionTitles[i],
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow,
                        InstructorLessons = new List<InstructorLessons>()
                    };

                    foreach (var lessonTitle in courseDto.SectionLessons[i])
                    {
                        var newLesson = new InstructorLessons
                        {
                            Title = lessonTitle,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };
                        newSection.InstructorLessons.Add(newLesson);
                    }
                    newCourse.InstructorCourseSections.Add(newSection);
                }

                await _context.InstructorCourses.AddAsync(newCourse, token);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Course added successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, " Error in creating course");
                return BadRequest();
            }
        }

        //kjo osht kur te fillon ni kurs qe e ofron instruktori ose kur kyqet me ane te url
        [Authorize]
        [HttpPost("StartCourse")]
        public async Task<IActionResult> EnrollCourse([FromBody] EnrollCourseDto enrollCourse, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(user == null)
                {
                    return Unauthorized();
                }

                var userId = Int32.Parse(user);

                var ifInstructor = await _context.Instructors.Where(c => c.UserId == userId).FirstOrDefaultAsync();
                if(ifInstructor != null)
                {
                    return Ok(new { Message = "It is instructor, no need for progress" });
                }

                var getStudentAvailable = await _context.InstructorStudents.FirstOrDefaultAsync(c => c.UserId == userId && c.InstructorId == enrollCourse.InstructorId);
                var getLessonProgress = await _context.StudentCourseLessonProgress.FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == enrollCourse.CourseId);

                if (getStudentAvailable == null)
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
                }

                var getCourseLessons = await _context.InstructorCourseSections
                    .Where(c => c.Course_Id == enrollCourse.CourseId)
                    .Include(c => c.InstructorLessons)
                    .ToListAsync();

                if(getCourseLessons.Count == 0)
                {
                    return BadRequest(new { Message = "Invalid data provided, no lessons found" });
                }

                if(getLessonProgress == null)
                {
                    foreach (var sections in getCourseLessons)
                    {
                        foreach (var lesson in sections.InstructorLessons)
                        {
                            var newLessonProgress = new StudentCourseLessonProgress
                            {
                                UserId = userId,
                                CourseId = enrollCourse.CourseId,
                                LessonId = lesson.ID,
                                HasJoined = false,
                                IsCompleted = false,
                                JoinedTime = null,
                                CreatedAt = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };
                            await _context.StudentCourseLessonProgress.AddAsync(newLessonProgress, token);
                        }
                    }
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

                var user = await _context.Users.Select(c => new
                {
                    c.ID,
                    InstructorId = c.Instructor.ID,
                }).FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));

                if(user == null)
                {
                    return NotFound(new { Message = "no user found" });
                }
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

                var courses = await _context.InstructorCourses.Where(c => c.InstructorId == user.InstructorId).ToListAsync();
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
                    .AsNoTracking()
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
                    })
                    .FirstOrDefaultAsync();


                if (instructor == null)
                {
                    return NotFound();
                }

                var courses = await _context.InstructorCourses.Where(c => c.InstructorId == instructor.InstructorId).CountAsync();
                var friends = await _context.Friends.Where(c => c.UserId == instructor.ID).ToListAsync();
                var meetings = await _context.OnlineMeetings.Where(c => c.InstructorId == instructor.InstructorId).CountAsync();
                var students = await _context.InstructorStudents.Where(c => c.InstructorId == instructor.InstructorId).CountAsync();

                return Ok(new
                {
                    instructor,
                    courses,
                    friends,
                    meetings,
                    students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor by id");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetInstructorsCourses")]
        public async Task<IActionResult> GetInstructorCourses()
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

                var courses = await _context.InstructorCourses
                    .AsNoTracking()
                    //.Include(c => c.Instructor).ThenInclude(c => c.User)
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

                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor courses");
                return BadRequest();
            }
        }


        //PROGRESS OF PARTICIPATION IN MEETINGS IN COURSE ENROLLMENTS
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
                            Enrolled = c.InstructorStudents.Any(s => s.UserId == userId),
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
        public async Task<IActionResult> GetAllInstructors()
        {
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
            {
                return Unauthorized();
            }

            try
            {
                var instructors = await _context.Instructors
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

                return Ok(instructors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all isntructors");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpGet("GetInstructorManageContentData")]
        public async Task<IActionResult> ManageInstructorData([FromQuery] InstructorsManageContentType manageType)
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
                    .Select(u => new
                    {
                        u.ID,
                        InstructorId = u.Instructor.ID
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                IQueryable<object> query = null;

                switch (manageType)
                {
                    case InstructorsManageContentType.Courses:
                        query = _context.InstructorCourses.AsQueryable().AsNoTracking().Where(c => c.InstructorId == user.InstructorId).Select(c => new
                        {
                            c.ID,
                            c.InstructorId,
                            InstructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                            c.Instructor.User.ProfilePictureUrl,
                            c.Image,
                            c.Name,
                            c.Description,
                            c.Level,
                            c.TopicsCovered,
                            SectionTitles = c.InstructorCourseSections.Select(ic => ic.Title).ToList(),
                            SectionLessons = c.InstructorCourseSections
                                .Select(ic => ic.InstructorLessons.Select(il => il.Title).ToList())
                                .ToList(),
                            c.CategoryId,
                            c.Instructor,
                            EnrolledStudents = c.InstructorStudents.Where(s => s.CourseId == c.ID).Count(),
                            c.CreatedAt,
                        });
                        break;
                    case InstructorsManageContentType.Students:
                        query = _context.InstructorStudents.AsQueryable().AsNoTracking().Where(c => c.InstructorId == user.InstructorId).Select(c => c.User).Distinct()
                            .Select(u => new
                            {
                                u.ID,
                                Name = u.Firstname + " " + u.Lastname,
                                u.ProfilePictureUrl,
                                u.Email,
                                u.Username,
                                OtherInformation = new
                                {
                                    Birthday = u.UserInformations != null ? u.UserInformations.Birthday : null,
                                    Profession = u.UserInformations != null ? u.UserInformations.Profession : null,
                                },
                            });
                        break;
                    case InstructorsManageContentType.Meetings:
                        query = _context.OnlineMeetings.AsQueryable().AsNoTracking().Where(c => c.InstructorId == user.InstructorId).Select(c => new
                        {
                            c.ID,
                            Course = c.Course ?? null,
                            Lesson = c.Lesson ?? null,
                            c.Title,
                            c.Description,
                            c.MeetingUrl,
                            c.ScheduleDateTime,
                            DurationTime = c.DurationTime ?? null,
                            Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                            : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                            : c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                            : c.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                            Participants = c.OnlineMeetingsParticipants.Count(),
                            Instructor = new
                            {
                                Name = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                                c.Instructor.User.ProfilePictureUrl,
                                c.Instructor.User.Username,
                                c.Instructor.User.Email
                            },
                            c.CreatedAt
                        });
                        break;
                    default:
                        return NotFound("Invalid content type specified");
                }

                if(query == null || query.Count() == 0)
                {
                    return NotFound(new {Message = "No data found"});
                }

                var returnValue = await query.ToListAsync();

                return Ok(returnValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data");
                return BadRequest();
            }
        }

        [HttpDelete("CourseDelete/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.InstructorCourses.FindAsync(id);
                if (course == null)
                {
                    return NotFound();
                }
                _context.Remove(course);
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
