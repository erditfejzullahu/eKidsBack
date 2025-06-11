using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateUser
    {
        [Required(ErrorMessage = "Firstname is required")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Firstname is required")]
        public string Lastname { get; set; }

        [Required(ErrorMessage = "username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number and one special character")]
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "age is required")]
        [Range(13, int.MaxValue, ErrorMessage = "Age must be at least 13")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Role required")]
        [RegularExpression("^(Student|Instructor)$", ErrorMessage = "Role must be either 'Student' or 'Instructor'")]
        public string Role { get; set; }

        public string? ProfilePictureUrl { get; set; }

    }


    /*public class CreateUserMeta
    {
        [Required(ErrorMessage = "MetaKey is required")]
        public string MetaKey { get; set; }

        [Required(ErrorMessage = "MetaValue is required")]
        public string MetaValue { get; set; }

    }*/
}
