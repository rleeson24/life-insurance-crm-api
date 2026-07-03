using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Models.Requests;
using LifeInsuranceCRM.Utilities;

namespace LifeInsuranceCRM.Core.UseCases.Clients;

public interface IDeleteClientUseCase
{
    Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientRequest> request);
}

public sealed class DeleteClientUseCase : IDeleteClientUseCase
{
    private readonly IActorTracker _actorTracker;
    private readonly INowProvider _nowProvider;
    private readonly IClientRepository _clientRepository;
    private readonly IClientUseCaseHelpers _clientUseCaseHelpers;

    public DeleteClientUseCase(
        IActorTracker actorTracker,
        INowProvider nowProvider,
        IClientRepository clientRepository,
        IClientUseCaseHelpers clientUseCaseHelpers)
    {
        _actorTracker = actorTracker;
        _nowProvider = nowProvider;
        _clientRepository = clientRepository;
        _clientUseCaseHelpers = clientUseCaseHelpers;
    }

    public async Task<ProcessResponse<bool>> Execute(ProcessRequest<DeleteClientRequest> request)
    {
        var validation = _clientUseCaseHelpers.ValidateClientAccess(_actorTracker, request.Payload.ClientId);
        if (validation.IsFailed(out ProcessResponse<bool> failure))
        {
            return failure;
        }

        var audit = _clientUseCaseHelpers.CreateAuditStamp(_actorTracker, _nowProvider);
        var deleted = await _clientRepository.SoftDeleteAsync(
            request.Payload.ClientId,
            audit,
            request.CancellationToken);

        if (!deleted)
        {
            return ProcessResponse<bool>.WithStatus(
                UseCaseStatus.NotFound,
                "Client not found",
                ClientErrorCodes.ClientNotFound);
        }

        return ProcessResponse<bool>.Succeeded(true);
    }
}
