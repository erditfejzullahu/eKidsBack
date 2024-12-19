using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IVideoFileService
    {
        bool isValidFile(IFormFile file);
        string SanitizeFileName(string fileName);
    }
}
