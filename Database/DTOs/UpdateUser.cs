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

        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        [Required]
        public string Username { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number and one special character")]
        public string? Password { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number and one special character")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Email { get; set; }
        [Required]
        public int Age { get; set; }
        [Required]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }
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
