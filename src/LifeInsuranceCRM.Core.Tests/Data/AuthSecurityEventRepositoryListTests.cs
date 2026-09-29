using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Data.Repositories;
using Microsoft.Data.SqlClient;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.Data;

public class AuthSecurityEventRepositoryListTests
{
    private readonly Mock<IDbExecutor> _db = new();
    private readonly AuthSecurityEventRepository _repository;

    public AuthSecurityEventRepositoryListTests()
    {
        _db.Setup(e => e.BypassTenantFilter()).Returns(Mock.Of<IDisposable>());
        _db.Setup(e => e.ExecuteScalarAsync<int>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SqlParameter[]>()))
            .ReturnsAsync(0);
        _db.Setup(e => e.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<Func<SqlDataReader, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SqlParameter[]>()))
            .Returns(Task.CompletedTask);
        _repository = new AuthSecurityEventRepository(_db.Object);
    }

    [Fact]
    public async Task ListAsync_BypassesTenantFilter()
    {
        await _repository.ListAsync(new ListAuthSecurityEventsRequest { Page = 1, PageSize = 50 });

        _db.Verify(e => e.BypassTenantFilter(), Times.Once);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(2, 50, 2, 50)]
    [InlineData(1, 101, 1, 100)]
    public async Task ListAsync_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _repository.ListAsync(new ListAuthSecurityEventsRequest
        {
            Page = page,
            PageSize = pageSize,
        });

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
        var expectedOffset = (expectedPage - 1) * expectedPageSize;
        _db.Verify(
            e => e.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<Func<SqlDataReader, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>(),
                It.Is<SqlParameter[]>(p =>
                    ParameterValue<int>(p, "@Offset") == expectedOffset
                    && ParameterValue<int>(p, "@PageSize") == expectedPageSize)),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_BypassesTenantFilter()
    {
        await _repository.GetByIdAsync(Guid.NewGuid());

        _db.Verify(e => e.BypassTenantFilter(), Times.Once);
    }

    private static T ParameterValue<T>(SqlParameter[] parameters, string name)
    {
        var parameter = Assert.Single(parameters, p => p.ParameterName == name);
        return (T)parameter.Value!;
    }
}
