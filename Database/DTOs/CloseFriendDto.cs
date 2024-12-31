using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class CloseFriendDto
    {
        public int UserId { get; set; }
        public int CloseFriendId { get; set; }
    }

    public class FriendDto
    {
        public int UserId { get; set; }
        public int FriendId { get; set; }
    }
}
