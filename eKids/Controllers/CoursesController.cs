using AutoMapper;
using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly IRepository<Courses> _courseRepository;
        private readonly ILogger<CoursesController> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly IMapper _mapper;
        private readonly ISorterService<Courses> _sorterService;
        private readonly ApplicationDbContext _context;

        public CoursesController(IRepository<Courses> courseRepository, ApplicationDbContext context, ILogger<CoursesController> logger, IFileUploadService fileUploadService, IMapper mapper, ISorterService<Courses> sorterService)
        {
            _courseRepository = courseRepository;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
            _sorterService = sorterService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourses courseDto, CancellationToken token)
        {
            if(courseDto == null)
            {
                return BadRequest("Course data is null");
            }

            try
            {
                string relativeUrl = await _fileUploadService.UploadFile(courseDto.CourseFeaturedImage, FileCategory.Other);
                var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

                var course = new Courses
                {
                    CourseName = courseDto.CourseName,
                    CourseDescription = courseDto.CourseDescription,
                    CourseFeaturedImage = url,
                    CourseCategory = courseDto.CourseCategory,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _courseRepository.Add(course);
                await _courseRepository.SaveAsync(token);
                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating course!");
                var errorMessage = new
                {
                    Message = "Error in creating course."
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourse(int id, CancellationToken token)
        {
            var course = await _courseRepository.Get(id, token, c => c.Lessons, c => c.Category);
            
                
            if(course == null)
            {
                var errorMessage = new
                {
                    Message = "No course found with that id!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            return Ok(course);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id, CancellationToken token)
        {
            var course = await _courseRepository.Get(id, token);
            if (course == null)
            {
                var errorMessage = new
                {
                    Message = "No course found with that id!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            await _courseRepository.Delete(course.ID, token);
            await _courseRepository.SaveAsync(token);
            return Ok(course);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourses courseDto)
        {
            try
            {
                var course = await _courseRepository.Get(id, default);
                if (course == null)
                {
                    var errorMessage = new
                    {
                        Message = "No course found with that id!"
                    };
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }

                _mapper.Map(courseDto, course);

                if(!string.IsNullOrEmpty(courseDto.CourseFeaturedImage))
                {
                    try
                    {
                        string relativeUrl = await _fileUploadService.UploadFile(courseDto.CourseFeaturedImage, FileCategory.Other);
                        var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
                        course.CourseFeaturedImage = url;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in updating course Image");
                        var errorMessage = new
                        {
                            Message = "Error in updating course image",
                        };
                        return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                    }
                }

                _courseRepository.Update(course);
                await _courseRepository.SaveAsync(default);

                return Ok(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in updating course with ID: {id}");
                var errorMessage = new
                {
                    Message = "Error in updating course",
                };

                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
        }

        [HttpGet("/getCoursesP")]
        public async Task<IActionResult> GetAllCoursesP([FromQuery] PaginationDto paginationDto, [FromQuery] SortQueryDto sortQuery, string? searchParam, int? categoryId, CancellationToken token)
        {
            try
            {
                //var totalCourses = categoryId.HasValue
                //    ? await _courseRepository.CountAsync(c => c.CourseCategory == categoryId, token)
                //    : await _courseRepository.CountAsync(token: token);
                var coursesQuery = _context.Courses.AsNoTracking();
                
                var totalCount = await coursesQuery.CountAsync(token);

                if (categoryId.HasValue)
                {
                    coursesQuery = coursesQuery.Where(c => c.CourseCategory == categoryId);
                }

                if(!string.IsNullOrEmpty(searchParam))
                {
                    coursesQuery = coursesQuery.Where(c => EF.Functions.Contains(c.CourseName, $"\"{searchParam}*\""));
                }

                var sortedQuery = _sorterService.SortData(coursesQuery, sortQuery);

                paginationDto.Validate();
                
                var courses = await sortedQuery
                    .Include(c => c.Lessons)
                .ToListAsync(token);

                bool hasMore = (paginationDto.Skip + courses.Count) < totalCount;

                return Ok(new {courses, hasMore});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retrieving Courses");
                var errorMessage = new
                {
                    Message = "Error in retrieving courses!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }           
        }

        [HttpGet("/getCourses")]
        public async Task<IActionResult> GetAllCourses(CancellationToken token)
        {
            try
            {
                var courses = await _courseRepository.GetAll().AsNoTracking().ToListAsync(token);
                if(courses == null)
                {
                    return BadRequest(new { Message = "No courses found!" });
                }
                return Ok(courses);
            }
            catch (Exception ex)
            {
                var errorMessage = new
                {
                    Message = "Error in retrieving courses!"
                };
                _logger.LogError(ex, "Error in retrieving all courses");
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
        }
    }
}
