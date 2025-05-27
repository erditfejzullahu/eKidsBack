using Database.DTOs;
using Database.Repository;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;

public class SorterService<T> : ISorterService<T> where T : class
{
    private readonly ILogger<SorterService<T>> _logger;
    private static readonly ConcurrentDictionary<string, LambdaExpression> _propertyAccessorCache = new();

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
            var sortExpressions = new List<(string property, bool descending)>();

            if (!string.IsNullOrEmpty(queryDto.SortByName))
                sortExpressions.Add((queryDto.SortByName, IsDescending(queryDto.SortNameOrder)));

            if (!string.IsNullOrEmpty(queryDto.SortByDate))
                sortExpressions.Add((queryDto.SortByDate, IsDescending(queryDto.SortDateOrder)));

            if (!string.IsNullOrEmpty(queryDto.SortByViews))
                sortExpressions.Add((queryDto.SortByViews, IsDescending(queryDto.SortViewOrder)));

            return sortExpressions.Count == 0
                ? query
                : ApplyMultiSort(query, sortExpressions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sorting data");
            return query;
        }
    }

    private static bool IsDescending(string sortOrder) =>
        "desc".Equals(sortOrder, StringComparison.OrdinalIgnoreCase);

    private IQueryable<T> ApplyMultiSort(IQueryable<T> query, List<(string property, bool descending)> sorts)
    {
        if (sorts.Count == 0) return query;

        var firstSort = sorts[0];
        var orderedQuery = firstSort.descending
            ? query.OrderByDescending(CreatePropertyExpression<T>(firstSort.property))
            : query.OrderBy(CreatePropertyExpression<T>(firstSort.property));

        for (int i = 1; i < sorts.Count; i++)
        {
            var sort = sorts[i];
            orderedQuery = sort.descending
                ? orderedQuery.ThenByDescending(CreatePropertyExpression<T>(sort.property))
                : orderedQuery.ThenBy(CreatePropertyExpression<T>(sort.property));
        }

        return orderedQuery;
    }

    private static Expression<Func<T, TKey>> CreatePropertyExpression<TKey>(string propertyName)
    {
        var cacheKey = $"{typeof(T).Name}_{propertyName}_{typeof(TKey).Name}";

        if (_propertyAccessorCache.TryGetValue(cacheKey, out var cachedExpression))
        {
            return (Expression<Func<T, TKey>>)cachedExpression;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, propertyName);
        var lambda = Expression.Lambda<Func<T, TKey>>(property, parameter);

        _propertyAccessorCache.TryAdd(cacheKey, lambda);
        return lambda;
    }
}