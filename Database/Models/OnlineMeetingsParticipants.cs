using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class OnlineMeetingsParticipants
    {
        public int OnlineMeetId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("OnlineMeetId")]
        public OnlineMeetings OnlineMeeting { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
    }
}
