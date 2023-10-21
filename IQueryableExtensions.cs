using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EntityFrameworkCore.XSwift.Datastore
{
    internal static class IQueryableExtensions
    {
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
