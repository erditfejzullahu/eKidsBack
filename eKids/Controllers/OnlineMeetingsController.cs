using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnlineMeetingsController : ControllerBase
    {
        private readonly ILogger<OnlineMeetingsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ISorterService<OnlineMeetings> _sorterService;

        public OnlineMeetingsController(ILogger<OnlineMeetingsController> logger, ApplicationDbContext context, ISorterService<OnlineMeetings> sorterSevice)
        {
            _logger = logger;
            _context = context;
            _sorterService = sorterSevice;
        }

        //kur te kryhet meetingu butoni finish a najsen duhet mu thirr qeky api
        [Authorize(Roles = "Instructor")]
        [HttpPatch("MeetingCompletedFromInstructor")]
        public async Task<IActionResult> MeetingCompletedFromInstructorAsync([FromQuery] int meetingId, CancellationToken token)
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
                var meeting = await _context.OnlineMeetings.FindAsync(meetingId);

                if(meeting == null)
                {
                    return NotFound(new { Message = "Meeting not found" });
                }

                var totalStudents = await _context.StudentCourseLessonProgress
                    .Where(c => c.LessonId == meeting.LessonId && c.CourseId == meeting.CourseId)
                    .ToListAsync();
                
                if(totalStudents.Count() > 0)
                {
                    foreach (var student in totalStudents)
                    {

                        if(student.JoinedTime != null)
                        {
                            if(meeting.DurationTime != null)
                            {
                                double actualWatchMinutes = (DateTime.UtcNow - student.JoinedTime.Value).TotalMinutes;
                                if(actualWatchMinutes >= meeting.DurationTime.Value - 10)
                                {
                                    student.IsCompleted = true;
                                    student.LastModified = DateTime.UtcNow;
                                }
                                else
                                {
                                    student.IsCompleted = false;
                                    student.LastModified = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                student.IsCompleted = true;
                                student.LastModified = DateTime.UtcNow;
                            }

                        }
                        _context.StudentCourseLessonProgress.Update(student);
                    }

                }

                DateTime expectedEndTime;
                if(meeting.DurationTime != null)
                {
                    expectedEndTime = meeting.ScheduleDateTime.AddMinutes(meeting.DurationTime.Value);
                    DateTime actualEndTime = DateTime.UtcNow;
                    double actualDuration = (actualEndTime - meeting.ScheduleDateTime).TotalMinutes;
                    if (actualDuration >= meeting.DurationTime.Value - 10)
                    {
                        meeting.Status = MeetingStatus.Completed;
                    }
                    else
                    {
                        meeting.Status = MeetingStatus.Cancelled;
                    }
                }
                else
                {
                    meeting.Status = MeetingStatus.Completed;
                }
                //qitu ni logjik per me track a u bo completed apo jo.
                _context.OnlineMeetings.Update(meeting);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Meeting completed" });
                
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error completing statuses for students");
                return BadRequest();
            }
        }

        //kur te hin ne miting duhet mu thirr qiky
        [Authorize]
        [HttpPatch("StartMeeting")]
        public async Task<IActionResult> StartMeetingAsync([FromQuery] int meetingId)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(user == null)
                {
                    return Unauthorized();
                }
                var userId = Int32.Parse(user);
                var meeting = await _context.OnlineMeetings.FindAsync(meetingId);
                if(meeting == null)
                {
                    return NotFound(new { Message = "No meeting found" });
                }

                if(!meeting.CourseId.HasValue)
                {
                    return Ok(new {Message = "Nuk ka nevoje per evidentim."});
                }

                var progress = await _context.StudentCourseLessonProgress.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == meeting.CourseId);
                if(progress == null)
                {
                    return NotFound(new { Message = "No progress found" });
                }
                if (!progress.HasJoined)
                {
                    progress.JoinedTime = DateTime.UtcNow;
                    progress.HasJoined = true;
                    progress.LastModified = DateTime.UtcNow;
                _context.StudentCourseLessonProgress.Update(progress);
                await _context.SaveChangesAsync();
                }
                return Ok(new {Message = "Successfully joined"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting meeting");
                return BadRequest();
            }
        }

        //qiky thirret ne server te callit ne express
        [Authorize]
        [HttpGet("GetParticipantData")]
        public async Task<IActionResult> GetParticipantData()
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (user == null)
                {
                    return Unauthorized(new { Message = "User not authorized" });
                }
                var userId = Int32.Parse(user);

                var userData = await _context.Users
                    .Select(c => new
                    {
                        id = c.ID,
                        name = c.Firstname + " " + c.Lastname,
                        email = c.Email,
                        profilePicture = c.ProfilePictureUrl,
                        role = c.Role,
                        username = c.Username,
                    })
                    .FirstOrDefaultAsync(c => c.id == userId);

                if (userData == null)
                {
                    return NotFound(new { Message = "No user found" });
                }
                return Ok(userData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting participant data");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("GetMobileMeetingInformations/{id}")]
        public async Task<IActionResult> GetMeetingMobileInformations(int id)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var meeting = await _context.OnlineMeetings
                    .Where(c => c.ID == id)
                    .Select(c => new
                    {
                        c.ID,
                        Instructor = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        c.Instructor.User.ProfilePictureUrl,
                        c.InstructorId,
                        c.CourseId,
                        c.Title,
                        c.Description,
                        c.ScheduleDateTime,
                        c.DurationTime,
                        c.MeetingUrl,
                        IsAllowed = c.Instructor.InstructorStudents.Any(ins => ins.CourseId == c.CourseId && ins.UserId == userId),
                        StatusNumber = c.Status,
                        //Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende" : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar" : "Ka perfunduar",
                        Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ?  "Nuk ka filluar ende" 
                            : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar" 
                            : c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                            : c.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                        Course = c.Course != null ? c.Course : null,
                        Lesson = c.Lesson != null ? c.Lesson : null
                    })
                    .FirstOrDefaultAsync();

                if(meeting == null)
                {
                    return NotFound(new { Message = "No meeting found" });
                }

                return Ok(meeting);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting information");
                return BadRequest();
            }
        }

        // ky thirret ne nextjs per check
        [HttpGet("GetMeetingInformations/{meetingUrl}")]
        public async Task<IActionResult> GetMeetingInformations(string meetingUrl)
        {
            try
            {
                var meeting = await _context.OnlineMeetings
                    .Where(c => EF.Functions.Contains(c.MeetingUrl, $"\"{meetingUrl}*\""))
                    .Select(c => new
                    {
                        c.ID,
                        Instructor = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                        c.InstructorId,
                        c.CourseId,
                        c.Title,
                        c.Description,
                        c.ScheduleDateTime,
                        c.DurationTime,
                        Category = c.Course != null ? c.Course.Category.CategoryName : null,
                        StatusNumber = c.Status,
                        //Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende" : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar" : "Ka perfunduar",
                        Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                            : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                            : c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                            : c.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                        Course = c.Course != null ? c.Course.Name : null,
                        Lesson = c.Lesson != null ? c.Lesson.Title : null
                    })
                    .FirstOrDefaultAsync();

                if(meeting == null)
                {
                    return NotFound(new { Message = "No meeting found" });
                }
                return Ok(meeting);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting informations");
                return BadRequest();
                throw;
            }
        }

        //check per me lan a jo dueht me kqyr
        [Authorize]
        [HttpGet("GetAllowedParticipants/{onlineMeetUrl}")]
        public async Task<IActionResult> GetAllowedParticipants(string onlineMeetUrl)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userIdClaim) || !Int32.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { Message = "Not authorized" });
            }

            var onlineMeet = await _context.OnlineMeetings.AsNoTracking().Where(c => EF.Functions.Contains(c.MeetingUrl, $"\"{onlineMeetUrl}*\"")).FirstOrDefaultAsync();
            if(onlineMeet == null)
            {
                return BadRequest(new { Message = "No meeting found" });
            }
            var getAllowedUser = await _context.InstructorStudents
                .AsNoTracking()
                .Where(c => c.InstructorId == onlineMeet.InstructorId && c.UserId == userId)
                .FirstOrDefaultAsync();
            var ifInstructor = await _context.Instructors.Where(c => c.UserId == userId).FirstOrDefaultAsync();
            
            if(getAllowedUser == null && ifInstructor == null)
            {
                return NotFound(new {Message = "You are not allowed in this meeting two null"});
            }else if(getAllowedUser != null && ifInstructor == null)
            {
                return Ok(new { Message = "You are allowed in this meeting STUDENT" });
            }
            else if(getAllowedUser == null && ifInstructor != null)
            {
                return Ok(new { Message = "You are allowed in this meeting INSTRUCTOR" });
            }

            return NotFound(new { Message = "You are not allowed in this meeting" });
        }


        //remove student from meeting ???? logic
        [HttpPost("RemoveStudent")]
        public async Task<IActionResult> RemoveStudentFromMeeting([FromBody] RemoveStudentDto removeDto, CancellationToken token)
        {
            try
            {
                //todo: create logic
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing student");
                return BadRequest();
            }
        }

        [Authorize(Roles = "Instructor")]
        [HttpPost("CreateMeeting")]
        public async Task<IActionResult> CreateMeeting([FromBody] OnlineMeetingsDto meetingDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
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
                    InstructorId = c.Instructor.ID
                }).FirstOrDefaultAsync(c => c.ID == Int32.Parse(userId));

                if(user == null)
                {
                    return NotFound(new { Message = "No user found" });
                }
                var date = DateTime.UtcNow;
                var newMeeting = new OnlineMeetings
                {
                    CourseId = meetingDto.CourseId,
                    LessonId = meetingDto.LessonId,
                    Title = meetingDto.Title,
                    Description = meetingDto.Description,
                    ScheduleDateTime = meetingDto.ScheduleDateTime,
                    DurationTime = meetingDto.DurationTime,
                    MeetingUrl = Guid.NewGuid().ToString(),
                    InstructorId = user.InstructorId,
                    Status = MeetingStatus.Scheduled,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                //logic to create meeting url
                await _context.AddAsync(newMeeting, token);
                await _context.SaveChangesAsync(token);

                await transaction.CommitAsync(token);

                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error creating meeting");
                return BadRequest();
            }
        }


        //me ndrru meeting statusin ?? duhet mu hek nashta ky api
        [HttpPatch("MeetingStatus")]
        public async Task<IActionResult> ChangeMeetingStatus([FromBody] ChangeMeetingStatusDto meetingStatusDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var meeting = await _context.OnlineMeetings.FindAsync(meetingStatusDto.MeetingId, token);
                if(meeting == null)
                {
                    return NotFound(new { Message = "No meeting found" });
                }
                meeting.Status = meetingStatusDto.Status;
                meeting.LastModified = DateTime.UtcNow;

                _context.OnlineMeetings.Update(meeting);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Meeting status changed" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error changing status of meeting");
                return BadRequest();
            }
        }

        //do fixa qitu
        [HttpGet("AllMeetingsAttendedByStudentId")] //merri krejt kurset ku un jom student tek qai instruktor logic
        public async Task<IActionResult> GetAllMeetingsAttendedByStudentId([FromQuery] int userId)
        {
            try
            {
                var meetings = await _context.OnlineMeetings
                    .Where(c => c.Instructor.User.InstructorStudents.Any(i => i.UserId == userId))
                    .Select(c => new
                    {
                        c.ID,
                        Course = c.Course != null ? new
                        {
                            c.Course.ID,
                            c.Course.Name,
                            c.Course.Description,
                            c.Course.TopicsCovered,
                        } : null,
                        Lesson = c.Lesson != null ? new
                        {
                            c.Lesson.ID,
                            c.Lesson.Title,
                            Content = c.Lesson.Content != null ? c.Lesson.Content : null,
                            VideoUrl = c.Lesson.Video_Url != null ? c.Lesson.Video_Url : null
                        } : null,
                        c.ScheduleDateTime,
                        c.DurationTime,
                        Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                            : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                            : c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                            : c.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                        Instructor = new
                        {
                            Name = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                            ProfilePicture = c.Instructor.User.ProfilePictureUrl,
                            Students = c.Instructor.InstructorStudents.Count(),
                            Courses = c.Instructor.InstructorCourses.Count(),
                            Lessons = c.Instructor.InstructorCourses
                                .SelectMany(section => section.InstructorCourseSections)
                                .SelectMany(lesson => lesson.InstructorLessons)
                                .Count(),
                            MeetingsCompleted = c.Instructor.OnlineMeetings.Where(m => m.Status == MeetingStatus.Completed).Count(),
                        },
                        c.CreatedAt
                    })
                    .ToListAsync();

                if(meetings.Count == 0)
                {
                    return NotFound(new { Message = "No meetings found" });
                }

                return Ok(meetings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all meetings");
                return BadRequest();
            }
        }

        [Authorize]
        [HttpGet("AllMeetings")]
        public async Task<IActionResult> GetAllMeetings([FromQuery] SortQueryDto sortQueryDto, [FromQuery] PaginationDto paginationDto, [FromQuery] bool userActiveMeetingsSection = false)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(userId) || !Int32.TryParse(userId, out int userAuthed))
                {
                    return Unauthorized();
                }

                
                var query = userActiveMeetingsSection
                    ? _context.OnlineMeetings
                        .AsNoTracking()
                        .Where(c => c.Instructor.InstructorStudents.Any(ic => ic.UserId == userAuthed) && c.Status != MeetingStatus.Completed && c.Status != MeetingStatus.Cancelled) 
                    : _context.OnlineMeetings.AsNoTracking();

                query = sortQueryDto.IsEmpty() ? query.OrderByDescending(c => c.CreatedAt) : _sorterService.SortData(query, sortQueryDto);
                var totalCount = await query.CountAsync();

                paginationDto.Validate();

                var meetings = await query
                    .Skip(paginationDto.Skip)
                    .Take(paginationDto.Take)
                    .Select(c => new
                    {
                        c.ID,
                        Course = c.Course ?? null,
                        Lesson = c.Lesson ?? null,
                        c.Title,
                        c.Description,
                        c.MeetingUrl,
                        c.ViewCount,
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
                    })
                    .ToListAsync();

                if(meetings.Count == 0)
                {
                    return NotFound(new { Message = "No meetings found" });
                }
                bool hasMore = (paginationDto.Skip + meetings.Count) < totalCount;
                return Ok(new {meetings, hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all meetings");
                return BadRequest();
            }
        }
    }
}
