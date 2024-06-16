using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Database.Models
{
    public class Usermeta : BaseModel
    {
        [Required]
        [Column("UMetaID")]
        [JsonProperty("UMetaID")]
        public override int ID { get; set; }

        [Required]
        [Column("UserMID")]
        public int UserID { get; set; }

        [Required]
        public string MetaKey { get; set; }

        [Required]
        public string MetaValue { get; set; }

        [ForeignKey("UserID")]
        [JsonIgnore]
        public virtual Users User { get; set; }

        [NotMapped]
        [JsonIgnore]
        public override DateTime CreatedAt { get => base.CreatedAt; set => base.CreatedAt = value; }

        [NotMapped]
        [JsonIgnore]
        public override DateTime LastModified { get => base.LastModified; set => base.LastModified = value; }
    }
}
