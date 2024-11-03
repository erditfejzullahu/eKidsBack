using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateCourses
    {
        [Required(ErrorMessage = "Missing Course Name")]
        public string CourseName { get; set; }

        [Required(ErrorMessage = "Missing Course Description")]
        public string CourseDescription { get; set; }

        [Required(ErrorMessage = "Missing Course Category")]
        public int CourseCategory { get; set; }

        [Required(ErrorMessage = "Missing Course Image")]
        public string CourseFeaturedImage { get; set; }
    }
}
