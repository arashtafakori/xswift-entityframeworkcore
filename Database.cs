using XSwift.Base;
using XSwift.Datastore;
using XSwift.Domain;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete;
using SoftDeleteServices.Configuration;

namespace EntityFrameworkCore.XSwift.Datastore
{
    public abstract class Database : IDatabase
    {
        public Database(DbContext context)
        {
            _dbContext = context;
        }

        private readonly DbContext _dbContext;
  
        public CascadeSoftDeleteConfiguration<ISoftDelete>? _softDeleteConfiguration;

        public TDbContext GetDbContext<TDbContext>() 
            where TDbContext : DbContext
        {
            return (TDbContext)_dbContext;
        }

        #region Handle the commands of requests

        public async Task CreateAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToCreate<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var _dbSet = _dbContext.Set<TEntity>();

            if (entity.Uniqueness() != null)
            {
                await new LogicalState()
                    .AddAnPreventer( new PreventIfTheEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted<TEntity>(
                        _dbContext, entity.Uniqueness()!.Condition)
                    .WithDescription(entity.Uniqueness()!.Description!))
                    .CheckAsync();
            }

            _dbSet.Add(entity);
        }

        public async Task UpdateAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToUpdate<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            if (entity.Uniqueness() != null)
            {
                var builder = new ExpressionBuilder<TEntity>();
                builder.And(builder.Invert(request.Identification()!));
                builder.And(entity.Uniqueness()!.Condition!);
                var expression = builder.GetExpression();
                if (expression != null)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfTheEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted
                        <TEntity>(_dbContext, expression)
                        .WithDescription(entity.Uniqueness()!.Description!))
                        .CheckAsync();
            }
        }

        public async Task ArchiveAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToArchive<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var service = new CascadeSoftDelServiceAsync<ISoftDelete>(_softDeleteConfiguration);
            await service.SetCascadeSoftDeleteAsync(entity, callSaveChanges: false);
        }

        public async Task RestoreAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToRestore<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            if (entity.Uniqueness() != null)
            {
                var builder = new ExpressionBuilder<TEntity>();
                builder.And(builder.Invert(request.Identification()!));
                builder.And(entity.Uniqueness()!.Condition!);
                var expression = builder.GetExpression();
                if (expression != null)
                    await new LogicalState()
                    .AddAnPreventer(
                        new PreventIfTheEntityWithTheseConditionsOfExistenceHasAlreadyBeenExisted
                        <TEntity>(_dbContext, expression)
                        .WithDescription(entity.Uniqueness()!.Description!))
                    .CheckAsync();
            }

            var service = new CascadeSoftDelServiceAsync<ISoftDelete>(_softDeleteConfiguration);
            await service.ResetCascadeSoftDeleteAsync(entity, callSaveChanges: false);
        }

        public async Task DeleteAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToDelete<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            await CanDelete(entity);

            var _dbSet = _dbContext.Set<TEntity>();
            _dbSet.Attach(entity);
            _dbSet.Remove(entity);
        }

        private async Task<bool> CanDelete<TEntity>(TEntity entity)
            where TEntity : BaseEntity<TEntity>
        {
            var service = new CascadeSoftDelServiceAsync<ISoftDelete>(_softDeleteConfiguration);
            return (await service.CheckCascadeSoftDeleteAsync(entity)).IsValid;
        }

        #endregion

        #region Handle the query based requests

        public async Task<bool> AnyAsync<TRequest, TEntity>(
            TRequest request)
            where TRequest : AnyRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            return await _dbContext.Set<TEntity>().AsQueryable().AnyAsync(
                condition: request.Identification(),
                evenArchivedData: request.EvenArchivedData,
                offset: request.PageNumber,
                limit: request.PageSize);
        }

        public async Task<TEntity?> GetItemAsync<TRequest, TEntity>(
            TRequest request)
            where TRequest : QueryItemRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var query = MakeQuery<TRequest, TEntity>(request);
            query = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return await query.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .CheckAsync();

                return await Task.FromResult<TEntity?>(null);
            }
        }

        public async Task<TModel?> GetItemAsync<TRequest, TEntity, TModel>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TModel>> selector)
            where TRequest : QueryItemRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var query = MakeQuery<TRequest, TEntity>(request);
            query = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return await selector(query).SingleAsync();
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .CheckAsync();

                return await Task.FromResult<dynamic>(null!);
            }
        }

        public async Task<TModel?> GetItemAsync<TRequest, TEntity, TModel>(
            TRequest request,
            Converter<TEntity, TModel> converter)
            where TRequest : QueryItemRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var query = MakeQuery<TRequest, TEntity>(request);
            query = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return converter(await query.SingleAsync());
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .CheckAsync();

                return await Task.FromResult<dynamic>(null!);
            }
        }

        public async Task<List<TEntity>> GetListAsync<
            TRequest, TEntity>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return await query.ToListAsync();
        }

        public async Task<List<TModel>> GetListAsync<
            TRequest, TEntity, TModel>(
            TRequest request,
            Converter<TEntity, TModel> converter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return (await query.ToListAsync()).ConvertAll(converter!);
        }

        public async Task<List<TModel>> GetListAsync<
            TRequest, TEntity, TModel>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TModel>> selector,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return await selector(skippedQuery).ToListAsync();
        }

        public async Task<PaginatedViewModel<TEntity>> GetPaginatedListAsync<
            TRequest, TEntity>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return new PaginatedViewModel<TEntity>(
                items: await query.ToListAsync(),
                countOfAllItems: await query.CountAsync(),
                pageNumber: request.PageNumber,
                pageSize: request.PageSize);
        }

        public async Task<PaginatedViewModel<TModel>> GetPaginatedListAsync<
            TRequest, TEntity, TModel>(
            TRequest request,
            Converter<TEntity, TModel> converter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return new PaginatedViewModel<TModel>(
                  items: (await query.ToListAsync()).ConvertAll(converter!),
                  countOfAllItems: await query.CountAsync(),
                  pageNumber: request.PageNumber,
                  pageSize: request.PageSize);
        }

        public async Task<PaginatedViewModel<TModel>> GetPaginatedListAsync<
            TRequest, TEntity, TModel>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TModel>> selector,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            await CheckInvariantsAsync<TRequest, TEntity>(request);

            var baseQuery = MakeQuery<TRequest, TEntity>(request);
            var query = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .CheckAsync();

            return new PaginatedViewModel<TModel>(
                items: await selector(skippedQuery).ToListAsync(),
                countOfAllItems: await query.CountAsync(),
                pageNumber: request.PageNumber,
                pageSize: request.PageSize);
        }

        #endregion

        #region Query Operations
        public IQueryable<TSource> MakeQuery<TRequest, TSource>(
            TRequest request)
            where TRequest : QueryRequest<TSource>
            where TSource : class
        {
            return _dbContext.Set<TSource>().AsQueryable().MakeQuery(
                condition: request.Identification(),
                orderBy: request.OrderBy(),
                orderByDescending: request.OrderByDescending(),
                include: request.Include(),
                trackingMode: request.TrackingMode,
                evenArchivedData: request.EvenArchivedData);
        }

        public IQueryable<TSource> SkipQuery<TSource>(
            IQueryable<TSource> query,
            int? pageNumber, int? pageSize)
            where TSource : class
        {
            if (pageNumber != null && pageNumber < 1)
                throw new ArgumentException("The page number must be 1 or higher.");

            if (pageSize != null && pageSize < 1)
                throw new ArgumentException("The page size must be 1 or higher.");

            return query.SkipQuery(
                offset: pageNumber - 1,
                limit: pageSize);
        }
        #endregion

        #region Handle the invariants of requests
        public async Task CheckInvariantsAsync<TRequest, TEntity>(
            TRequest request)
            where TRequest : ModelBasedRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            var query = _dbContext.Set<TEntity>().AsQueryable();
            foreach (var invariant in request.GetInvariants())
            {
                var result = await query.AnyAsync(condition: invariant.Condition);
     
                request.InvariantState.DefineAnInvariant(result, invariant.Issue);
            }
        }
        #endregion
    }
}
