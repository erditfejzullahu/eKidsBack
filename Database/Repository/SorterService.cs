using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Database.DTOs;
using Database.Repository;
using Microsoft.Extensions.Logging;

public class SorterService<T> : ISorterService<T> where T : class
{
    private readonly ILogger<SorterService<T>> _logger;
    private static readonly ConcurrentDictionary<string, Delegate> _propertyAccessorCache = new();

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

        var orderedQuery = query;
        var isFirstSort = true;

        foreach (var sort in sorts)
        {
            orderedQuery = ApplySort(orderedQuery, sort.property, sort.descending, !isFirstSort);
            isFirstSort = false;
        }

        return orderedQuery;
    }

    private IQueryable<T> ApplySort(IQueryable<T> query, string propertyName, bool descending, bool isThenBy = false)
    {
        var entityType = typeof(T);
        var propertyInfo = entityType.GetProperty(propertyName,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo == null)
        {
            _logger.LogWarning("Property {PropertyName} not found on type {TypeName}", propertyName, entityType.Name);
            return query;
        }

        // Create the property access lambda
        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, propertyInfo);
        var propertyType = propertyInfo.PropertyType;
        var delegateType = typeof(Func<,>).MakeGenericType(entityType, propertyType);
        var lambda = Expression.Lambda(delegateType, propertyAccess, parameter);

        // Determine the method name
        string methodName;
        if (isThenBy)
        {
            methodName = descending ? "ThenByDescending" : "ThenBy";
        }
        else
        {
            methodName = descending ? "OrderByDescending" : "OrderBy";
        }

        // Get the MethodInfo for the appropriate OrderBy/ThenBy method
        var method = typeof(Queryable).GetMethods()
            .Where(m => m.Name == methodName && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(entityType, propertyType);

        // Invoke the method
        return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda });
    }
}