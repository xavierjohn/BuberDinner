namespace BuberDinner.Domain.User.ValueObjects;

[Trim, NotDefault]
public partial class LastName : RequiredString<LastName>
{
}
