using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UpdateStorie
    {
        public string StorieName { get; set; }
        public string StorieDescription { get; set; }
        public string StorieCategory { get; set; }
        public string StorieContent { get; set; }
        public string StorieURL { get; set; }
        public string StorieImage { get; set; }
        public bool StorieVoice { get; set; }
        public string? StorieVoiceURL { get; set; }

    }
}
