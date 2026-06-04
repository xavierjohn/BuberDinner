namespace BuberDinner.Domain.Common.ValueObjects;

[Trim, NotDefault]
public partial class Name : RequiredString<Name>
{
}
