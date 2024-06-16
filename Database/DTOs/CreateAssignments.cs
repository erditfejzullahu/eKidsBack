using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CreateAssignments
    {
        [Required(ErrorMessage = "Missing UserID")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Missing AssignmentName")]
        public string AssignmentName { get; set; }

        [Required(ErrorMessage = "Missing AssignmentContent")]
        public string AssignmentContent { get; set; }

        [Required(ErrorMessage = "Missing AssignmentPath")]
        public string AssignmentPath { get; set; }

        [Required(ErrorMessage ="AssignmentChecked Missing")]
        public bool AssignmentChecked { get; set; }

        public string? AdminComment { get; set; }

    }

    public class AssignmentFile
    {
        [Required]
        public IFormFile Assignment {  get; set; }
    }
}
