using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class UserJobs : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string Job_Place { get; set; }
        public string Job_Title { get; set; }
        public int Start_Year { get; set; }
        public int? End_Year { get; set; }

        public int UserInformationId { get; set; }

        [ForeignKey("UserInformationId")]
        public UserInformations UserInformation { get; set; }
    }
}
