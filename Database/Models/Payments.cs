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
    public class Payments : BaseModel
    {
        [Required]
        [Column("PaymentID")]
        [JsonProperty("PaymentID")]
        public override int ID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int PackageID { get; set; }

        /*[Required]
        [ForeignKey("UserID")]
        [JsonIgnore]
        public virtual Users User { get; set; }*/

        [Required]
        public int PaymentValue { get; set; }

    }
}
