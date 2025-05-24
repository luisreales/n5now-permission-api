namespace N5Now.Domain.Entities;

public class PermissionType
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;

    public ICollection<Permission>? Permissions { get; set; }
}
