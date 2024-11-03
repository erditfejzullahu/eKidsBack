using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateCategory
    {
        [Required(ErrorMessage = "Category Name is required!")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Category Slug is required!")]
        public string CategorySlug { get; set; }

        [Required(ErrorMessage = "Category Picture is required!")]
        public string CategoryPictureUrl { get; set; }


    }
}
