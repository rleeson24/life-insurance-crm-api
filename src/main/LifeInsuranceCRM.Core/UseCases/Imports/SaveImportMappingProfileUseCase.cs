using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Input;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public interface ISaveImportMappingProfileUseCase
{
    Task<ProcessResponse<ImportMappingProfileDto>> Execute(ProcessRequest<SaveImportMappingProfileModel> request);
}

public sealed class SaveImportMappingProfileUseCase : ISaveImportMappingProfileUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IImportMappingProfileRepository _repository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public SaveImportMappingProfileUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IImportMappingProfileRepository repository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _repository = repository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<ImportMappingProfileDto>> Execute(
        ProcessRequest<SaveImportMappingProfileModel> request)
    {
        if (ImportMappingProfileUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers)
            .IsFailed(out ProcessResponse<ImportMappingProfileDto> failure))
        {
            return failure;
        }

        var validation = ImportMappingProfileValidator.Validate(request.Payload);
        if (validation.IsFailed(out ProcessResponse<ImportMappingProfileDto> invalid))
        {
            return invalid;
        }

        var profile = validation.Result!;
        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var existing = await _repository.GetByNameAsync(profile.Name, request.CancellationToken);
        var saved = existing is null
            ? await _repository.InsertAsync(
                _actorTracker.TenantId!.Value,
                profile.Name,
                profile.DefinitionJson,
                audit,
                request.CancellationToken)
            : await _repository.UpdateAsync(
                existing.ImportMappingProfileId,
                profile.DefinitionJson,
                audit,
                request.CancellationToken);

        if (saved is null)
        {
            return ProcessResponse<ImportMappingProfileDto>.WithStatus(
                UseCaseStatus.NotFound,
                "Import layout not found",
                ImportErrorCodes.ProfileNotFound);
        }

        var dto = ImportMappingProfileValidator.ReadDefinition(
            saved.ImportMappingProfileId,
            saved.Name,
            saved.Definition);
        return ProcessResponse<ImportMappingProfileDto>.Succeeded(dto);
    }
}
