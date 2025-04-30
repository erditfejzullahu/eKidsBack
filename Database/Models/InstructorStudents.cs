using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class InstructorStudents
    {
        public int InstructorId { get; set; }
        public int UserId { get; set; }
        public int? CourseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
        [ForeignKey("CourseId")]
        public InstructorCourses InstructorCourse { get; set; }
        [ForeignKey("InstructorId")]
        public Instructors Instructor { get; set; }
    }
}
