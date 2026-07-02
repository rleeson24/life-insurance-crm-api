namespace LifeInsuranceCRM.Core.Tests;

public class FoundationTests
{
    [Fact]
    public void Utilities_ProcessResponse_Succeeded_IsSuccess()
    {
        var response = Utilities.ProcessResponse<string>.Succeeded("ok");
        Assert.True(response.IsSuccess);
        Assert.Equal("ok", response.Result);
    }
}
