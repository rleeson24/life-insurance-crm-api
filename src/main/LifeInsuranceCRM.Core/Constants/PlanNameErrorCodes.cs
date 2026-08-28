namespace LifeInsuranceCRM.Core.Constants;

public static class PlanNameErrorCodes
{
    public const string ActorNotAuthenticated = "actor.not_authenticated";
    public const string ActorNotAdmin = "plan_name.actor.not_admin";
    public const string KindInvalid = "plan_name.kind.invalid";
    public const string IdInvalid = "plan_name.id.invalid";
    public const string NotFound = "plan_name.not_found";
    public const string NameRequired = "plan_name.name.required";
    public const string NameTooLong = "plan_name.name.too_long";
    public const string NameAlreadyExists = "plan_name.name.already_exists";
    public const string PlanYearInvalid = "plan_name.year.invalid";
    public const string YearRangeInvalid = "plan_name.year_range.invalid";
    public const string CloneYearsInvalid = "plan_name.clone.years_invalid";
}
