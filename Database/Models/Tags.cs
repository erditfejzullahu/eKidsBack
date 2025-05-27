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
        public int? Category_Id { get; set; }
        public int? Parent_Id { get; set; }

        public Tags Parent { get; set; }
        public ICollection<Tags> Children = new List<Tags>();

        public ICollection<Blogs> Blogs { get; set; } = new List<Blogs>();    
    }
}
