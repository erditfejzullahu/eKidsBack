using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IViewCountService
    {
        void IncrementViewCount(int id, string entityType);
        Task<int> GetViewCountAsync(int id, string entityType);
    }
}
