using XSwift.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace XSwift.EntityFrameworkCore
{
    /// <summary>
    /// Provides a base class for database transactions using Entity Framework Core.
    /// </summary>
    public abstract class DbTransaction : IDbTransaction
    {
        private readonly DbContext _context;

        /// <summary>
        /// Initializes a new instance of the DbTransaction class.
        /// </summary>
        /// <param name="context">The DbContext associated with the transaction.</param>
        public DbTransaction(DbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<IDbContextTransaction> BeginAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        /// <inheritdoc/>
        public async Task CommitAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        /// <inheritdoc/>
        public async Task RollbackAsync()
        {
            await _context.Database.RollbackTransactionAsync();
        }

        /// <inheritdoc/>
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
