namespace BuberDinner.Application.Hosts.Commands;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Host.Entities;
using Mediator;

public sealed class CreateHostCommandHandler : IRequestHandler<CreateHostCommand, Result<Host>>
{
    private readonly IRepository<Host> _hostRepository;

    public CreateHostCommandHandler(IRepository<Host> hostRepository)
    {
        _hostRepository = hostRepository;
    }

    public async ValueTask<Result<Host>> Handle(CreateHostCommand request, CancellationToken cancellationToken) =>
        await Host.TryCreate(request.OwnerId, request.DisplayName)
            .TapAsync(host => _hostRepository.Add(host, cancellationToken));
}
