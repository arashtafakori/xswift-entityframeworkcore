using XSwift.Domain;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.XSwift.Datastore
{
    public class RequestQueryHelper
    {
        public static IQueryable<TEntity> MakeQuery<TRequest, TEntity>(
            DbContext context,
            TRequest request)
            where TRequest : QueryRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            return context.Set<TEntity>().AsQueryable().MakeQuery(
                where: request.Where().GetExpression(),
                orderBy: request.OrderBy(),
                orderByDescending: request.OrderByDescending(),
                include: request.Include(),
                trackingMode: request.TrackingMode,
                evenArchivedData: request.EvenArchivedData);
        }

        public static IQueryable<TEntity> SkipQuery<TEntity>(
            IQueryable<TEntity> query,
            int? pageNumber, int? pageSize)
            where TEntity : BaseEntity<TEntity>
        {
            if (pageNumber != null && pageNumber < 1)
                throw new ArgumentException("The page number must be 1 or higher.");

            if (pageSize != null && pageSize < 1)
                throw new ArgumentException("The page size must be 1 or higher.");

            return query.SkipQuery(
                offset: pageNumber - 1,
                limit: pageSize);
        }
    }
}
