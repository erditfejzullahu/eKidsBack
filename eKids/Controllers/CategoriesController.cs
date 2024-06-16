using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Policy;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly IRepository<Categories> _categoryRepository;

        public CategoriesController(IRepository<Categories> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategory categoryDto)
        {
            if(categoryDto == null)
            {
                return BadRequest("Category data is null");
            }

            var category = new Categories
            {
                CategoryName = categoryDto.CategoryName,
                CategorySlug = categoryDto.CategorySlug,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _categoryRepository.Add(category);
            await _categoryRepository.SaveAsync(default);
            return Ok(category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategories categoryDto)
        {
            var category = await _categoryRepository.Get(id, default);

            if(category == null)
            {
                return NotFound();
            }

            category.CategoryName = categoryDto.CategoryName;
            category.CategorySlug = categoryDto.CategorySlug;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveAsync(default);

            return Ok(category);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _categoryRepository.Get(id, default);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        [HttpGet("/getCategories")]
        public async Task<IActionResult> getAllCategories()
        {

            var categories = await _categoryRepository.GetAll().ToListAsync();
            if(categories == null)
            {
                return NotFound("Nocategories or smth error");
            }

            return Ok(categories);
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
