using XSwift.Domain;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Configuration;
using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkCore.XSwift
{
    public static class SoftDeleteExtensions
    {
        public static void ConfigureSoftDelete(
            this CascadeSoftDeleteConfiguration<ISoftDelete> config)
        {
            config.GetSoftDeleteValue = entity => entity.Deleted;
            config.SetSoftDeleteValue = (entity, value) =>
            {
                entity.Deleted = value;
            };
        }
        public static void ResolveSoftDeleteConfiguration(
            this Database database,
            CascadeSoftDeleteConfiguration<ISoftDelete> softDeleteConfiguration)
        {
            database._softDeleteConfiguration ??= softDeleteConfiguration;
        }
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

        private static LambdaExpression GetSoftDeleteFilter<TEntity>()
            where TEntity : BaseEntity<TEntity>, ISoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => x.Deleted == 0;
            return filter;
        }
    }
}
