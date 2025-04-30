using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class StudentCourseLessonProgress
    {
        public int UserId { get; set; }
        public int OnlineMeetId { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? JoinedTime { get; set; }
        public bool HasJoined { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
        [ForeignKey("OnlineMeetId")]
        public OnlineMeetings OnlineMeeting { get; set; }
    }
}
