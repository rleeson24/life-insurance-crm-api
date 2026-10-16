using System.Text.Json;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.Validation;

public readonly record struct ValidImportMappingProfile(string Name, string DefinitionJson, ImportMappingProfileDto Dto);

public static class ImportMappingProfileValidator
{
    public const int MaxNameLength = 100;
    public const int MaxLabelLength = 200;
    public const int MaxDefinitionLength = 16_000;

    private static readonly HashSet<string> Shapes = new(StringComparer.Ordinal)
    {
        "normalized",
        "flat",
        "ledger",
    };

    private static readonly string[] Roles =
    [
        "clients",
        "medEnrollments",
        "otherEnrollments",
        "contacts",
        "rows",
    ];

    private static readonly HashSet<string> FieldIds = new(StringComparer.Ordinal)
    {
        "clientId",
        "firstName",
        "lastName",
        "legalName",
        "householdName",
        "phone",
        "address1",
        "address2",
        "city",
        "state",
        "postalCode",
        "email",
        "dateOfBirth",
        "medicareNumber",
        "partA",
        "partB",
        "isActive",
        "aca",
        "permission",
        "clientNotes",
        "medPlan",
        "drugPlan",
        "medStart",
        "medDate",
        "medTime",
        "medActive",
        "medNew",
        "medHra",
        "medPlatform",
        "medLocation",
        "medNotes",
        "secondaryPlan",
        "secondaryStart",
        "secondaryDate",
        "secondaryActive",
        "secondaryNotes",
        "contactDate",
        "contactSummary",
        "contactNotes",
        "contactFollowUp",
        "coverageKind",
        "planName",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static ProcessResponse<ValidImportMappingProfile> Validate(SaveImportMappingProfileModel? model)
    {
        if (model is null)
        {
            return Invalid("Import layout is required");
        }

        var name = model.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > MaxNameLength || HasControlCharacter(name))
        {
            return Invalid("Layout name is required and must be 100 characters or fewer");
        }

        if (model.Shape is null || !Shapes.Contains(model.Shape))
        {
            return Invalid("Choose a file layout: separate tables, one row per client, or one row per coverage");
        }

        if (model.Tables is not null && model.Tables.Keys.Any(key => !Roles.Contains(key)))
        {
            return Invalid("The layout uses an unknown table role");
        }

        var tables = new Dictionary<string, string?>(StringComparer.Ordinal);
        var columns = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var role in Roles)
        {
            tables[role] = CleanLabel(model.Tables?.GetValueOrDefault(role));
            if (model.Tables?.TryGetValue(role, out var tableName) == true
                && !string.IsNullOrWhiteSpace(tableName)
                && tables[role] is null)
            {
                return Invalid("Table names must be 200 characters or fewer");
            }

            var roleColumns = new Dictionary<string, string>(StringComparer.Ordinal);
            if (model.Columns?.TryGetValue(role, out var incoming) == true && incoming is not null)
            {
                foreach (var (fieldId, header) in incoming)
                {
                    if (!FieldIds.Contains(fieldId))
                    {
                        return Invalid("The layout uses an unknown field");
                    }

                    var cleaned = CleanLabel(header);
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    if (cleaned is null)
                    {
                        return Invalid("Column names must be 200 characters or fewer");
                    }

                    roleColumns[fieldId] = cleaned;
                }
            }

            columns[role] = roleColumns;
        }

        var dto = new ImportMappingProfileDto
        {
            Name = name,
            Shape = model.Shape,
            Tables = tables,
            Columns = columns,
        };
        var json = JsonSerializer.Serialize(new
        {
            dto.Shape,
            dto.Tables,
            dto.Columns,
        }, JsonOptions);

        if (json.Length > MaxDefinitionLength)
        {
            return Invalid("The layout is too large");
        }

        return ProcessResponse<ValidImportMappingProfile>.Succeeded(
            new ValidImportMappingProfile(name, json, dto));
    }

    public static ImportMappingProfileDto ReadDefinition(Guid id, string name, string definitionJson)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<StoredDefinition>(definitionJson, JsonOptions);
            if (stored?.Shape is null || !Shapes.Contains(stored.Shape))
            {
                return Empty(id, name);
            }

            return new ImportMappingProfileDto
            {
                ImportMappingProfileId = id,
                Name = name,
                Shape = stored.Shape,
                Tables = FillRoles(stored.Tables),
                Columns = FillColumns(stored.Columns),
            };
        }
        catch (JsonException)
        {
            return Empty(id, name);
        }
    }

    private static ImportMappingProfileDto Empty(Guid id, string name) => new()
    {
        ImportMappingProfileId = id,
        Name = name,
        Shape = "flat",
        Tables = FillRoles(null),
        Columns = FillColumns(null),
    };

    private static Dictionary<string, string?> FillRoles(Dictionary<string, string?>? tables)
    {
        var filled = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var role in Roles)
        {
            filled[role] = tables is not null && tables.TryGetValue(role, out var name) ? name : null;
        }

        return filled;
    }

    private static Dictionary<string, Dictionary<string, string>> FillColumns(
        Dictionary<string, Dictionary<string, string>>? columns)
    {
        var filled = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var role in Roles)
        {
            filled[role] = columns is not null && columns.TryGetValue(role, out var roleColumns)
                ? roleColumns
                : new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return filled;
    }

    private static string? CleanLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLabelLength || HasControlCharacter(trimmed))
        {
            return null;
        }

        return trimmed;
    }

    private static bool HasControlCharacter(string value) =>
        value.Any(character => char.IsControl(character));

    private static ProcessResponse<ValidImportMappingProfile> Invalid(string message) =>
        ProcessResponse<ValidImportMappingProfile>.InvalidRequestResponse(
            message,
            ImportErrorCodes.ProfileInvalid);

    private sealed class StoredDefinition
    {
        public string? Shape { get; init; }

        public Dictionary<string, string?>? Tables { get; init; }

        public Dictionary<string, Dictionary<string, string>>? Columns { get; init; }
    }
}
