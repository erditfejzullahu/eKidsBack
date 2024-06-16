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
    public class Categories : BaseModel
    {
        [Key]
        [Column("CategoryID")]
        [JsonProperty("CategoryID")]
        public override int ID { get; set; }
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; }

    }
}
