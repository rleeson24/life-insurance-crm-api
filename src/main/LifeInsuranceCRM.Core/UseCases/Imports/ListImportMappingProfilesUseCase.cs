using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Output;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Core.Validation;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public interface IListImportMappingProfilesUseCase
{
    Task<ProcessResponse<IReadOnlyList<ImportMappingProfileDto>>> Execute(ProcessRequest<bool> request);
}

public sealed class ListImportMappingProfilesUseCase : IListImportMappingProfilesUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly IImportMappingProfileRepository _repository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public ListImportMappingProfilesUseCase(
        IActorTracker actorTracker,
        IImportMappingProfileRepository repository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _repository = repository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<IReadOnlyList<ImportMappingProfileDto>>> Execute(ProcessRequest<bool> request)
    {
        if (ImportMappingProfileUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers)
            .IsFailed(out ProcessResponse<IReadOnlyList<ImportMappingProfileDto>> failure))
        {
            return failure;
        }

        var profiles = await _repository.ListAsync(request.CancellationToken);
        var dtos = profiles
            .Select(profile => ImportMappingProfileValidator.ReadDefinition(
                profile.ImportMappingProfileId,
                profile.Name,
                profile.Definition))
            .ToList();

        return ProcessResponse<IReadOnlyList<ImportMappingProfileDto>>.Succeeded(dtos);
    }
}
