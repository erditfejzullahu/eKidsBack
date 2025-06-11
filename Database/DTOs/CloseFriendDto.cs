using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CloseFriendDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int CloseFriendId { get; set; }
    }

    public class FriendDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int FriendId { get; set; }
    }
}
