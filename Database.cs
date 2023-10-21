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
        private readonly DbContext _dbContext;

        public Database(DbContext context)
        {
            _dbContext = context;
        }
  
        public CascadeSoftDeleteConfiguration<ISoftDelete>? _softDeleteConfiguration;

        public TDbContext GetDbContext<TDbContext>() 
            where TDbContext : DbContext
        {
            return (TDbContext)_dbContext;
        }

        #region Handle the commands of requests

        public async Task CreateAsync<TRequest, TEntity, TReturnedType>(
            TRequest request, TEntity entity)
            where TRequest : RequestToCreate<TEntity, TReturnedType>
            where TEntity : BaseEntity<TEntity>
        {
            var _dbSet = _dbContext.Set<TEntity>();

            if (entity.Uniqueness() != null && entity.Uniqueness()!.Condition != null)
            {
                await new LogicalState()
                    .AddAnPreventer( new PreventIfTheEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted<TEntity>(_dbContext, entity.Uniqueness()!.Condition!)
                    .WithDescription(entity.Uniqueness()!.Description!))
                    .AssesstAsync();
            }

            _dbSet.Add(entity);
        }

        public async Task UpdateAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToUpdate<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            if (entity.Uniqueness() != null && entity.Uniqueness()!.Condition != null)
            {
                var builder = new ExpressionBuilder<TEntity>();
                if (request.Identification() != null)
                    builder.AndNot(request.Identification()!);
                builder.And(entity.Uniqueness()!.Condition!);
                var expression = builder.GetExpression();
                if (expression != null)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfTheEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted<TEntity>(_dbContext, expression)
                        .WithDescription(entity.Uniqueness()!.Description!))
                        .AssesstAsync();
            }
        }

        public async Task ArchiveAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToArchive<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            var service = new CascadeSoftDelServiceAsync<ISoftDelete>(_softDeleteConfiguration);
            await service.SetCascadeSoftDeleteAsync(entity, callSaveChanges: false);
        }

        public async Task RestoreAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToRestore<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            if (entity.Uniqueness() != null && entity.Uniqueness()!.Condition != null)
            {
                var builder = new ExpressionBuilder<TEntity>();
                if (request.Identification() != null)
                    builder.AndNot(request.Identification()!);
                builder.And(entity.Uniqueness()!.Condition!);
                var expression = builder.GetExpression();
                if (expression != null)
                    await new LogicalState()
                    .AddAnPreventer(
                        new PreventIfTheEntityWithTheseUniquenessConditionsHasAlreadyBeenExisted<TEntity>(_dbContext, expression)
                        .WithDescription(entity.Uniqueness()!.Description!))
                    .AssesstAsync();
            }

            var service = new CascadeSoftDelServiceAsync<ISoftDelete>(_softDeleteConfiguration);
            await service.ResetCascadeSoftDeleteAsync(entity, callSaveChanges: false);
        }

        public async Task DeleteAsync<TRequest, TEntity>(
            TRequest request, TEntity entity)
            where TRequest : RequestToDelete<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
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
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : AnyRequest<TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            var query = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            if (filter != null)
                query = filter != null ? filter!(query) : query;
            var skippedQuery = RequestQueryHelper.SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            return await skippedQuery.AnyAsync();
        }

        public async Task<TEntity?> GetItemAsync<TRequest, TEntity>(
            TRequest request)
            where TRequest : QueryItemRequest<TEntity, TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            var query = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            query = RequestQueryHelper.SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return await query.SingleAsync();
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .AssesstAsync();

                return await Task.FromResult<TEntity?>(null);
            }
        }

        public async Task<TReturnedType?> GetItemAsync<TRequest, TEntity, TReturnedType>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TReturnedType>> selector)
            where TRequest : QueryItemRequest<TEntity, TReturnedType>
            where TEntity : BaseEntity<TEntity>
        {
            var query = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            query = RequestQueryHelper.SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return await selector(query).SingleAsync();
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .AssesstAsync();

                return await Task.FromResult<dynamic>(null!);
            }
        }

        public async Task<TReturnedType?> GetItemAsync<TRequest, TEntity, TReturnedType>(
            TRequest request,
            Converter<TEntity, TReturnedType> converter)
            where TRequest : QueryItemRequest<TEntity, TReturnedType>
            where TEntity : BaseEntity<TEntity>
        {
            var query = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            query = RequestQueryHelper.SkipQuery(query, pageNumber: request.PageNumber, pageSize: request.PageSize);

            try
            {
                return converter(await query.SingleAsync());
            }
            catch (InvalidOperationException)
            {
                if (request.PreventIfNoEntityWasFound)
                    await new LogicalState()
                        .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(query))
                        .AssesstAsync();

                return await Task.FromResult<dynamic>(null!);
            }
        }

        public async Task<List<TEntity>> GetListAsync<TRequest, TEntity>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, TEntity>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return await skippedQuery.ToListAsync();
        }

        public async Task<List<TReturnedType>> GetListAsync<TRequest, TEntity, TReturnedType>(
            TRequest request,
            Converter<TEntity, TReturnedType> converter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, List<TReturnedType>>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return (await skippedQuery.ToListAsync()).ConvertAll(converter!);
        }

        public async Task<List<TReturnedType>> GetListAsync<TRequest, TEntity, TReturnedType>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TReturnedType>> selector,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, List<TReturnedType>>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return await selector(skippedQuery).ToListAsync();
        }

        public async Task<PaginatedViewModel<TEntity>> GetPaginatedListAsync<
            TRequest, TEntity>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, PaginatedViewModel<TEntity>>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return new PaginatedViewModel<TEntity>(
                items: await skippedQuery.ToListAsync(),
                countOfAllItems: await filteredQuery.CountAsync(),
                pageNumber: request.PageNumber,
                pageSize: request.PageSize);
        }

        public async Task<PaginatedViewModel<TReturnedType>> GetPaginatedListAsync<
            TRequest, TEntity, TReturnedType>(
            TRequest request,
            Converter<TEntity, TReturnedType> converter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, PaginatedViewModel<TReturnedType>>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return new PaginatedViewModel<TReturnedType>(
                  items: (await skippedQuery.ToListAsync()).ConvertAll(converter!),
                  countOfAllItems: await filteredQuery.CountAsync(),
                  pageNumber: request.PageNumber,
                  pageSize: request.PageSize);
        }

        public async Task<PaginatedViewModel<TReturnedType>> GetPaginatedListAsync<
            TRequest, TEntity, TReturnedType>(
            TRequest request,
            Func<IQueryable<TEntity>, IQueryable<TReturnedType>> selector,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? filter = null)
            where TRequest : QueryListRequest<TEntity, PaginatedViewModel<TReturnedType>>
            where TEntity : BaseEntity<TEntity>
        {
            var baseQuery = RequestQueryHelper.MakeQuery<TRequest, TEntity>(_dbContext, request);
            var filteredQuery = filter != null ? filter!(baseQuery) : baseQuery;
            var skippedQuery = RequestQueryHelper.SkipQuery(filteredQuery, pageNumber: request.PageNumber, pageSize: request.PageSize);

            if (request.PreventIfNoEntityWasFound)
                await new LogicalState()
                    .AddAnPreventer(new PreventIfNoEntityWasFound<TEntity, TEntity>(skippedQuery))
                    .AssesstAsync();

            return new PaginatedViewModel<TReturnedType>(
                items: await selector(skippedQuery).ToListAsync(),
                countOfAllItems: await filteredQuery.CountAsync(),
                pageNumber: request.PageNumber,
                pageSize: request.PageSize);
        }

        #endregion
    }
}
