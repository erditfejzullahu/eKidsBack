using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Database.Models
{
    public class Stories : BaseModel
    {
        [Required]
        [Column("StorieID")]
        [JsonPropertyName("StorieID")]
        public override int ID { get; set; }

        [Required]
        public string StorieName { get; set; }

        [Required]
        public string StorieDescription { get; set; }
        
        [Required]
        public string StorieCategory {  get; set; }

        [Required]
        public string StorieContent { get; set; }

        [Required]
        public string StorieURL { get; set; }

        [Required]
        public string StorieImage { get; set; }

        [Required]
        public bool StorieVoice { get; set; }

        public string? StorieVoiceURL { get; set; }

    }
}
