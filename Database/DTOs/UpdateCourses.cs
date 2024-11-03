using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateCourses
    {

        public string? CourseName { get; set; }

        public string? CourseDescription { get; set; }

        public string? CourseFeaturedImage { get; set; }

        public int? CourseCategory { get; set; }
    }
}
