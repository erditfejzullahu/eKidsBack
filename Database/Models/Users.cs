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
    public class Users : BaseModel
    {

        [Required]
        [Column("UserID")]
        [JsonProperty("UserID")]
        public override int ID { get; set; } 

        [Required]
        public string Firstname { get; set; }

        [Required]
        public string Lastname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [JsonIgnore]
        public int PackageID { get; set; }

        [Required]
        [JsonIgnore]
        public int PaymentID { get; set; }

        [JsonProperty("PackageInfo")]
        public virtual Packages Package { get; set; }

        [JsonProperty("UserMeta")]
        public virtual ICollection<Usermeta> UserMeta { get; set; }

        [JsonProperty("PaymentInfo")]
        public virtual Payments Payment { get; set; }

        [Required]
        public int Age { get; set; }

        public string ProfilePictureUrl { get; set; }

    }
}
