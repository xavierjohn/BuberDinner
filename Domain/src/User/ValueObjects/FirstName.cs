namespace BuberDinner.Domain.User.ValueObjects;

[Trim, NotDefault]
public partial class FirstName : RequiredString<FirstName>
{
}
