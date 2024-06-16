using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        private readonly IRepository<Packages> _packageRepository;

        public PackagesController(IRepository<Packages> packageRepository)
        {
            _packageRepository = packageRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackages(int id)
        {
            var package = await _packageRepository.Get(id, default);

            if(package == null)
            {
                return NotFound();
            }

            return Ok(package);
        }

        [HttpPost]
        public async Task<IActionResult> AddPackage([FromForm] CreatePackages packageDto)
        {
            if(packageDto == null)
            {
                return BadRequest("Package data is null");
            }

            var package = new Packages
            {
                PackageName = packageDto.PackageName,
                PackageContent = packageDto.PackageContent,
                PackageValue = packageDto.PackageValue,
                PackageFeatured = packageDto.PackageFeatured,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
            };

            _packageRepository.Add(package);
            await _packageRepository.SaveAsync(default);

            return Ok(package); 
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePackage(int id, [FromForm] UpdatePackages packageDto)
        {
            var package = await _packageRepository.Get(id, default);
            if(package == null)
            {
                return NotFound();
            }

            package.PackageName = packageDto.PackageName;
            package.PackageValue = packageDto.PackageValue;
            package.PackageFeatured = packageDto.PackageFeatured;
            package.LastModified = DateTime.UtcNow;

            _packageRepository.Update(package);
            await _packageRepository.SaveAsync(default);

            return Ok(package);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePackage(int id, CancellationToken token)
        {
            var package = await _packageRepository.Get(id, default);
            if(package == null)
            {
                return NotFound();
            }

            _packageRepository.Delete(package.ID, token);
            await _packageRepository.SaveAsync(default);

            return Ok(package);
        }
    }
}
