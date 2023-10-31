using XSwift.Base;
using XSwift.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EntityFrameworkCore.XSwift.Datastore
{
    public class PreventIfTheEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted<TEntity>
        : LogicalPreventer
        where TEntity : BaseEntity<TEntity>
    {
        private DbContext _context;
        private Expression<Func<TEntity, bool>> _condition;
 
        public PreventIfTheEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted(
            DbContext context, Expression<Func<TEntity, bool>> condition)
        {
            _context = context;
            _condition = condition;
        }

        public override async Task<bool> ResolveAsync()
        {
            return await _context.Set<TEntity>()
                .AsQueryable()
                .Where(_condition)
                .AnyAsync();
        }

        public override IIssue? GetIssue()
        {
            return new AnEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted(
                    typeof(TEntity).Name, Description);
        }
    }
}
