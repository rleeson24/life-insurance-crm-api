using LifeInsuranceCRM.Core.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace LifeInsuranceCRM.API.Auth;

public sealed class ActorTracker : IActorTracker
{
    private Guid? _userId;
    private string? _userEmail;
    private Guid? _tenantId;
    private string? _role;

    public Guid? UserId => _userId;

    public string? UserEmail => _userEmail;

    public Guid? TenantId => _tenantId;

    public string? Role => _role;

    public bool IsAuthenticated => _userId.HasValue;

    public void SetActor(Guid userId, string? userEmail, Guid tenantId, string role)
    {
        _userId = userId;
        _userEmail = userEmail;
        _tenantId = tenantId;
        _role = role;
    }

    public void Clear()
    {
        _userId = null;
        _userEmail = null;
        _tenantId = null;
        _role = null;
    }
}
