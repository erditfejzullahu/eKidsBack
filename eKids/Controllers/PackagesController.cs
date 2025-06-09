using Database.DTOs;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eKids.Controllers
{
    [Authorize(Roles = "Admin")]
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

        [HttpGet("AllPackages")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPackages(CancellationToken token)
        {
            var packages = await _packageRepository.GetAll().ToListAsync(token);

            if (packages == null)
            {
                return BadRequest("No users found");
            }
            return Ok(packages);
        }

        [HttpPost]
        public async Task<IActionResult> AddPackage([FromBody] CreatePackages packageDto)
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
        public async Task<IActionResult> UpdatePackage(int id, [FromBody] UpdatePackages packageDto)
        {
            var package = await _packageRepository.Get(id, default);
            if(package == null)
            {
                return NotFound();
            }

            package.PackageName = packageDto.PackageName;
            package.PackageValue = packageDto.PackageValue;
            package.PackageContent = packageDto.PackageContent;
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
