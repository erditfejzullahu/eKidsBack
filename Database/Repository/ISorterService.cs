using Database.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public interface ISorterService<T> where T : class
    {
        IQueryable<T> SortData(IQueryable<T> query, SortQueryDto queryDto);
    }
}
