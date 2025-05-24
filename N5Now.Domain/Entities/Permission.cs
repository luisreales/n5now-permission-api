namespace N5Now.Domain.Entities;


public class Permission
{
    public Guid Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }

    public PermissionType? PermissionType { get; set; }
}


