using Database.DTOs;
using Database.Repository;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

public class SorterService<T> : ISorterService<T> where T : class
{
    private readonly ILogger<SorterService<T>> _logger;

    public SorterService(ILogger<SorterService<T>> logger)
    {
        _logger = logger;
    }

    public IQueryable<T> SortData(IQueryable<T> query, SortQueryDto queryDto)
    {
        if (queryDto == null || query == null)
            return query;

        try
        {
            // Handle each sort field in priority order
            if (!string.IsNullOrEmpty(queryDto.SortByName))
            {
                query = ApplySort(query, queryDto.SortByName, queryDto.SortNameOrder);
            }

            if (!string.IsNullOrEmpty(queryDto.SortByDate))
            {
                query = ApplySort(query, queryDto.SortByDate, queryDto.SortDateOrder);
            }

            if (!string.IsNullOrEmpty(queryDto.SortByViews))
            {
                query = ApplySort(query, queryDto.SortByViews, queryDto.SortViewOrder);
            }

            return query;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sorting data");
            return query; // Return original query if sorting fails
        }
    }

    private IQueryable<T> ApplySort(IQueryable<T> query, string propertyName, string sortOrder)
    {
        if (string.IsNullOrEmpty(propertyName))
            return query;

        var propertyInfo = typeof(T).GetProperty(propertyName,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (propertyInfo == null)
        {
            _logger.LogWarning("Property {PropertyName} not found for sorting", propertyName);
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, propertyInfo);
        var keySelector = Expression.Lambda(property, parameter);

        var methodName = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
            ? "OrderByDescending"
            : "OrderBy";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), propertyInfo.PropertyType },
            query.Expression,
            Expression.Quote(keySelector));

        return query.Provider.CreateQuery<T>(resultExpression);
    }
}