using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class InstructorCourses : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set ; }
        public int InstructorId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TopicsCovered { get; set; }

        [ForeignKey("InstructorId")]
        public Instructors Instructor { get; set; }
        [ForeignKey("CategoryId")]
        public Categories Category { get; set; }

        public virtual ICollection<InstructorCourseSections> InstructorCourseSections { get; set; } = new List<InstructorCourseSections>();
        public virtual ICollection<InstructorStudents> InstructorStudents { get; set; } = new List<InstructorStudents>();

        public virtual ICollection<OnlineMeetings> OnlineMeetings { get; set; } = new List<OnlineMeetings>();
    }
}
