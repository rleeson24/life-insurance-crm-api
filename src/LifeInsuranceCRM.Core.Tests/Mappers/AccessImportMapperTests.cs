using System.Text.Json;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Mappers;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Tests.Mappers;

public class AccessImportMapperTests
{
    private readonly AccessImportMapper _mapper = new();
    private readonly DateTimeOffset _now = new(2026, 8, 28, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Map_DustinClientRow_MapsFieldsAndOmitsCredentials()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients =
                [
                    Row(
                        ("ClientID", 42),
                        ("IsActive", true),
                        ("ACA", true),
                        ("First", "Jane"),
                        ("*RealName", "Jane Q Public"),
                        ("HouseholdName", "Public Household"),
                        ("Last", "Public"),
                        ("Phone#", "8435550100"),
                        ("Address", "1 Ocean Blvd"),
                        ("Address2", "Apt 2"),
                        ("City", "Myrtle Beach"),
                        ("State", "sc"),
                        ("Zip", 29577.0),
                        ("Email", "Jane#mailto:jane@example.com#"),
                        ("DOB", new DateTime(1952, 3, 4, 0, 0, 0, DateTimeKind.Utc)),
                        ("Med#", "1EG4TE5MK72"),
                        ("A", new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                        ("B", new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                        ("Permission", true),
                        ("Username", "secret-user"),
                        ("Password", "secret-pass"),
                        ("Description", "Prefers morning calls")),
                ],
            },
            _now);

        var client = Assert.Single(mapped.Clients);
        Assert.Equal(42, client.AccessClientId);
        Assert.Equal("Jane", client.FirstName);
        Assert.Equal("Public", client.LastName);
        Assert.Equal("Jane Q Public", client.LegalName);
        Assert.Equal("Public Household", client.HouseholdName);
        Assert.Equal("8435550100", client.PrimaryPhone);
        Assert.Equal("1 Ocean Blvd", client.AddressLine1);
        Assert.Equal("Apt 2", client.AddressLine2);
        Assert.Equal("Myrtle Beach", client.City);
        Assert.Equal("SC", client.State);
        Assert.Equal("29577", client.PostalCode);
        Assert.Equal("jane@example.com", client.EmailAddress);
        Assert.Equal(new DateOnly(1952, 3, 4), client.DateOfBirth);
        Assert.Equal("1EG4TE5MK72", client.MedicareNumber);
        Assert.True(client.IsActive);
        Assert.True(client.IsAcaClient);
        Assert.True(client.HasContactConsent);
        Assert.Equal("Prefers morning calls", client.Notes);
        Assert.DoesNotContain(mapped.Warnings, w => w.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("secret-user", client.Notes ?? string.Empty);
        Assert.DoesNotContain("secret-pass", client.Notes ?? string.Empty);
    }

    [Fact]
    public void Map_HraTextAndInvalidState_Normalizes()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients = [Row(("ClientID", 1), ("First", "Ann"), ("Last", "Lee"), ("State", "South Carolina"))],
                MedEnrollments =
                [
                    Row(
                        ("ClientID", 1),
                        ("Date", new DateTime(2025, 10, 16, 0, 0, 0, DateTimeKind.Utc)),
                        ("Time", new DateTime(1899, 12, 30, 9, 15, 0, DateTimeKind.Utc)),
                        ("ActivePlan", true),
                        ("Enrollments", "Humana Gold Plus"),
                        ("StartDate", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                        ("HRA", "Yes")),
                ],
            },
            _now);

        Assert.Null(mapped.Clients[0].State);
        var enrollment = Assert.Single(mapped.MajorMedicalEnrollments);
        Assert.True(enrollment.HealthReimbursementArrangement);
        Assert.Equal(new DateTimeOffset(2025, 10, 16, 9, 15, 0, TimeSpan.Zero), enrollment.RecordedAt);
        Assert.Equal(new DateOnly(2026, 1, 1), enrollment.CoverageStartDate);
    }

    [Fact]
    public void Map_AccessDateOnly_UsesBrowserOffsetSoLocalMidnightStaysThatCalendarDay()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                TimeZoneOffsetMinutes = 240,
                Clients = [Row(("ClientID", 1), ("First", "Ann"), ("Last", "Lee"))],
                MedEnrollments =
                [
                    Row(
                        ("ClientID", 1),
                        ("Date", new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc)),
                        ("Enrollments", "Humana Gold Plus"),
                        ("StartDate", new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc))),
                ],
                Contacts =
                [
                    Row(
                        ("ClientID", 1),
                        ("ContactDate", new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc)),
                        ("Description", "Called")),
                ],
            },
            _now);

        Assert.Equal(new DateTimeOffset(2026, 8, 3, 4, 0, 0, TimeSpan.Zero), mapped.MajorMedicalEnrollments[0].RecordedAt);
        Assert.Equal(new DateOnly(2026, 8, 31), mapped.MajorMedicalEnrollments[0].CoverageStartDate);
        Assert.Equal(new DateTimeOffset(2025, 8, 1, 4, 0, 0, TimeSpan.Zero), mapped.Interactions[0].ContactedAt);
    }

    [Fact]
    public void Map_MedRow_SplitsMajorMedicalAndDrugAndBucketsPlanNamesByYear()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients = [Row(("ClientID", 7), ("First", "Pat"), ("Last", "Kim"))],
                MedEnrollments =
                [
                    Row(
                        ("ClientID", 7),
                        ("Date", new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc)),
                        ("ActivePlan", true),
                        ("Enrollments", "Aetna MA"),
                        ("RX Card", "SilverScript"),
                        ("StartDate", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                    Row(
                        ("ClientID", 7),
                        ("Date", new DateTime(2024, 11, 2, 0, 0, 0, DateTimeKind.Utc)),
                        ("Enrollments", "Aetna MA"),
                        ("RX Card", "SilverScript"),
                        ("StartDate", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                ],
            },
            _now);

        Assert.Equal(2, mapped.MajorMedicalEnrollments.Count);
        Assert.Equal(2, mapped.DrugPlanEnrollments.Count);
        Assert.Equal("Aetna MA", mapped.MajorMedicalEnrollments[0].PlanName);
        Assert.Equal("SilverScript", mapped.DrugPlanEnrollments[0].PlanName);
        Assert.Contains(mapped.PlanNames, p => p.Kind == PlanNameKind.Medicare && p.PlanYear == 2026 && p.Name == "Aetna MA");
        Assert.Contains(mapped.PlanNames, p => p.Kind == PlanNameKind.Drug && p.PlanYear == 2025 && p.Name == "SilverScript");
        Assert.Equal(4, mapped.PlanNames.Count);
    }

    [Fact]
    public void Map_OrphanEnrollmentAndMissingNames_AreSkippedWithWarnings()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients =
                [
                    Row(("ClientID", 1), ("First", " "), ("Last", "NoFirst")),
                    Row(("ClientID", 2), ("First", "Ok"), ("Last", "Client")),
                ],
                MedEnrollments =
                [
                    Row(("ClientID", 99), ("Enrollments", "Ghost Plan")),
                    Row(("ClientID", 2), ("Enrollments", "Humana")),
                ],
                OtherEnrollments =
                [
                    Row(("ClientID", 2), ("Other Insurance", "Aflac"), ("Start Date", new DateTime(2026, 1, 1))),
                    Row(("ClientID", 2), ("Other Insurance", " ")),
                ],
                Contacts =
                [
                    Row(("ClientID", 2), ("Description", "Called"), ("Followup", true)),
                    Row(("ClientID", 88), ("Description", "Orphan")),
                ],
            },
            _now);

        Assert.Single(mapped.Clients);
        Assert.Equal("Ok", mapped.Clients[0].FirstName);
        Assert.Single(mapped.MajorMedicalEnrollments);
        Assert.Single(mapped.SecondaryEnrollments);
        Assert.Equal("Aflac", mapped.SecondaryEnrollments[0].PlanOrCarrierName);
        Assert.Single(mapped.Interactions);
        Assert.True(mapped.Interactions[0].RequiresFollowUp);
        Assert.Contains(mapped.Warnings, w => w.Contains("first and last name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.Warnings, w => w.Contains("unknown client 99", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.Warnings, w => w.Contains("unknown client 88", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.Warnings, w => w.Contains("no plan or carrier", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.PlanNames, p => p.Kind == PlanNameKind.Secondary && p.Name == "Aflac");
    }

    [Fact]
    public void Map_InvalidEmailAndMissingCoverageYear_LeavesEmailNullAndSkipsCatalogWhenYearUnknown()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients = [Row(("ClientID", 3), ("First", "Sam"), ("Last", "Wu"), ("Email", "not-an-email"))],
                MedEnrollments =
                [
                    Row(("ClientID", 3), ("Enrollments", "Humana"), ("Date", new DateTime(1899, 12, 30))),
                ],
            },
            _now);

        Assert.Null(mapped.Clients[0].EmailAddress);
        Assert.Single(mapped.MajorMedicalEnrollments);
        Assert.Equal(_now, mapped.MajorMedicalEnrollments[0].RecordedAt);
        Assert.Contains(mapped.PlanNames, p => p.Kind == PlanNameKind.Medicare && p.PlanYear == 2026 && p.Name == "Humana");
    }

    [Fact]
    public void Map_NullAndEmptyCollections_ReturnsEmptyLists()
    {
        var mapped = _mapper.Map(new AccessImportModel(), _now);

        Assert.Empty(mapped.Clients);
        Assert.Empty(mapped.MajorMedicalEnrollments);
        Assert.Empty(mapped.DrugPlanEnrollments);
        Assert.Empty(mapped.SecondaryEnrollments);
        Assert.Empty(mapped.Interactions);
        Assert.Empty(mapped.PlanNames);
        Assert.Empty(mapped.Warnings);
    }

    [Fact]
    public void Map_EmptyClientArray_ReturnsNoClients()
    {
        var mapped = _mapper.Map(new AccessImportModel { Clients = [] }, _now);

        Assert.Empty(mapped.Clients);
    }

    [Fact]
    public void Map_PlanYearAtMinAndMax_AddsCatalogEntries()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients = [Row(("ClientID", 1), ("First", "Ann"), ("Last", "Lee"))],
                MedEnrollments =
                [
                    Row(
                        ("ClientID", 1),
                        ("Enrollments", "Min Plan"),
                        ("StartDate", new DateTime(AccessImportLimits.MinPlanYear, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                    Row(
                        ("ClientID", 1),
                        ("Enrollments", "Max Plan"),
                        ("StartDate", new DateTime(AccessImportLimits.MaxPlanYear, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                ],
            },
            _now);

        Assert.Contains(
            mapped.PlanNames,
            p => p.Kind == PlanNameKind.Medicare && p.PlanYear == AccessImportLimits.MinPlanYear && p.Name == "Min Plan");
        Assert.Contains(
            mapped.PlanNames,
            p => p.Kind == PlanNameKind.Medicare && p.PlanYear == AccessImportLimits.MaxPlanYear && p.Name == "Max Plan");
    }

    [Fact]
    public void Map_CoverageYearJustBelowMin_OmitsCoverageDateAndUsesRecordedYearForCatalog()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients = [Row(("ClientID", 1), ("First", "Ann"), ("Last", "Lee"))],
                MedEnrollments =
                [
                    Row(
                        ("ClientID", 1),
                        ("Enrollments", "Too Old"),
                        ("StartDate", new DateTime(AccessImportLimits.MinPlanYear - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                ],
            },
            _now);

        Assert.Null(Assert.Single(mapped.MajorMedicalEnrollments).CoverageStartDate);
        Assert.Equal(_now.Year, Assert.Single(mapped.PlanNames).PlanYear);
    }

    [Fact]
    public void Map_UnicodeAndSpecialCharacters_ArePreservedOnFreeText()
    {
        var mapped = _mapper.Map(
            new AccessImportModel
            {
                Clients =
                [
                    Row(
                        ("ClientID", 1),
                        ("First", "O'Brien"),
                        ("Last", "李"),
                        ("Description", "Prefers <morning> & \"quotes\"")),
                ],
            },
            _now);

        var client = Assert.Single(mapped.Clients);
        Assert.Equal("O'Brien", client.FirstName);
        Assert.Equal("李", client.LastName);
        Assert.Equal("Prefers <morning> & \"quotes\"", client.Notes);
    }

    private static Dictionary<string, JsonElement> Row(params (string Key, object? Value)[] fields)
    {
        var row = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
        {
            row[key] = JsonSerializer.SerializeToElement(value);
        }

        return row;
    }
}
