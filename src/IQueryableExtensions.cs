using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace XSwift.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for IQueryable to facilitate query construction and pagination.
    /// </summary>
    internal static class IQueryableExtensions
    {
        /// <summary>
        /// Constructs a query based on specified parameters such as filtering, ordering, and inclusion.
        /// </summary>
        /// <typeparam name="TSource">The type of the entity in the query.</typeparam>
        /// <param name="query">The IQueryable to be modified.</param>
        /// <param name="where">The filter expression for the query.</param>
        /// <param name="orderBy">The ordering expression for ascending order.</param>
        /// <param name="orderByDescending">The ordering expression for descending order.</param>
        /// <param name="include">The include expression for related entities.</param>
        /// <param name="trackingMode">Indicates whether tracking is enabled or disabled.</param>
        /// <param name="evenArchivedData">Indicates whether archived data should be included.</param>
        /// <returns>An IQueryable representing the modified query based on the specified parameters.</returns>
        internal static IQueryable<TSource> MakeQuery<TSource>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, bool>>? where = null,
            Expression<Func<TSource, object>>? orderBy = null,
            Expression<Func<TSource, object>>? orderByDescending = null,
            Expression<Func<TSource, object>>? include = null,
            bool? trackingMode = false,
            bool? evenArchivedData = false) 
            where TSource : class
        {
            if ((bool)!trackingMode!)
                query = query.AsNoTracking();

            if ((bool)evenArchivedData!)
                query = query.IgnoreQueryFilters();

            if (where != null)
                query = query.Where(where);

            if (orderBy != null)
                query = query.OrderBy(orderBy);

            if (orderByDescending != null)
                query = query.OrderByDescending(orderByDescending);

            if (include != null)
                query = query.Include(include);

            return query;
        }

        /// <summary>
        /// Skips a specified number of elements in a query and limits the result set.
        /// </summary>
        /// <typeparam name="TSource">The type of the entity in the query.</typeparam>
        /// <param name="query">The IQueryable to be modified.</param>
        /// <param name="offset">The number of items to skip (offset for pagination).</param>
        /// <param name="limit">The number of items to be included in the result set (limit for pagination).</param>
        /// <returns>An IQueryable representing the modified query with skip and limit applied.</returns>
        internal static IQueryable<TSource> SkipQuery<TSource>(
            this IQueryable<TSource> query,
            int? offset = null,
            int? limit = null)
            where TSource : class
        {
            if (offset != null)
            {
                query = query.Skip(((int)offset) * (limit ?? 0));
                if (limit != null)
                    query = query.Take((int)limit);
            }

            return query;
        }
    }
}
