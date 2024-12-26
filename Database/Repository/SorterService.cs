using Database.DTOs;
using Database.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class SorterService<T> : ISorterService<T> where T : class
    {
        private readonly ILogger<SorterService<T>> _logger;
        public SorterService(ILogger<SorterService<T>> logger)
        {
            _logger = logger;
        }
        public IQueryable<T> SortData(IQueryable<T> query, SortQueryDto queryDto)
        {
            if (queryDto == null) return query;

            IOrderedQueryable<T>? orderedQuery = null;

            var sortFields = new List<(string? SortBy, string? SortOrder)>()
            {
                (queryDto.SortByName, queryDto.SortNameOrder),
                (queryDto.SortByDate, queryDto.SortDateOrder),
                (queryDto.SortByViews, queryDto.SortViewOrder)
            };

            foreach (var (sortBy, sortOrder) in sortFields)
            {
                if (!string.IsNullOrEmpty(sortBy))
                {

                    var propertyInfo = typeof(T).GetProperty(sortBy,
                            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (propertyInfo != null)
                    {
                        var parameter = Expression.Parameter(typeof(T), "x");
                        var property = Expression.Property(parameter, sortBy);
                        var keySelector = Expression.Lambda(property, parameter);

                        var methodName = sortOrder?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";

                        _logger.LogInformation("Sorting by: {SortBy}, Order: {SortOrder}", sortBy, sortOrder);

                        if (orderedQuery == null)
                        {
                            orderedQuery = query.Provider.CreateQuery<T>(
                                Expression.Call(
                                    typeof(Queryable),
                                    methodName,
                                    new Type[] { typeof(T), property.Type },
                                    query.Expression,
                                    Expression.Quote(keySelector)
                                    )
                                ) as IOrderedQueryable<T>;
                        }
                        else
                        {
                            var thenMethodName = sortOrder?.ToLower() == "desc" ? "ThenByDescending" : "ThenBy";

                            orderedQuery = query.Provider.CreateQuery<T>(
                                Expression.Call(
                                    typeof(Queryable),
                                    "ThenBy",
                                    new Type[] { typeof(T), property.Type },
                                    query.Expression,
                                    Expression.Quote(keySelector)
                                    )
                                ) as IOrderedQueryable<T>;
                        }

                    }

                    return orderedQuery ?? query;

                }
            }

            return query;

        }
    }
}
