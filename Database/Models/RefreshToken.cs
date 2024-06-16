using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class RefreshToken : BaseModel
    {
        [Key]
        [Column("ID")]
        [JsonProperty("TokenID")]
        public override int ID { get; set; }
        public string UserID { get; set; }
        public string Token {  get; set; }

        public DateTime ExpiryDate { get; set; }

        [NotMapped]
        public override DateTime CreatedAt { get; set; }

        [NotMapped]
        public override DateTime LastModified { get; set; }

    }
}
