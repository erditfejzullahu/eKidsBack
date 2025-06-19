using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface IStatisticsService
    {
        Task<int[]> GetStatisticsBasedOfType(StatisticsType type, int year, int userId);
    }
}
