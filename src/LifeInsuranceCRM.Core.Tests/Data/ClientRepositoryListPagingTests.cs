using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Security;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Data.Repositories;
using Microsoft.Data.SqlClient;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.Data;

public class ClientRepositoryListPagingTests
{
    private readonly Mock<IDbExecutor> _db = new();
    private readonly Mock<IFieldEncryptionService> _encryption = new();
    private readonly ClientRepository _repository;

    public ClientRepositoryListPagingTests()
    {
        _db.Setup(e => e.ExecuteScalarAsync<int>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SqlParameter[]>()))
            .ReturnsAsync(0);
        _db.Setup(e => e.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Microsoft.Data.SqlClient.SqlDataReader, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<SqlParameter[]>()))
            .Returns(Task.CompletedTask);
        _repository = new ClientRepository(_db.Object, _encryption.Object);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(1, 100, 1, 100)]
    [InlineData(1, 101, 1, 100)]
    [InlineData(-1, int.MinValue, 1, 1)]
    [InlineData(int.MinValue, int.MaxValue, 1, 100)]
    public async Task ListAsync_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _repository.ListAsync(new ListClientsRequest { Page = page, PageSize = pageSize });

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
        var expectedOffset = (expectedPage - 1) * expectedPageSize;
        _db.Verify(
            e => e.ExecuteReaderAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Microsoft.Data.SqlClient.SqlDataReader, CancellationToken, Task>>(),
                It.IsAny<CancellationToken>(),
                It.Is<SqlParameter[]>(p =>
                    ParameterValue<int>(p, "@Offset") == expectedOffset
                    && ParameterValue<int>(p, "@PageSize") == expectedPageSize)),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListAsync_WhenSearchNullOrEmpty_PassesNullSearch(string? search)
    {
        await _repository.ListAsync(new ListClientsRequest { Search = search, Page = 1, PageSize = 25 });

        _encryption.Verify(e => e.ComputeMedicareNumberBlindIndex(It.IsAny<string>()), Times.Never);
        _db.Verify(
            e => e.ExecuteScalarAsync<int>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.Is<SqlParameter[]>(p => ParameterValue<object>(p, "@Search") == DBNull.Value)),
            Times.Once);
    }

    [Fact]
    public async Task ListAsync_WhenSearchHasUnicodeAndSpecialCharacters_PassesLikePattern()
    {
        await _repository.ListAsync(new ListClientsRequest { Search = "  O'Brien 李 %_  ", Page = 1, PageSize = 25 });

        _db.Verify(
            e => e.ExecuteScalarAsync<int>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.Is<SqlParameter[]>(p => Equals(ParameterValue<object>(p, "@Search"), "%O'Brien 李 %_%"))),
            Times.Once);
    }

    [Fact]
    public async Task ListAsync_WhenCanceled_PassesToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await _repository.ListAsync(new ListClientsRequest { Page = 1, PageSize = 25 }, cts.Token);

        _db.Verify(
            e => e.ExecuteScalarAsync<int>(
                It.IsAny<string>(),
                cts.Token,
                It.IsAny<SqlParameter[]>()),
            Times.Once);
    }

    private static T ParameterValue<T>(SqlParameter[] parameters, string name)
    {
        var parameter = Assert.Single(parameters, p => p.ParameterName == name);
        return (T)parameter.Value!;
    }
}
