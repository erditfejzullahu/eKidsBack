using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class ReportTickets : BaseModel 
    {
        [Column("Id")]
        public override int ID { get ; set ; }
        public int UserId { get; set; }
        public int AvailableTicketId { get; set; }
        public int? ReportedUserId { get; set; }
        public string? OtherMessage { get; set; }

        [ForeignKey("UserId")]
        public Users UserSubmitted { get; set; }

        [ForeignKey("ReportedUserId")]
        public Users? ReportedUser { get; set; }

        [ForeignKey("AvailableTicketId")]
        public AvailableTickets AvailableTicket { get; set; }
    }
}
