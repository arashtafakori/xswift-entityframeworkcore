using XSwift.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.XSwift
{
    public abstract class DbTransaction : IDbTransaction
    {
        private readonly DbContext _context;

        public DbTransaction(DbContext context)
        {
            _context = context;
        }

        public async Task<IDbContextTransaction> BeginAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        public async Task<int> SaveChangesAsync(
            bool concurrencyCheck = false,
            DbUpdateConcurrencyConflictOccurred? toCheckConcurrencyConflictOccurred = null)
        {
            if(concurrencyCheck == false)
            {
                return await _context.SaveChangesAsync();
            }else{
                try
                {
                    return await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (toCheckConcurrencyConflictOccurred != null)
                        toCheckConcurrencyConflictOccurred();
                    else
                        throw;
                }

                return -1;
            }
        }
    }
}
