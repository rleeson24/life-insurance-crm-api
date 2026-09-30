using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Services;

namespace LifeInsuranceCRM.Core.Tests.Services;

public class SecurityEventDetailTests
{
    [Fact]
    public void AccessChange_RecordsRoleAndActiveFlagWithoutProfileText()
    {
        var detail = SecurityEventDetail.AccessChange(
            OrganizationRoles.Admin,
            OrganizationRoles.Agent,
            previousActive: true,
            nextActive: false);

        Assert.Equal("role=Admin->Agent;isActive=true->false", detail);
    }

    [Fact]
    public void AccessChange_WhenUnchanged_ReturnsNull()
    {
        Assert.Null(SecurityEventDetail.AccessChange(
            OrganizationRoles.Agent,
            OrganizationRoles.Agent,
            previousActive: true,
            nextActive: true));
    }

    [Fact]
    public void AccessChange_RejectsFreeTextRoles()
    {
        var detail = SecurityEventDetail.AccessChange("Admin", "Pat Lee", previousActive: true, nextActive: true);

        Assert.Equal("role=Admin->unknown", detail);
    }

    [Fact]
    public void Import_OmitsBytesWhenUnknownAndKeepsCounts()
    {
        var detail = SecurityEventDetail.Import(
            bytes: null,
            clients: 2,
            majorMedical: 1,
            drug: 0,
            secondary: 0,
            interactions: 3,
            warnings: 1,
            outcome: "inserted");

        Assert.Equal("clients=2;majorMedical=1;drug=0;secondary=0;interactions=3;warnings=1;outcome=inserted", detail);
    }

    [Fact]
    public void TenantChange_RecordsActiveFlagWithoutTheName()
    {
        Assert.Equal(
            "isActive=true->false;nameChanged=true",
            SecurityEventDetail.TenantChange(previousActive: true, nextActive: false, nameChanged: true));
    }
}
