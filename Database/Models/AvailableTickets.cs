using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class AvailableTickets : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string TicketTitle { get; set; }
        public AvailableTicketsTypes TicketTypes { get; set; }
        public ICollection<ReportTickets> Tickets { get; set; }  = new List<ReportTickets>();
    }
}
