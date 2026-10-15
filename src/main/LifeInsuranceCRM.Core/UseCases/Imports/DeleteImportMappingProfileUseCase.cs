using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.UseCases.Clients;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Imports;

public interface IDeleteImportMappingProfileUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<Guid> request);
}

public sealed class DeleteImportMappingProfileUseCase : IDeleteImportMappingProfileUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IImportMappingProfileRepository _repository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeleteImportMappingProfileUseCase(
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

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<Guid> request)
    {
        if (ImportMappingProfileUseCaseHelpers.ValidateAdmin(_actorTracker, _clientUseCaseHelpers)
            .IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        if (request.Payload == Guid.Empty)
        {
            return ProcessResponse<bool>.InvalidRequestResponse(
                "Layout id is required",
                ImportErrorCodes.ProfileInvalid);
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _repository.SoftDeleteAsync(request.Payload, audit, request.CancellationToken);
        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Import layout not found",
                ImportErrorCodes.ProfileNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
