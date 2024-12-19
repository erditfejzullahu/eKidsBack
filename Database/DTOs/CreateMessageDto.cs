using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateMessageDto
    {
        public string SenderUsername { get; set; }
        public string ReceiverUsername { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
    }

}
