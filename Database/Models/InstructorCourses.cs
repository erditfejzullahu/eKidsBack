using Database.Shared.Enums;
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
        public string? Image { get; set; }
        public string Description { get; set; }
        public InstructorCoursesLevels Level { get; set; }
        public string TopicsCovered { get; set; }

        [ForeignKey("InstructorId")]
        public Instructors Instructor { get; set; }
        [ForeignKey("CategoryId")]
        public Categories Category { get; set; }

        public ICollection<InstructorCourseSections> InstructorCourseSections { get; set; } = new List<InstructorCourseSections>();
        public ICollection<InstructorStudents> InstructorStudents { get; set; } = new List<InstructorStudents>();

        public ICollection<OnlineMeetings> OnlineMeetings { get; set; } = new List<OnlineMeetings>();

        public ICollection<StudentCourseLessonProgress> CourseLessonProgresses { get; set; } = new List<StudentCourseLessonProgress>();
    }
}
