using System.Diagnostics;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Security;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly IDbExecutor _dbExecutor;
    private readonly IFieldEncryptionService _fieldEncryption;

    public ReportRepository(IDbExecutor dbExecutor, IFieldEncryptionService fieldEncryption)
    {
        _dbExecutor = dbExecutor;
        _fieldEncryption = fieldEncryption;
    }

    public async Task<BookOfBusinessReportDto> GetBookOfBusinessAsync(
        CancellationToken cancellationToken = default)
    {
        MarkCurrentSpanContainsPhiSql();

        const string sql = """
            SELECT TOP (@Take)
                c.ClientId, c.FirstName, c.LastName, c.LegalName, c.PrimaryPhone,
                c.AddressLine1, c.AddressLine2, c.City, c.State, c.PostalCode, c.EmailAddress,
                c.DateOfBirth, c.MedicareNumber, c.MedicarePartAEffectiveDate, c.MedicarePartBEffectiveDate,
                c.HasContactConsent, c.Notes,
                mm.PlanName AS MedicarePlanName, mm.CoverageStartDate AS MedicareCoverageStartDate,
                dp.PlanName AS DrugPlanName, dp.CoverageStartDate AS DrugCoverageStartDate,
                se.PlanOrCarrierName AS SecondaryPlanName, se.CoverageStartDate AS SecondaryCoverageStartDate
            FROM dbo.Clients c
            CROSS APPLY (
                SELECT TOP (1) PlanName, CoverageStartDate
                FROM dbo.MajorMedicalEnrollments
                WHERE ClientId = c.ClientId AND IsDeleted = 0 AND IsActivePlan = 1
                ORDER BY RecordedAt DESC
            ) mm
            OUTER APPLY (
                SELECT TOP (1) PlanName, CoverageStartDate
                FROM dbo.DrugPlanEnrollments
                WHERE ClientId = c.ClientId AND IsDeleted = 0 AND IsActivePlan = 1
                ORDER BY RecordedAt DESC
            ) dp
            OUTER APPLY (
                SELECT TOP (1) PlanOrCarrierName, CoverageStartDate
                FROM dbo.SecondaryEnrollments
                WHERE ClientId = c.ClientId AND IsDeleted = 0 AND IsActiveCoverage = 1
                ORDER BY RecordedAt DESC
            ) se
            WHERE c.IsDeleted = 0 AND c.IsActive = 1
            ORDER BY c.LastName, c.FirstName;
            """;

        var take = ReportConstants.BookRowCap + 1;
        var items = new List<BookOfBusinessRowDto>(ReportConstants.BookRowCap);
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadBookRow(reader));
                }
            },
            cancellationToken,
            new SqlParameter("@Take", take));

        var truncated = items.Count > ReportConstants.BookRowCap;
        if (truncated)
        {
            items.RemoveRange(ReportConstants.BookRowCap, items.Count - ReportConstants.BookRowCap);
        }

        return new BookOfBusinessReportDto
        {
            Items = items,
            Truncated = truncated,
        };
    }

    public async Task<MailingListReportDto> GetMailingListAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.ClientId, c.FirstName, c.LastName, c.LegalName, c.PrimaryPhone,
                c.AddressLine1, c.AddressLine2, c.City, c.State, c.PostalCode, c.EmailAddress,
                c.HasContactConsent,
                mm.PlanName AS MedicarePlanName, mm.CoverageStartDate AS MedicareCoverageStartDate,
                mm.EnrollmentLocation, mm.EnrollmentPlatform
            FROM dbo.Clients c
            OUTER APPLY (
                SELECT TOP (1) PlanName, CoverageStartDate, EnrollmentLocation, EnrollmentPlatform
                FROM dbo.MajorMedicalEnrollments
                WHERE ClientId = c.ClientId AND IsDeleted = 0 AND IsActivePlan = 1
                ORDER BY RecordedAt DESC
            ) mm
            WHERE c.IsDeleted = 0 AND c.IsActive = 1
            ORDER BY c.LastName, c.FirstName;
            """;

        var items = new List<MailingListRowDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    items.Add(ReadMailingRow(reader));
                }
            },
            cancellationToken);

        return new MailingListReportDto { Items = items };
    }

    public Task<IReadOnlyList<ProductionReportRowDto>> ListMedicareProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default) =>
        ListProductionAsync(
            """
            SELECT PlanName AS PlanName, CoverageStartDate, COUNT(*) AS EnrollmentCount
            FROM dbo.MajorMedicalEnrollments
            WHERE IsDeleted = 0
              AND CoverageStartDate IS NOT NULL
              AND YEAR(CoverageStartDate) = @PlanYear
            GROUP BY PlanName, CoverageStartDate
            ORDER BY CoverageStartDate, PlanName;
            """,
            planYear,
            cancellationToken);

    public Task<IReadOnlyList<ProductionReportRowDto>> ListDrugProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default) =>
        ListProductionAsync(
            """
            SELECT PlanName AS PlanName, CoverageStartDate, COUNT(*) AS EnrollmentCount
            FROM dbo.DrugPlanEnrollments
            WHERE IsDeleted = 0
              AND CoverageStartDate IS NOT NULL
              AND YEAR(CoverageStartDate) = @PlanYear
            GROUP BY PlanName, CoverageStartDate
            ORDER BY CoverageStartDate, PlanName;
            """,
            planYear,
            cancellationToken);

    public Task<IReadOnlyList<ProductionReportRowDto>> ListSecondaryProductionAsync(
        short planYear,
        CancellationToken cancellationToken = default) =>
        ListProductionAsync(
            """
            SELECT PlanOrCarrierName AS PlanName, CoverageStartDate, COUNT(*) AS EnrollmentCount
            FROM dbo.SecondaryEnrollments
            WHERE IsDeleted = 0
              AND CoverageStartDate IS NOT NULL
              AND YEAR(CoverageStartDate) = @PlanYear
            GROUP BY PlanOrCarrierName, CoverageStartDate
            ORDER BY CoverageStartDate, PlanOrCarrierName;
            """,
            planYear,
            cancellationToken);

    public async Task<IReadOnlyList<RetentionReportRowDto>> ListRetentionAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                firstYear.FirstYear,
                COUNT(*) AS TotalCount,
                CAST(SUM(CASE WHEN c.IsActive = 1 THEN 1 ELSE 0 END) AS int) AS StillActiveCount
            FROM (
                SELECT
                    mm.ClientId,
                    MIN(COALESCE(YEAR(mm.CoverageStartDate), DATEPART(year, mm.RecordedAt))) AS FirstYear
                FROM dbo.MajorMedicalEnrollments mm
                WHERE mm.IsDeleted = 0
                GROUP BY mm.ClientId
            ) firstYear
            INNER JOIN dbo.Clients c ON c.ClientId = firstYear.ClientId AND c.IsDeleted = 0
            GROUP BY firstYear.FirstYear
            ORDER BY firstYear.FirstYear;
            """;

        var rows = new List<RetentionReportRowDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    rows.Add(new RetentionReportRowDto
                    {
                        FirstYear = reader.GetInt32("FirstYear"),
                        TotalCount = reader.GetInt32("TotalCount"),
                        StillActiveCount = reader.GetInt32("StillActiveCount"),
                    });
                }
            },
            cancellationToken);

        return rows;
    }

    private async Task<IReadOnlyList<ProductionReportRowDto>> ListProductionAsync(
        string sql,
        short planYear,
        CancellationToken cancellationToken)
    {
        var rows = new List<ProductionReportRowDto>();
        await _dbExecutor.ExecuteReaderAsync(
            sql,
            async (reader, ct) =>
            {
                while (await reader.ReadAsync(ct))
                {
                    var planName = reader.GetNullableString("PlanName");
                    rows.Add(new ProductionReportRowDto
                    {
                        PlanName = string.IsNullOrWhiteSpace(planName)
                            ? ReportConstants.UnnamedPlan
                            : planName.Trim(),
                        CoverageStartDate = reader.GetNullableDateOnly("CoverageStartDate"),
                        EnrollmentCount = reader.GetInt32("EnrollmentCount"),
                    });
                }
            },
            cancellationToken,
            new SqlParameter("@PlanYear", planYear));

        return rows;
    }

    private static MailingListRowDto ReadMailingRow(SqlDataReader reader) => new()
    {
        ClientId = reader.GetGuid("ClientId"),
        FirstName = reader.GetNullableString("FirstName"),
        LastName = reader.GetNullableString("LastName"),
        LegalName = reader.GetNullableString("LegalName"),
        PrimaryPhone = reader.GetNullableString("PrimaryPhone"),
        AddressLine1 = reader.GetNullableString("AddressLine1"),
        AddressLine2 = reader.GetNullableString("AddressLine2"),
        City = reader.GetNullableString("City"),
        State = reader.GetNullableString("State"),
        PostalCode = reader.GetNullableString("PostalCode"),
        EmailAddress = reader.GetNullableString("EmailAddress"),
        HasContactConsent = reader.GetBoolean("HasContactConsent"),
        MedicarePlanName = reader.GetNullableString("MedicarePlanName"),
        MedicareCoverageStartDate = reader.GetNullableDateOnly("MedicareCoverageStartDate"),
        EnrollmentLocation = reader.GetNullableString("EnrollmentLocation"),
        EnrollmentPlatform = reader.GetNullableString("EnrollmentPlatform"),
    };

    private BookOfBusinessRowDto ReadBookRow(SqlDataReader reader) => new()
    {
        ClientId = reader.GetGuid("ClientId"),
        FirstName = reader.GetNullableString("FirstName"),
        LastName = reader.GetNullableString("LastName"),
        LegalName = reader.GetNullableString("LegalName"),
        PrimaryPhone = reader.GetNullableString("PrimaryPhone"),
        AddressLine1 = reader.GetNullableString("AddressLine1"),
        AddressLine2 = reader.GetNullableString("AddressLine2"),
        City = reader.GetNullableString("City"),
        State = reader.GetNullableString("State"),
        PostalCode = reader.GetNullableString("PostalCode"),
        EmailAddress = reader.GetNullableString("EmailAddress"),
        DateOfBirth = _fieldEncryption.DecryptDateOnly(reader.GetNullableBytes("DateOfBirth")),
        MedicareNumber = _fieldEncryption.Decrypt(reader.GetNullableBytes("MedicareNumber")),
        MedicarePartAEffectiveDate = _fieldEncryption.DecryptDateOnly(reader.GetNullableBytes("MedicarePartAEffectiveDate")),
        MedicarePartBEffectiveDate = _fieldEncryption.DecryptDateOnly(reader.GetNullableBytes("MedicarePartBEffectiveDate")),
        HasContactConsent = reader.GetBoolean("HasContactConsent"),
        Notes = reader.GetNullableString("Notes"),
        MedicarePlanName = reader.GetNullableString("MedicarePlanName"),
        MedicareCoverageStartDate = reader.GetNullableDateOnly("MedicareCoverageStartDate"),
        DrugPlanName = reader.GetNullableString("DrugPlanName"),
        DrugCoverageStartDate = reader.GetNullableDateOnly("DrugCoverageStartDate"),
        SecondaryPlanName = reader.GetNullableString("SecondaryPlanName"),
        SecondaryCoverageStartDate = reader.GetNullableDateOnly("SecondaryCoverageStartDate"),
    };

    private static void MarkCurrentSpanContainsPhiSql() =>
        Activity.Current?.SetTag(TelemetryConstants.ContainsPhiSqlTag, true);
}
