namespace N5Now.Application.DTOs;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }
}
