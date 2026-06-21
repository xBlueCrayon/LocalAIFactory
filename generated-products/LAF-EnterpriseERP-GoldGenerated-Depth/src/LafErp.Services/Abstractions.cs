namespace LafErp.Services;

/// <summary>Ambient identity for audit + maker/checker. Web binds it to the authenticated user.</summary>
public interface ICurrentUser
{
    string Username { get; }
    IReadOnlyList<string> Roles { get; }
}

/// <summary>Simple settable current-user implementation (web sets per request; tests set directly).</summary>
public sealed class CurrentUser : ICurrentUser
{
    public string Username { get; set; } = "system";
    public List<string> RoleList { get; set; } = new();
    public IReadOnlyList<string> Roles => RoleList;

    public CurrentUser() { }
    public CurrentUser(string username, params string[] roles)
    {
        Username = username;
        RoleList = roles.ToList();
    }
}
