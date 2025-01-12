using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Database.Shared.Enums
{
    public enum BlogStatus
    {
        Public = 1,
        Private = 2,
        FriendOnly = 3,
        Deleted = 4
    }
}
