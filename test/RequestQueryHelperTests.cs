using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using XSwift.Domain;
using XSwift.EntityFrameworkCore;

namespace XSwift.Test.EntityFrameworkCore
{
    [TestClass]
    public class RequestQueryHelperTests
    {
        public class TestEntity : BaseEntity<TestEntity>
        {
        }

        public class TestQueryRequest : QueryRequest<TestEntity>
        {
        }

        [TestMethod]
        public void MakeQuery_ShouldReturnQueryableWithCorrectFiltersAndOrdering()
        {
            // Arrange
            var dbContextMock = new Mock<DbContext>();
            var request = new TestQueryRequest();
            var entities = new List<TestEntity>().AsQueryable();
            var dbSetMock = new Mock<DbSet<TestEntity>>();

            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Provider).Returns(entities.Provider);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.Expression).Returns(entities.Expression);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.ElementType).Returns(entities.ElementType);
            dbSetMock.As<IQueryable<TestEntity>>().Setup(m => m.GetEnumerator()).Returns(entities.GetEnumerator());

            dbContextMock.Setup(x => x.Set<TestEntity>()).Returns(dbSetMock.Object);

            // Act
            var result = RequestQueryHelper.MakeQuery<TestQueryRequest, TestEntity>(dbContextMock.Object, request);

            // Assert
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void SkipQuery_ShouldSkipCorrectNumberOfItems()
        {
            // Arrange
            var entities = new List<TestEntity>
        {
            new TestEntity(),
            new TestEntity(),
            new TestEntity()
        }.AsQueryable();

            // Act
            var result = RequestQueryHelper.SkipQuery(entities, 2, 1);

            // Assert
            result.Count().Should().Be(1);
        }

        [TestMethod]
        public void SkipQuery_WithInvalidPageNumber_ShouldThrowArgumentException()
        {
            // Arrange
            var entities = new List<TestEntity>().AsQueryable();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => RequestQueryHelper.SkipQuery(entities, 0, 1));
        }

        [TestMethod]
        public void SkipQuery_WithInvalidPageSize_ShouldThrowArgumentException()
        {
            // Arrange
            var entities = new List<TestEntity>().AsQueryable();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => RequestQueryHelper.SkipQuery(entities, 1, 0));
        }
    }


}
