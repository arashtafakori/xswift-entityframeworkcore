using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EntityFrameworkCore.XSwift.Datastore
{
    public static class IQueryableExtensions
    {
        public static async Task<bool> AnyAsync<TSource>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, bool>>? condition,
            bool evenArchivedData = false,
            int? offset = null,
            int? limit = null)
            where TSource : class
        {
            return await MakeQuery(
                query: query,
                condition: condition,
                evenArchivedData: evenArchivedData)
                .SkipQuery(offset: offset, limit: limit)
                .AnyAsync();
        }

        public static IQueryable<TSource> MakeQuery<TSource>(
            this IQueryable<TSource> query,
            Expression<Func<TSource, bool>>? condition = null,
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

            if (condition != null)
                query = query.Where(condition);

            if (orderBy != null)
                query = query.OrderByDescending(orderBy);

            if (orderByDescending != null)
                query = query.OrderByDescending(orderByDescending);

            if (include != null)
                query = query.Include(include);

            return query;
        }
        public static IQueryable<TSource> SkipQuery<TSource>(
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
