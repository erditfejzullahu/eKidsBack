using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Packages : BaseModel
    {
        [Required]
        [Column("PackageID")]
        [JsonProperty("PackageID")]
        public override int ID { get; set; }

        /*[Key]
        public int PackageID { get; set; }*/

        [Required]
        public string PackageName { get; set; }

        [Required]
        public string PackageContent { get; set; }

        [Required]
        public string PackageValue {  get; set; }

        [Required]
        public string PackageFeatured { get; set; }

    }
}
