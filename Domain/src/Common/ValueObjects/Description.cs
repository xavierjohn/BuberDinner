namespace BuberDinner.Domain.Common.ValueObjects;

[Trim, NotDefault]
public partial class Description : RequiredString<Description>
{
}
