namespace LifeInsuranceCRM.Tests.Utilities;

public abstract class UseCaseTestBase<TSubject> where TSubject : class
{
    protected TSubject SubjectUnderTest { get; private set; } = null!;

    protected virtual Task InitializeAsync() => Task.CompletedTask;

    protected void SetSubject(TSubject subject) => SubjectUnderTest = subject;
}
