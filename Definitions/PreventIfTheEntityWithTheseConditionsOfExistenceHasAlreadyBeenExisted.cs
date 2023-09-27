using XSwift.Base;
using XSwift.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EntityFrameworkCore.XSwift.Datastore
{
    public class PreventIfTheEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted<TEntity>
        : LogicalPreventer
        where TEntity : BaseEntity<TEntity>
    {
        private DbContext _context;
        private Expression<Func<TEntity, bool>> _condition;
 
        public PreventIfTheEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted(
            DbContext context, Expression<Func<TEntity, bool>> condition)
        {
            _context = context;
            _condition = condition;
        }

        public override async Task<bool> ResolveAsync()
        {
            return await _context.Set<TEntity>().AsQueryable().AnyAsync(
                condition: _condition);
        }

        public override IIssue? GetIssue()
        {
            return new AnEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted(
                    typeof(TEntity).Name);
        }
    }
}
