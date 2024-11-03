using AutoMapper;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public CoursesController(IRepository<Courses> courseRepository, ILogger<CoursesController> logger, IFileUploadService fileUploadService, IMapper mapper)
        {
            _courseRepository = courseRepository;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourses courseDto)
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
                await _courseRepository.SaveAsync(default);
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
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _courseRepository.Get(id, default);
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
            var course = await _courseRepository.Get(id, default);
            if (course == null)
            {
                var errorMessage = new
                {
                    Message = "No course found with that id!"
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
            }
            await _courseRepository.Delete(course.ID, token);
            await _courseRepository.SaveAsync(default);
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
        public async Task<IActionResult> GetAllCoursesP(int page = 1, int pageSize = 10, string sortOrder = "asc", CancellationToken token = default)
        {
            try
            {
                if(page <= 0 || pageSize <= 0)
                {
                    return BadRequest(new { Message = "Value should be more than 0 " });
                }
                var totalCourses = await _courseRepository.CountAsync(token);
                var totalPages = (int)Math.Ceiling(totalCourses / (double)pageSize);

                if(page > totalPages)
                {
                    return BadRequest(new { Message = "Page number exceeds total pages!" });
                }

                IQueryable<Courses> coursesQuery = _courseRepository.GetAll();

                if(sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    coursesQuery = coursesQuery.OrderByDescending(c => c.CourseName);
                }
                else
                {
                    coursesQuery = coursesQuery.OrderBy(c => c.CourseName);
                }

                var courses = await coursesQuery
                    .Include(c => c.Lessons)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(token);

                var response = new
                {
                    TotalCourses = totalCourses,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    Courses = courses
                };

                return Ok(response);
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
                var courses = await _courseRepository.GetAll().ToListAsync(token);
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
