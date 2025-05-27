using AutoMapper;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Categories> _categoryRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<CategoriesController> _logger;
        private readonly IMapper _mapper;
        private readonly ISorterService<Categories> _sortService;

        public CategoriesController(IRepository<Categories> categoryRepository, ISorterService<Categories> sortService, IFileUploadService fileUploadService, ILogger<CategoriesController> logger, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _fileUploadService = fileUploadService;
            _logger = logger;
            _mapper = mapper;
            _sortService = sortService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategory categoryDto)
        {
            if(categoryDto == null)
            {
                return BadRequest("Category data is null");
            }

            try
            {
                string relativeUrl = await _fileUploadService.UploadFile(categoryDto.CategoryPictureUrl, FileCategory.Other);
                var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

                var category = new Categories
                {
                    CategoryName = categoryDto.CategoryName,
                    CategorySlug = categoryDto.CategorySlug,
                    CategoryPictureUrl = url,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                };

                _categoryRepository.Add(category);
                await _categoryRepository.SaveAsync(default);
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                var errorResponse = new
                {
                    Message = "Error in creating category.",
                    Details = "Please try again later or check all fields."
                };
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategories categoryDto)
        {
            var category = await _categoryRepository.Get(id, default);

            if(category == null)
            {
                return NotFound();
            }

            category.CategoryName = categoryDto.CategoryName;
            category.CategorySlug = categoryDto.CategorySlug;
            if(!string.IsNullOrEmpty(categoryDto.CategoryPictureUrl))
            {
                try
                {
                    string relativeUrl = await _fileUploadService.UploadFile(categoryDto.CategoryPictureUrl, FileCategory.Other);
                    var url = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

                    category.CategoryPictureUrl = url;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in updating category");
                    var errorMessage = new
                    {
                        Message = "Error in updating category",
                    };
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }
            }

            _categoryRepository.Update(category);
            await _categoryRepository.SaveAsync(default);

            return Ok(category);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            //var category = await _categoryRepository.Get(id, default);
            var category = await _categoryRepository
                .GetAll()
                .Include(c => c.Courses)
                .FirstOrDefaultAsync(c => c.ID == id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpGet("/getCategories")]
        public async Task<IActionResult> getAllCategories([FromQuery] string? searchParam, [FromQuery] SortQueryDto sortQuery, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {

            var totalCount = await _categoryRepository.CountAsync();
            var query = _categoryRepository.GetAll().AsNoTracking();

            if(query == null)
            {
                return NotFound(new {Message = "No categories found!"});
            }

            if (!string.IsNullOrEmpty(searchParam))
            {
                query = query.Where(c => EF.Functions.Contains(c.CategoryName, $"\"{searchParam}*\""));
            }

            var sortedQuery = _sortService.SortData(query, sortQuery);
            sortedQuery.Skip(paginationDto.Skip).Take(paginationDto.Take);    

            var categories = await sortedQuery.Include(c => c.Courses).ToListAsync(token);
            if(!categories.Any())
            {
                return NotFound(new { Message = "No categories found!" });
            }

            bool hasMore = (paginationDto.Skip + categories.Count) < totalCount;


            return Ok(new {categories, hasMore});
            
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken token)
        {
            var category = await _categoryRepository.Get(id, default);
            if(category == null)
            {
                return NotFound();
            }
            await _categoryRepository.Delete(category.ID, token);
            await _categoryRepository.SaveAsync(default);

            return Ok(category);
        }

    }
}
