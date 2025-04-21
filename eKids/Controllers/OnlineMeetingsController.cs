using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnlineMeetingsController : ControllerBase
    {
        private readonly ILogger<OnlineMeetingsController> _logger;
        private readonly ApplicationDbContext _context;

        public OnlineMeetingsController(ILogger<OnlineMeetingsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

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

        [HttpPost("CreateMeeting")]
        public async Task<IActionResult> CreateMeeting([FromBody] OnlineMeetingsDto meetingDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = await _context.Instructors.Where(c => c.UserId == meetingDto.UserId).FirstOrDefaultAsync(token);
                if(user == null)
                {
                    return NotFound(new { Message = "No user found" });
                }
                var newMeeting = new OnlineMeetings
                {
                    CourseId = meetingDto.CourseId,
                    LessonId = meetingDto.LessonId,
                    Title = meetingDto.Title,
                    Description = meetingDto.Description,
                    ScheduleDateTime = meetingDto.ScheduleDateTime,
                    DurationTime = meetingDto.DurationTime,
                    MeetingUrl = Guid.NewGuid().ToString(),
                    InstructorId = user.ID,
                    Status = MeetingStatus.Scheduled,
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

        [HttpGet("AllMeetings")] //merri krejt kurset ku un jom student tek qai instruktor logic
        public async Task<IActionResult> GetAllMeetings([FromQuery] int userId)
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
                        c.Status,
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

    }
}
