using XSwift.Domain;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Configuration;
using System.Linq.Expressions;
using System.Reflection;

namespace XSwift.EntityFrameworkCore
{
    /// <summary>
    /// Provides extension methods for configuring soft delete functionality in Entity Framework Core.
    /// </summary>
    public static class SoftDeleteExtensions
    {
        /// <summary>
        /// Configures soft delete functionality for each given entity type.
        /// </summary>
        /// <param name="config">The CascadeSoftDeleteConfiguration instance.</param>
        public static void ConfigureSoftDelete(
            this CascadeSoftDeleteConfiguration<ISoftDelete> config)
        {
            config.GetSoftDeleteValue = entity => entity.Deleted;
            config.SetSoftDeleteValue = (entity, value) =>
            {
                entity.Deleted = value;
            };
        }

        /// <summary>
        /// Resolves the soft delete configuration for the database.
        /// </summary>
        /// <param name="database">The Database instance.</param>
        /// <param name="softDeleteConfiguration">The CascadeSoftDeleteConfiguration instance.</param>
        public static void ResolveSoftDeleteConfiguration(
            this Database database,
            CascadeSoftDeleteConfiguration<ISoftDelete> softDeleteConfiguration)
        {
            database._softDeleteConfiguration ??= softDeleteConfiguration;
        }

        /// <summary>
        /// Adds soft delete capability for queries in the model.
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder instance.</param>
        public static void AddSoftDeleteCapabilityForQuery(
            this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var methodToCall = typeof(SoftDeleteExtensions)
                    .GetMethod(nameof(GetSoftDeleteFilter),
                     BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                    var filter = methodToCall.Invoke(null, new object[] { });
                    entityType.SetQueryFilter((LambdaExpression)filter!);
                    entityType.AddIndex(entityType.
                         FindProperty(nameof(ISoftDelete.Deleted))!);
                }
            }
        }

        /// <summary>
        /// Gets the soft delete filter expression for a specific entity type.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity implementing ISoftDelete.</typeparam>
        /// <returns>The soft delete filter expression.</returns>
        private static LambdaExpression GetSoftDeleteFilter<TEntity>()
            where TEntity : BaseEntity<TEntity>, ISoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => x.Deleted == 0;
            return filter;
        }
    }
}
