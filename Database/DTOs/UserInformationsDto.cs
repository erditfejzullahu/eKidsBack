using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class UserInformationsDto
    {
        public int UserId { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? SoftSkills { get; set; }
        public string? Skills { get; set; }
        public List<UserEducationsDto> UserEducations { get; set; } = new List<UserEducationsDto>();
        public List<UserJobsDto> UserJobs { get; set; } = new List<UserJobsDto>();

    }

    public class UserEducationsDto
    {
        public int? ID { get; set; }
        [MaxLength(255)]
        public string Place_Name { get; set; }
        public SchoolDegrees SchoolDegree { get; set; }

        [MaxLength(255)]
        public string Field { get; set; }
        public int Start_Year { get; set; }
        public int? End_Year { get; set; }
    }
    public class UserJobsDto
    {
        public int? ID { get; set; }
        [MaxLength(255)]
        public string Job_Place { get; set; }

        [MaxLength(255)]
        public string Job_Title { get; set; }
        public int Start_Year { get; set; }
        public int? End_Year { get; set; }
    }
}
