using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [Required(ErrorMessage = "password is required")]
        public string Password { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "age is required")]
        public int Age { get; set; }

        [Required(ErrorMessage = "PackageID requrired")]
        public int PackageID { get; set; }

        [Required(ErrorMessage = "Role required")]
        public string Role { get; set; }


        public string ProfilePictureUrl { get; set; }

    }

    /*public class CreateUserMeta
    {
        [Required(ErrorMessage = "MetaKey is required")]
        public string MetaKey { get; set; }

        [Required(ErrorMessage = "MetaValue is required")]
        public string MetaValue { get; set; }

    }*/
}
