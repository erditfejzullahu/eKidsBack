using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StoriesController : ControllerBase
    {
        private readonly IRepository<Stories> _storiesRepository;

        public StoriesController(IRepository<Stories> storiesRepository)
        {
            _storiesRepository = storiesRepository;
        }

        [HttpPost]
        public async Task<IActionResult> CreateStorie([FromForm] CreateStorie storieDto)
        {
            if(storieDto == null)
            {
                return BadRequest("Storie data is null");
            }

            var storie = new Stories
            {
                StorieName = storieDto.StorieName,
                StorieDescription = storieDto.StorieDescription,
                StorieCategory = storieDto.StorieCategory,
                StorieContent = storieDto.StorieContent,
                StorieURL = storieDto.StorieURL,
                StorieImage = storieDto.StorieImage,
                StorieVoice = storieDto.StorieVoice,
                StorieVoiceURL = storieDto.StorieVoiceURL,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };

            if (string.IsNullOrEmpty(storie.StorieVoiceURL))
            {
                storie.StorieVoiceURL = null; // Or assign a default value if needed
            }

            _storiesRepository.Add(storie);
            await _storiesRepository.SaveAsync(default);
            return Ok(storie);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStorie(int id, [FromForm] UpdateStorie storieDto)
        {
            var storie = await _storiesRepository.Get(id, default);
            if(storie == null)
            {
                return NotFound();
            }

            storie.StorieName = storieDto.StorieName;
            storie.StorieCategory = storieDto.StorieCategory;
            storie.StorieContent = storieDto.StorieContent;
            storie.StorieURL = storieDto.StorieURL;
            storie.StorieImage = storieDto.StorieImage;
            storie.StorieVoice = storieDto.StorieVoice;
            storie.StorieVoiceURL = storieDto.StorieVoiceURL;
            storie.LastModified = DateTime.UtcNow;

            

            _storiesRepository.Update(storie);
            await _storiesRepository.SaveAsync(default);

            return Ok(storie);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStorie(int id)
        {
            var storie = _storiesRepository.Get(id, default);

            if(storie == null)
            {
                return NotFound();
            }

            return Ok(storie);
        }

        /*[HttpPut("{id}")]
        public async Task<IActionResult> UpdateStorie(int id, [FromForm] UpdateStorie storieDto)
        {
            var storie = await _storiesRepository.Get(id, default);

            if(storie == null)
            {
                return NotFound();
            }

            storie.StorieName = storieDto.StorieName;
            storie.StorieCategory = storieDto.StorieCategory;
            storie.StorieDescription = storieDto.StorieDescription;
            storie.StorieContent = storieDto.StorieContent;
            storie.StorieURL = storieDto.StorieURL;
            storie.StorieImage = storieDto.StorieImage;
            storie.StorieVoice = storieDto.StorieVoice;
            storie.StorieVoiceURL = storieDto.StorieVoiceURL;

            _storiesRepository.Update(storie);
            await _storiesRepository.SaveAsync(default);
            return Ok(storie);
        }*/

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStorie(int id, CancellationToken token)
        {
            var storie = await _storiesRepository.Get(id, default);
            if(storie == null)
            {
                return NotFound();
            }

            _storiesRepository.Delete(storie.ID, token);
            await _storiesRepository.SaveAsync(default);

            return Ok(storie);
        }

    }
}
