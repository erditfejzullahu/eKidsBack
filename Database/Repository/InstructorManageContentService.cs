using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Database.Repository
{
    public class ManageInstructorContentService : IManageInstructorContentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ManageInstructorContentService> _logger;

        public ManageInstructorContentService(ApplicationDbContext context, ILogger<ManageInstructorContentService> logger)
        {
                _context = context;
            _logger = logger;
        }

        
        private class CourseResult
        {
            public int ID { get; set; }
            public int InstructorId { get; set; }
            public string InstructorName { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string? Image { get; set; }
            public int ViewCount { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public InstructorCoursesLevels Level { get; set; }
            public string TopicsCovered { get; set; }
            public List<string> SectionTitles { get; set; }
            public List<List<string>> SectionLessons { get; set; }
            public int CategoryId { get; set; }
            public Instructors Instructor { get; set; }
            public int EnrolledStudents { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class StudentResult
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
        }

        private class MeetingResult
        {
            public int ID { get; set; }
            public InstructorCourses? Course { get; set; }
            public InstructorLessons? Lesson { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string MeetingUrl { get; set; }
            public DateTime ScheduleDateTime { get; set; }
            public int? DurationTime { get; set; }
            public string Status { get; set; }
            public int ViewCount { get; set; }
            public int Participants { get; set; }
            public MeetingInstructor Instructor { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        private class MeetingInstructor
        {
            public string Name { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
        }


        public async Task<(List<dynamic>, bool hasMore)> RetrieveInstructorActivities(InstructorManageUserDto userDto, InstructorsManageContentType manageType, SortQueryDto sortQueryDto, PaginationDto paginationDto)
        {
            try
            {
                switch (manageType)
                {
                    case InstructorsManageContentType.Courses:
                        var (courses, coursesHasMore) = await GetCourses(userDto, sortQueryDto, paginationDto);
                        return (courses.Cast<dynamic>().ToList(), coursesHasMore);
                    case InstructorsManageContentType.Students:
                        var (students, studentsHasMore) = await GetStudents(userDto, sortQueryDto, paginationDto);
                        return (students.Cast<dynamic>().ToList(), studentsHasMore);
                    case InstructorsManageContentType.Meetings:
                        var (meetings, meetingsHasMore) = await GetMeetings(userDto, sortQueryDto, paginationDto);
                        return (meetings.Cast<dynamic>().ToList(), meetingsHasMore);
                    default:
                        throw new ApplicationException("Invalid content type specified");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving data");
                throw new ApplicationException(ex.Message);
            }
        }

        private async Task<(List<MeetingResult>, bool hasMore)> GetMeetings(InstructorManageUserDto userDto, SortQueryDto sortQueryDto, PaginationDto paginationDto)
        {
            try
            {
                var query = _context.OnlineMeetings.AsNoTracking().Where(c => c.InstructorId == userDto.InstructorId);
                var totalCount = await query.CountAsync();

                if (!sortQueryDto.IsEmpty())
                {
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByName))
                    {
                        if(sortQueryDto.SortNameOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.Title);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.Title);
                        }
                    }
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByViews))
                    {
                        if(sortQueryDto.SortViewOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.ViewCount);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.ViewCount);
                        }
                    }
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByDate))
                    {
                        if(sortQueryDto.SortDateOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.CreatedAt);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.CreatedAt);
                        }
                    }
                }
                else
                {
                    query = query.OrderByDescending(c => c.CreatedAt);
                }
                paginationDto.Validate();
                query = query.Skip(paginationDto.Skip).Take(paginationDto.Take);
                var result = await query
                    .Select(c => new MeetingResult
                    {
                        ID = c.ID,
                        Course = c.Course ?? null,
                        Lesson = c.Lesson ?? null,
                        Title = c.Title,
                        Description = c.Description,
                        MeetingUrl = c.MeetingUrl,
                        ViewCount = c.ViewCount,
                        ScheduleDateTime = c.ScheduleDateTime,
                        DurationTime = c.DurationTime ?? null,
                        Status = c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime > DateTime.UtcNow ? "Nuk ka filluar ende"
                        : c.Status == MeetingStatus.Cancelled ? "Eshte anuluar"
                        : c.Status == MeetingStatus.Scheduled && c.ScheduleDateTime < DateTime.UtcNow ? "Nuk eshte mbajtur(Mungese Instruktori)"
                        : c.Status == MeetingStatus.Started ? "Ka filluar" : "Ka perfunduar",
                        Participants = c.OnlineMeetingsParticipants.Count(),
                        Instructor = new MeetingInstructor
                        {
                            Name = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                            ProfilePictureUrl = c.Instructor.User.ProfilePictureUrl,
                            Username = c.Instructor.User.Username,
                            Email = c.Instructor.User.Email
                        },
                        CreatedAt = c.CreatedAt
                    }).ToListAsync();
                bool hasMore = (paginationDto.Skip + result.Count) < totalCount;
                return (result, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting mettings");
                throw new ApplicationException(ex.Message);
            }
        }

        private async Task<(List<StudentResult>, bool hasMore)> GetStudents(InstructorManageUserDto userDto, SortQueryDto sortQueryDto, PaginationDto paginationDto)
        {
            try
            {
                var query = _context.InstructorStudents.AsNoTracking().Where(c => c.InstructorId == userDto.InstructorId);
                var totalCount = await query.CountAsync();

                paginationDto.Validate();
                if (sortQueryDto.IsEmpty())
                {
                    query = query.OrderByDescending(c => c.CreatedAt);
                }
                else
                {
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByName))
                    {
                        if(sortQueryDto.SortNameOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.User.Firstname);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.User.Firstname);
                        }
                    }
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByDate))
                    {
                        if(sortQueryDto.SortDateOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.CreatedAt);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.CreatedAt);
                        }
                    }

                    //one logic for how much courses student has attended
                }

                query = query.Skip(paginationDto.Skip).Take(paginationDto.Take);
                var result = await query
                    .Select(c => new StudentResult
                    {
                        Id = c.User.ID,
                        Name = c.User.Firstname + " " + c.User.Lastname,
                        ProfilePictureUrl = c.User.ProfilePictureUrl,
                        Username = c.User.Username,
                        Email = c.User.Email
                    }).ToListAsync();
                bool hasMore = (paginationDto.Skip + result.Count) < totalCount;
                return (result, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving students");
                throw new ApplicationException(ex.Message);
            }
        }
                
        private async Task<(List<CourseResult>, bool hasMore)> GetCourses(InstructorManageUserDto userDto, SortQueryDto sortQueryDto, PaginationDto paginationDto)
        {
            try
            {
                var query = _context.InstructorCourses.AsNoTracking().Where(c => c.InstructorId == userDto.InstructorId);
                var totalCount = await query.CountAsync();

                if (sortQueryDto.IsEmpty())
                {
                    query = query.OrderByDescending(c => c.CreatedAt);
                }
                else
                {
                    if (!string.IsNullOrEmpty(sortQueryDto.SortByName))
                    {
                        if(sortQueryDto.SortNameOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.Name);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.Name); 
                        }
                    }

                    if (!string.IsNullOrEmpty(sortQueryDto.SortByDate))
                    {
                        if(sortQueryDto.SortDateOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.CreatedAt);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.CreatedAt);
                        }
                    }

                    if (!string.IsNullOrEmpty(sortQueryDto.SortByViews))
                    {
                        if(sortQueryDto.SortViewOrder == "desc")
                        {
                            query = query.OrderByDescending(c => c.ViewCount);
                        }
                        else
                        {
                            query = query.OrderBy(c => c.ViewCount);
                        }
                    }
                }
                var result = await query.Select(c => new CourseResult
                {
                    ID = c.ID,
                    InstructorId = c.InstructorId,
                    InstructorName = c.Instructor.User.Firstname + " " + c.Instructor.User.Lastname,
                    ProfilePictureUrl = c.Instructor.User.ProfilePictureUrl,
                    Image = c.Image,
                    Name = c.Name,
                    Description = c.Description,
                    Level = c.Level,
                    ViewCount = c.ViewCount,
                    TopicsCovered = c.TopicsCovered,
                    SectionTitles = c.InstructorCourseSections.Select(ic => ic.Title).ToList(),
                    SectionLessons = c.InstructorCourseSections
                        .Select(ic => ic.InstructorLessons.Select(il => il.Title).ToList())
                        .ToList(),
                    CategoryId = c.CategoryId,
                    Instructor = c.Instructor,
                    EnrolledStudents = c.InstructorStudents.Count(),
                    CreatedAt = c.CreatedAt,
                }).ToListAsync();
                bool hasMore = (paginationDto.Skip + result.Count) < totalCount;
                return (result, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving instructor courses");
                throw new ApplicationException(ex.Message);
            }
        }
    }
}
