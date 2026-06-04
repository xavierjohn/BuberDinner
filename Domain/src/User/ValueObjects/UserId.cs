namespace BuberDinner.Domain.User.ValueObjects;

[Trim, NotDefault]
public partial class UserId : RequiredString<UserId>
{
}
