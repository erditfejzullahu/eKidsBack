using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class InstructorCourseSections : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int Course_Id { get; set; }
        public string Title { get; set; }

        [ForeignKey("Course_Id")]
        public InstructorCourses InstructorCourses { get; set; }
        public ICollection<InstructorLessons> InstructorLessons { get; set; } = new List<InstructorLessons>();
    }
}
