using AutoFixture;
using Moq;

namespace LifeInsuranceCRM.Tests.Utilities;

public abstract class UseCaseTestBase<TSubject> where TSubject : class
{
    private readonly Dictionary<Type, object> _mocks = new();

    protected Fixture TestFixture { get; }

    protected TSubject SubjectUnderTest { get; private set; } = null!;

    protected object? Result { get; set; }

    protected Exception? ThrownException { get; set; }

    protected UseCaseTestBase()
    {
        TestFixture = new Fixture();
        TestFixture.ConfigureCrmEntities();
    }

    protected T Create<T>() => TestFixture.Create<T>();

    protected Guid CreateGuid() => TestFixture.Create<Guid>();

    protected DateTimeOffset CreateTimestamp() => TestFixture.Create<DateTimeOffset>();

    protected Mock<T> MockFor<T>() where T : class
    {
        if (!_mocks.TryGetValue(typeof(T), out var mock))
        {
            mock = new Mock<T>();
            _mocks[typeof(T)] = mock;
        }

        return (Mock<T>)mock;
    }

    protected abstract TSubject BuildSubject();

    protected void SetSubject(TSubject subject) => SubjectUnderTest = subject;

    protected async Task ExecuteOnceAsync(Func<TSubject, Task> execute)
    {
        SubjectUnderTest = BuildSubject();
        try
        {
            await execute(SubjectUnderTest);
        }
        catch (Exception ex)
        {
            ThrownException = ex;
        }
    }

    protected async Task ExecuteOnceAsync(Func<TSubject, Task<object?>> execute)
    {
        SubjectUnderTest = BuildSubject();
        try
        {
            Result = await execute(SubjectUnderTest);
        }
        catch (Exception ex)
        {
            ThrownException = ex;
        }
    }
}
