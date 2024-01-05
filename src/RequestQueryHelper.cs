using XSwift.Domain;
using Microsoft.EntityFrameworkCore;

namespace XSwift.EntityFrameworkCore
{
    /// <summary>
    /// Helper class for building and manipulating queries based on request parameters.
    /// </summary>
    public class RequestQueryHelper
    {
        /// <summary>
        /// Constructs and executes a query based on the provided request parameters.
        /// </summary>
        /// <typeparam name="TRequest">The type of the query request.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="context">The DbContext instance.</param>
        /// <param name="request">The query request object containing filtering, ordering, and other parameters.</param>
        /// <returns>An IQueryable representing the constructed and executed query.</returns>
        /// <remarks>
        /// The query is constructed using the provided request parameters such as Where, OrderBy, Include, etc.
        /// </remarks>
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

        /// <summary>
        /// Skips a specified number of elements in a query and limits the result set.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The original query to be modified.</param>
        /// <param name="pageNumber">The page number (1-indexed) for pagination.</param>
        /// <param name="pageSize">The number of items to be included in each page.</param>
        /// <returns>An IQueryable representing the modified query with skip and limit applied.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the page number or page size is less than 1.
        /// </exception>
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
