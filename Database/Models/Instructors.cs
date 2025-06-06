using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Instructors : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int UserId { get; set; }
        public string Expertise { get; set; }
        public string Bio { get; set; }
        public string Socials { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
        public ICollection<Conversations> InstructorConversations { get; set; } = new List<Conversations>();
        public ICollection<InstructorStudents> InstructorStudents { get; set; } = new List<InstructorStudents>();
        public ICollection<InstructorCourses> InstructorCourses { get; set; } = new List<InstructorCourses>();

        public ICollection<OnlineMeetings> OnlineMeetings { get; set; } = new List<OnlineMeetings>();
    }
}
