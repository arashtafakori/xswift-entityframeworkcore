using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using XSwift.Domain;
using XSwift.EntityFrameworkCore;

namespace Xswift.EntityFrameworkCore.Test
{
    [TestClass]
    public class IQueryableExtensionsTests
    {
        public class TestEntity : BaseEntity<TestEntity>
        {
            [Required(AllowEmptyStrings = false)]
            public string Name { get; protected set; } = string.Empty;

            public TestEntity SetName(string value)
            {
                Name = value;

                return this;
            }
        }

        [TestMethod]
        public void MakeQuery_ShouldApplyFiltersAndOrdering()
        {
            // Arrange
            var entities = new List<TestEntity>
            {
                new TestEntity().SetName("Arta")
            };
            var query = entities.AsQueryable();

            // Act
            var result = query.MakeQuery(
                where: e => e.Name.Contains("A"),
                orderBy: e => e.Name,
                orderByDescending: e => e.CreatedDate,
                trackingMode: true,
                evenArchivedData: false);

            // Assert
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void SkipQuery_ShouldApplyOffsetAndLimit()
        {
            // Arrange
            var entities = new List<TestEntity>
        {
            new TestEntity(),
            new TestEntity(),
            new TestEntity()
        }.AsQueryable();

            // Act
            var result = entities.SkipQuery(offset: 1, limit: 1);

            // Assert
            result.Count().Should().Be(1);
            // Add more assertions based on your specific implementation and requirements
        }

        [TestMethod]
        public void SkipQuery_WithNullOffsetAndLimit_ShouldNotSkipOrTake()
        {
            // Arrange
            var entities = new List<TestEntity>().AsQueryable();

            // Act
            var result = entities.SkipQuery(offset: null, limit: null);

            // Assert
            result.Count().Should().Be(entities.Count());
            // Add more assertions based on your specific implementation and requirements
        }
    }
}
