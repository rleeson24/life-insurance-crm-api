using System.Text.Json;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Import;
using LifeInsuranceCRM.Core.Models.Input;

namespace LifeInsuranceCRM.Core.Mappers;

public interface IAccessImportMapper
{
    MappedAccessImport Map(AccessImportModel model, DateTimeOffset now);
}

public sealed class AccessImportMapper : IAccessImportMapper
{
    public MappedAccessImport Map(AccessImportModel model, DateTimeOffset now)
    {
        var warnings = new List<string>();
        var clients = MapClients(model.Clients, warnings);
        var clientIds = clients.ToDictionary(c => c.AccessClientId, c => c.ClientId);

        var majorMedical = new List<MappedImportMajorMedicalEnrollment>();
        var drugPlans = new List<MappedImportDrugPlanEnrollment>();
        var planNames = new Dictionary<(PlanNameKind Kind, short Year, string Name), MappedImportPlanName>(
            PlanNameKeyComparer.Instance);

        MapMedEnrollments(
            model.MedEnrollments,
            clientIds,
            now,
            model.TimeZoneOffsetMinutes,
            warnings,
            majorMedical,
            drugPlans,
            planNames);

        var secondary = MapOtherEnrollments(
            model.OtherEnrollments,
            clientIds,
            now,
            model.TimeZoneOffsetMinutes,
            warnings,
            planNames);

        var interactions = MapContacts(model.Contacts, clientIds, now, model.TimeZoneOffsetMinutes, warnings);

        return new MappedAccessImport
        {
            Clients = clients,
            MajorMedicalEnrollments = majorMedical,
            DrugPlanEnrollments = drugPlans,
            SecondaryEnrollments = secondary,
            Interactions = interactions,
            PlanNames = planNames.Values.OrderBy(p => p.Kind).ThenBy(p => p.PlanYear).ThenBy(p => p.Name).ToList(),
            Warnings = CapWarnings(warnings),
        };
    }

    private static List<MappedImportClient> MapClients(
        IReadOnlyList<Dictionary<string, JsonElement>>? rows,
        List<string> warnings)
    {
        var clients = new List<MappedImportClient>();
        var seen = new HashSet<long>();
        foreach (var raw in rows ?? [])
        {
            var row = AccessRowReader.Index(raw);
            var accessClientId = AccessRowReader.GetInt64(row, "ClientID", "Client ID");
            var firstName = AccessRowReader.GetString(row, 100, "First");
            var lastName = AccessRowReader.GetString(row, 100, "Last");

            if (accessClientId is null)
            {
                AddWarning(warnings, "Skipped a client row with no ClientID.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                AddWarning(
                    warnings,
                    $"Skipped client without a first and last name (Access ClientID {accessClientId}).");
                continue;
            }

            if (!seen.Add(accessClientId.Value))
            {
                AddWarning(warnings, $"Skipped duplicate Access ClientID {accessClientId}.");
                continue;
            }

            clients.Add(new MappedImportClient
            {
                AccessClientId = accessClientId.Value,
                ClientId = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                LegalName = AccessRowReader.GetString(row, 200, "*RealName", "RealName"),
                HouseholdName = AccessRowReader.GetString(row, 200, "HouseholdName"),
                PrimaryPhone = AccessRowReader.GetString(row, 32, "Phone#", "Phone"),
                AddressLine1 = AccessRowReader.GetString(row, 200, "Address"),
                AddressLine2 = AccessRowReader.GetString(row, 200, "Address2"),
                City = AccessRowReader.GetString(row, 100, "City"),
                State = AccessRowReader.GetState(row, "State"),
                PostalCode = AccessRowReader.GetPostalCode(row, "Zip"),
                EmailAddress = AccessRowReader.GetEmail(row, "Email"),
                DateOfBirth = ToCalendarDate(AccessRowReader.GetDateTime(row, "DOB")),
                MedicareNumber = AccessRowReader.GetString(row, 32, "Med#", "Med"),
                MedicarePartAEffectiveDate = ToCalendarDate(AccessRowReader.GetDateTime(row, "A")),
                MedicarePartBEffectiveDate = ToCalendarDate(AccessRowReader.GetDateTime(row, "B")),
                IsActive = AccessRowReader.GetBool(row, defaultValue: true, "IsActive"),
                IsAcaClient = AccessRowReader.GetBool(row, defaultValue: false, "ACA"),
                HasContactConsent = AccessRowReader.GetBool(row, defaultValue: false, "Permission"),
                Notes = AccessRowReader.GetString(row, 8000, "Description"),
            });
        }

        return clients;
    }

    private static void MapMedEnrollments(
        IReadOnlyList<Dictionary<string, JsonElement>>? rows,
        IReadOnlyDictionary<long, Guid> clientIds,
        DateTimeOffset now,
        int timeZoneOffsetMinutes,
        List<string> warnings,
        List<MappedImportMajorMedicalEnrollment> majorMedical,
        List<MappedImportDrugPlanEnrollment> drugPlans,
        Dictionary<(PlanNameKind Kind, short Year, string Name), MappedImportPlanName> planNames)
    {
        foreach (var raw in rows ?? [])
        {
            var row = AccessRowReader.Index(raw);
            var accessClientId = AccessRowReader.GetInt64(row, "ClientID", "Client ID");
            if (accessClientId is null || !clientIds.TryGetValue(accessClientId.Value, out var clientId))
            {
                AddWarning(
                    warnings,
                    $"Skipped Medicare enrollment for unknown client {AccessRowReader.FormatAccessClientId(accessClientId)}.");
                continue;
            }

            var date = AccessRowReader.GetDateTime(row, "Date");
            var time = AccessRowReader.GetDateTime(row, "Time");
            var start = AccessRowReader.GetDateTime(row, "StartDate", "Start Date");
            var recordedAt = CombineRecordedAt(date, time, now, timeZoneOffsetMinutes);
            var coverageStart = ToDateOnly(start);
            var planName = AccessRowReader.GetString(row, 200, "Enrollments");
            var rxCard = AccessRowReader.GetString(row, 200, "RX Card", "RXCard");
            var isActivePlan = AccessRowReader.GetBool(row, defaultValue: false, "ActivePlan");
            var isNew = AccessRowReader.GetBool(row, defaultValue: false, "NEW");
            var hra = AccessRowReader.GetBool(row, defaultValue: false, "HRA");
            var platform = AccessRowReader.GetString(row, 200, "Platform");
            var location = AccessRowReader.GetString(row, 200, "Store");
            var notes = AccessRowReader.GetString(row, 8000, "Notes");

            if (string.IsNullOrWhiteSpace(planName) && string.IsNullOrWhiteSpace(rxCard))
            {
                AddWarning(
                    warnings,
                    $"Skipped Medicare enrollment with no plan name (Access ClientID {accessClientId}).");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(planName))
            {
                majorMedical.Add(new MappedImportMajorMedicalEnrollment
                {
                    MajorMedicalEnrollmentId = Guid.NewGuid(),
                    ClientId = clientId,
                    RecordedAt = recordedAt,
                    IsActivePlan = isActivePlan,
                    PlanName = planName,
                    CoverageStartDate = coverageStart,
                    IsNewEnrollment = isNew,
                    HealthReimbursementArrangement = hra,
                    EnrollmentPlatform = platform,
                    EnrollmentLocation = location,
                    Notes = notes,
                });
                TryAddPlanName(planNames, PlanNameKind.Medicare, coverageStart, recordedAt, planName);
            }

            if (!string.IsNullOrWhiteSpace(rxCard))
            {
                drugPlans.Add(new MappedImportDrugPlanEnrollment
                {
                    DrugPlanEnrollmentId = Guid.NewGuid(),
                    ClientId = clientId,
                    RecordedAt = recordedAt,
                    IsActivePlan = isActivePlan,
                    PlanName = rxCard,
                    CoverageStartDate = coverageStart,
                    IsNewEnrollment = isNew,
                    HealthReimbursementArrangement = hra,
                    EnrollmentPlatform = platform,
                    EnrollmentLocation = location,
                    Notes = notes,
                });
                TryAddPlanName(planNames, PlanNameKind.Drug, coverageStart, recordedAt, rxCard);
            }
        }
    }

    private static List<MappedImportSecondaryEnrollment> MapOtherEnrollments(
        IReadOnlyList<Dictionary<string, JsonElement>>? rows,
        IReadOnlyDictionary<long, Guid> clientIds,
        DateTimeOffset now,
        int timeZoneOffsetMinutes,
        List<string> warnings,
        Dictionary<(PlanNameKind Kind, short Year, string Name), MappedImportPlanName> planNames)
    {
        var enrollments = new List<MappedImportSecondaryEnrollment>();
        foreach (var raw in rows ?? [])
        {
            var row = AccessRowReader.Index(raw);
            var accessClientId = AccessRowReader.GetInt64(row, "ClientID", "Client ID");
            if (accessClientId is null || !clientIds.TryGetValue(accessClientId.Value, out var clientId))
            {
                AddWarning(
                    warnings,
                    $"Skipped secondary enrollment for unknown client {AccessRowReader.FormatAccessClientId(accessClientId)}.");
                continue;
            }

            var planOrCarrier = AccessRowReader.GetString(row, 200, "Other Insurance", "OtherInsurance");
            if (string.IsNullOrWhiteSpace(planOrCarrier))
            {
                AddWarning(
                    warnings,
                    $"Skipped secondary enrollment with no plan or carrier (Access ClientID {accessClientId}).");
                continue;
            }

            var date = AccessRowReader.GetDateTime(row, "Date");
            var time = AccessRowReader.GetDateTime(row, "Time");
            var start = AccessRowReader.GetDateTime(row, "Start Date", "StartDate");
            var recordedAt = CombineRecordedAt(date, time, now, timeZoneOffsetMinutes);
            var coverageStart = ToDateOnly(start);

            enrollments.Add(new MappedImportSecondaryEnrollment
            {
                SecondaryEnrollmentId = Guid.NewGuid(),
                ClientId = clientId,
                RecordedAt = recordedAt,
                PlanOrCarrierName = planOrCarrier,
                CoverageStartDate = coverageStart,
                IsActiveCoverage = AccessRowReader.GetBool(row, defaultValue: false, "IsActive"),
                Notes = AccessRowReader.GetString(row, 8000, "Notes"),
            });
            TryAddPlanName(planNames, PlanNameKind.Secondary, coverageStart, recordedAt, planOrCarrier);
        }

        return enrollments;
    }

    private static List<MappedImportInteraction> MapContacts(
        IReadOnlyList<Dictionary<string, JsonElement>>? rows,
        IReadOnlyDictionary<long, Guid> clientIds,
        DateTimeOffset now,
        int timeZoneOffsetMinutes,
        List<string> warnings)
    {
        var interactions = new List<MappedImportInteraction>();
        foreach (var raw in rows ?? [])
        {
            var row = AccessRowReader.Index(raw);
            var accessClientId = AccessRowReader.GetInt64(row, "ClientID", "Client ID");
            if (accessClientId is null || !clientIds.TryGetValue(accessClientId.Value, out var clientId))
            {
                AddWarning(
                    warnings,
                    $"Skipped contact for unknown client {AccessRowReader.FormatAccessClientId(accessClientId)}.");
                continue;
            }

            interactions.Add(new MappedImportInteraction
            {
                ClientInteractionId = Guid.NewGuid(),
                ClientId = clientId,
                ContactedAt = CombineRecordedAt(AccessRowReader.GetDateTime(row, "ContactDate"), null, now, timeZoneOffsetMinutes),
                Summary = AccessRowReader.GetString(row, 500, "Description"),
                Notes = AccessRowReader.GetString(row, 8000, "Notes"),
                RequiresFollowUp = AccessRowReader.GetBool(row, defaultValue: false, "Followup", "FollowUp"),
            });
        }

        return interactions;
    }

    private static void TryAddPlanName(
        Dictionary<(PlanNameKind Kind, short Year, string Name), MappedImportPlanName> planNames,
        PlanNameKind kind,
        DateOnly? coverageStart,
        DateTimeOffset recordedAt,
        string name)
    {
        var year = coverageStart?.Year ?? recordedAt.Year;
        if (year < AccessImportLimits.MinPlanYear || year > AccessImportLimits.MaxPlanYear)
        {
            return;
        }

        var key = (kind, (short)year, name);
        planNames.TryAdd(key, new MappedImportPlanName
        {
            Kind = kind,
            PlanYear = (short)year,
            Name = name,
        });
    }

    private static DateTimeOffset CombineRecordedAt(
        DateTime? date,
        DateTime? time,
        DateTimeOffset fallback,
        int timeZoneOffsetMinutes)
    {
        var dateSource = MeaningfulDate(date) ?? MeaningfulDate(time);
        if (dateSource is null)
        {
            return fallback;
        }

        var datePart = DateOnly.FromDateTime(dateSource.Value);
        var timePart = time is DateTime timeValue ? TimeOnly.FromDateTime(timeValue) : TimeOnly.MinValue;
        var unspecified = DateTime.SpecifyKind(datePart.ToDateTime(timePart), DateTimeKind.Unspecified);
        var localOffset = TimeSpan.FromMinutes(-timeZoneOffsetMinutes);
        return new DateTimeOffset(unspecified, localOffset).ToUniversalTime();
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        var meaningful = MeaningfulDate(value);
        return meaningful is null ? null : DateOnly.FromDateTime(meaningful.Value);
    }

    private static DateOnly? ToCalendarDate(DateTime? value)
    {
        if (value is not DateTime dateTime)
        {
            return null;
        }

        if (dateTime.Year < 1900 || dateTime.Year > AccessImportLimits.MaxPlanYear)
        {
            return null;
        }

        return DateOnly.FromDateTime(dateTime);
    }

    private static DateTime? MeaningfulDate(DateTime? value)
    {
        if (value is not DateTime dateTime)
        {
            return null;
        }

        if (dateTime.Year < AccessImportLimits.MinPlanYear || dateTime.Year > AccessImportLimits.MaxPlanYear)
        {
            return null;
        }

        return dateTime;
    }

    private static void AddWarning(List<string> warnings, string message)
    {
        if (warnings.Count < AccessImportLimits.MaxWarnings)
        {
            warnings.Add(message);
        }
        else if (warnings.Count == AccessImportLimits.MaxWarnings)
        {
            warnings.Add("Additional rows were skipped.");
        }
    }

    private static IReadOnlyList<string> CapWarnings(List<string> warnings) => warnings;

    private sealed class PlanNameKeyComparer : IEqualityComparer<(PlanNameKind Kind, short Year, string Name)>
    {
        public static PlanNameKeyComparer Instance { get; } = new();

        public bool Equals(
            (PlanNameKind Kind, short Year, string Name) x,
            (PlanNameKind Kind, short Year, string Name) y) =>
            x.Kind == y.Kind
            && x.Year == y.Year
            && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((PlanNameKind Kind, short Year, string Name) obj) =>
            HashCode.Combine(obj.Kind, obj.Year, StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
    }
}
