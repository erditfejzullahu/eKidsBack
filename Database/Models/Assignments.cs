using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Database.Models
{
    public class Assignments : BaseModel
    {
        [Required]
        [Column("AssignmentsID")]
        [JsonProperty("AssignmentID")]
        public override int ID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public string AssignmentName { get; set; }

        [Required]
        public string AssignmentContent { get; set; }

        [Required]
        public string AssignmentPath { get; set; }

        public string? AdminComment { get; set; }

        [Required]
        public bool AssignmentChecked { get; set; }

        [Required]
        [ForeignKey("UserID")]
        [JsonIgnore]
        public Users User { get; set; }
        
    }
}
