using AutoFixture;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Models;
using LifeInsuranceCRM.Core.Models.Input;
using Moq;
using LifeInsuranceCRM.Core.Abstractions.Auth;

namespace LifeInsuranceCRM.Tests.Utilities;

public static class FixtureExtensions
{
    public static void ConfigureCrmEntities(this Fixture fixture)
    {
        fixture.Customize<DateOnly>(composer => composer.FromFactory(() => new DateOnly(1950, 6, 15)));

        fixture.Customize<Client>(composer => composer
            .With(c => c.IsActive, true)
            .With(c => c.IsAcaClient, false)
            .With(c => c.HasContactConsent, false));

        fixture.Customize<CreateClientModel>(composer => composer
            .With(m => m.IsActive, true)
            .With(m => m.IsAcaClient, false)
            .With(m => m.HasContactConsent, false));
    }

    public static CreateClientModel CreateValidCreateClientModel(this Fixture fixture) =>
        fixture.Build<CreateClientModel>()
            .With(m => m.FirstName, fixture.Create<string>())
            .With(m => m.LastName, fixture.Create<string>())
            .Create();

    public static Client CreateClient(
        this Fixture fixture,
        Guid clientId,
        Guid tenantId,
        Guid userId,
        DateTimeOffset timestamp,
        string? firstName = null,
        string? lastName = null) =>
        fixture.Build<Client>()
            .With(c => c.ClientId, clientId)
            .With(c => c.TenantId, tenantId)
            .With(c => c.FirstName, firstName ?? fixture.Create<string>())
            .With(c => c.LastName, lastName ?? fixture.Create<string>())
            .With(c => c.CreatedAt, timestamp)
            .With(c => c.UpdatedAt, timestamp)
            .With(c => c.CreatedByUserId, userId)
            .With(c => c.UpdatedByUserId, userId)
            .Create();

    public static ClientInteraction CreateClientInteraction(
        this Fixture fixture,
        Guid interactionId,
        Guid clientId,
        Guid tenantId,
        Guid userId,
        DateTimeOffset timestamp) =>
        fixture.Build<ClientInteraction>()
            .With(i => i.ClientInteractionId, interactionId)
            .With(i => i.ClientId, clientId)
            .With(i => i.TenantId, tenantId)
            .With(i => i.CreatedAt, timestamp)
            .With(i => i.UpdatedAt, timestamp)
            .With(i => i.CreatedByUserId, userId)
            .With(i => i.UpdatedByUserId, userId)
            .Create();

    public static MedicareEnrollment CreateMedicareEnrollment(
        this Fixture fixture,
        Guid enrollmentId,
        Guid clientId,
        Guid tenantId,
        Guid userId,
        DateTimeOffset timestamp) =>
        fixture.Build<MedicareEnrollment>()
            .With(e => e.MedicareEnrollmentId, enrollmentId)
            .With(e => e.ClientId, clientId)
            .With(e => e.TenantId, tenantId)
            .With(e => e.CreatedAt, timestamp)
            .With(e => e.UpdatedAt, timestamp)
            .With(e => e.CreatedByUserId, userId)
            .With(e => e.UpdatedByUserId, userId)
            .Create();

    public static SupplementalEnrollment CreateSupplementalEnrollment(
        this Fixture fixture,
        Guid enrollmentId,
        Guid clientId,
        Guid tenantId,
        Guid userId,
        DateTimeOffset timestamp) =>
        fixture.Build<SupplementalEnrollment>()
            .With(e => e.SupplementalEnrollmentId, enrollmentId)
            .With(e => e.ClientId, clientId)
            .With(e => e.TenantId, tenantId)
            .With(e => e.CreatedAt, timestamp)
            .With(e => e.UpdatedAt, timestamp)
            .With(e => e.CreatedByUserId, userId)
            .With(e => e.UpdatedByUserId, userId)
            .Create();

    public static AuditStamp CreateAuditStamp(this Fixture fixture, Guid userId, DateTimeOffset timestamp) =>
        new(userId, timestamp);

    public static void SetupAuthenticatedActor(
        this Mock<IActorTracker> actorTracker,
        Guid userId,
        Guid tenantId)
    {
        actorTracker.Setup(a => a.IsAuthenticated).Returns(true);
        actorTracker.Setup(a => a.UserId).Returns(userId);
        actorTracker.Setup(a => a.TenantId).Returns(tenantId);
    }

    public static void SetupUnauthenticatedActor(this Mock<IActorTracker> actorTracker)
    {
        actorTracker.Setup(a => a.IsAuthenticated).Returns(false);
        actorTracker.Setup(a => a.UserId).Returns((Guid?)null);
        actorTracker.Setup(a => a.TenantId).Returns((Guid?)null);
    }
}
