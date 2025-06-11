using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Tags : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string Name { get; set; }

        public ICollection<BlogsWithTags> BlogTags { get; set; } = new List<BlogsWithTags>();
    }
}
