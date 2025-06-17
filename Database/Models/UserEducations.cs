using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class UserEducations : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string Place_Name { get; set; }
        public SchoolDegrees School_Degree { get; set; }
        public string Field { get; set; }
        public int Start_Year { get; set; }
        
        public int? End_Year { get; set; }

        public int UserInformationId { get; set; }

        [ForeignKey("UserInformationId")]
        public UserInformations UserInformation { get; set; }
    }
}
