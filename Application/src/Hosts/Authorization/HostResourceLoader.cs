namespace BuberDinner.Application.Hosts.Authorization;

using System.Threading;
using System.Threading.Tasks;
using BuberDinner.Application.Abstractions.Persistence;
using BuberDinner.Domain.Host.Entities;
using BuberDinner.Domain.Host.ValueObject;
using Trellis.Authorization;

/// <summary>
/// Loads a <see cref="Host"/> by <see cref="HostId"/> for the resource-authorization pipeline.
/// Any command implementing both <c>IAuthorizeResource&lt;Host&gt;</c> and
/// <c>IIdentifyResource&lt;Host, HostId&gt;</c> reuses this shared loader — no per-command
/// loader required (per Cookbook Recipe 7 and trellis-api-authorization.md:333).
/// </summary>
public sealed class HostResourceLoader : SharedResourceLoaderById<Host, HostId>
{
    private readonly IRepository<Host> _hostRepository;

    public HostResourceLoader(IRepository<Host> hostRepository)
    {
        _hostRepository = hostRepository;
    }

    public override async Task<Result<Host>> GetByIdAsync(HostId id, CancellationToken cancellationToken) =>
        (await _hostRepository.FindById(id.Value.ToString(), cancellationToken))
            .ToResult(new Error.NotFound(ResourceRef.For<Host>(id)));
}
