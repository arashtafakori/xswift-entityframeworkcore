using XSwift.Base;
using XSwift.Domain;
using Microsoft.EntityFrameworkCore;

namespace XSwift.EntityFrameworkCore.Datastore
{
    /// <summary>
    /// Represents a logical preventer that checks if no entity was found in a given query.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity derived from BaseEntity.</typeparam>
    /// <typeparam name="TModel">The type of the model associated with the database query.</typeparam>
    public class PreventIfNoEntityWasFound<TEntity, TModel>
        : LogicalPreventer
        where TEntity : BaseEntity<TEntity>
        where TModel : class
    {
        private IQueryable<TModel> _query;

        /// <summary>
        /// Initializes a new instance of the PreventIfNoEntityWasFound class.
        /// </summary>
        /// <param name="query">The IQueryable representing the query to check for the existence of entities.</param>
        public PreventIfNoEntityWasFound(IQueryable<TModel> query)
        {
            _query = query;
        }

        /// <summary>
        /// Resolves the logical condition by checking if any entity is found in the associated query.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation with a result indicating whether any entity was found.</returns>
        public override async Task<bool> ResolveAsync()
        {
            return !await _query.AnyAsync();
        }

        /// <summary>
        /// Gets the issue related to the logical preventer, indicating that no entity was found.
        /// </summary>
        /// <returns>The issue is representing the condition of no entity being found.</returns>
        public override IIssue? GetIssue()
        {
            return new NoEntityWasFound(typeof(TEntity).Name, Description);
        }
    }
}
