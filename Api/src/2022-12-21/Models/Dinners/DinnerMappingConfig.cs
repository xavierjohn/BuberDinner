namespace BuberDinner.Api._2022_12_21.Models.Dinners;

using BuberDinner.Domain.Dinner.Entities;
using Mapster;

/// <summary>
/// Mapster mapping config from the <see cref="Dinner"/> aggregate to <see cref="DinnerResponse"/>.
/// Auto-discovered by <c>config.Scan(Assembly.GetExecutingAssembly())</c> in Api/DI.
/// </summary>
public sealed class DinnerMappingConfig : IRegister
{
    /// <summary>Registers the <see cref="Dinner"/> → <see cref="DinnerResponse"/> mapping.</summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Dinner, DinnerResponse>()
            .Map(dest => dest.Id, src => src.Id.Value.ToString())
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Description, src => src.Description.Value)
            .Map(dest => dest.HostId, src => src.HostId.Value.ToString())
            .Map(dest => dest.MenuId, src => src.MenuId.Value.ToString())
            .Map(dest => dest.Status, src => src.Status.Value);
    }
}
