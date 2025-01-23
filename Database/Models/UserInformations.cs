using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class UserInformations : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int UserId { get; set; }
        public DateOnly? Birthday { get; set; }

        public string? SoftSkills { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public virtual ICollection<UserJobs> UserJobs { get; set; } = new List<UserJobs>();
        public virtual ICollection<UserEducations> UserEducations { get; set; } = new List<UserEducations>();

    }
}
