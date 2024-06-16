using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateUser
    {

        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? Role { get; set; }
        public string? ProfilePictureUrl { get; set; }

    }

    public class UpdateProfilePic
    {
        [Required]
        public string Base64Profile { get; set; }
    }

    public class UpdateUserPackageID
    {
        [Required]
        public int PackageID { get; set; }
    }
}
