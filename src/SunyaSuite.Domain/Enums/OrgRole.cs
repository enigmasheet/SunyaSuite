namespace SunyaSuite.Domain.Enums;

public static class OrgRoles
{
    public const string Owner = "Owner";
    public const string OrgAdmin = "OrgAdmin";
    public const string Member = "Member";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Owner, OrgAdmin, Member, Viewer];
}
