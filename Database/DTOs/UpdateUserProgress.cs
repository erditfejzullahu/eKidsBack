using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateUserProgress
    {
        public int UserId { get; set; }

        public int LessonId { get; set; }

        public int CourseId { get; set; }

        public bool? IsCompleted { get; set; }

        public bool? HasStarted { get; set; }

    }
}
