using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateStorie
    {
        [Required(ErrorMessage = "StorieName is required")]
        public string StorieName { get; set; }

        [Required(ErrorMessage = "StorieDescription is required")]
        public string StorieDescription { get; set; }

        [Required(ErrorMessage = "StorieCategory is required")]
        public string StorieCategory { get; set; }

        [Required(ErrorMessage = "StorieContent is required")]
        public string StorieContent { get; set; }

        [Required(ErrorMessage = "StorieURL is required")]
        public string StorieURL { get; set; }

        [Required(ErrorMessage = "StorieImage is required")]
        public string StorieImage {  get; set; }

        [Required(ErrorMessage = "StorieVoice is required")]
        public bool StorieVoice { get; set; }

        
        public string? StorieVoiceURL { get; set; }

    }
}
