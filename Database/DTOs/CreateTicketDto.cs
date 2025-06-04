using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateReportSupportTicketDto
    {
        [Required]
        public int AvailableTicketId { get; set; }
        [Required]
        public int TicketCreatorUserId { get; set; }
        public int? ReportedUserId { get; set; }
        public string? OtherMessage { get; set; }
    }

    public class CreateAvailableTicketDto
    {
        [Required]
        public string TicketTitle { get; set; }
        [Required]
        public AvailableTicketsTypes TicketType { get; set; }

    }
}
