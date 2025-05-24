using MediatR;

namespace N5Now.Application.Commands.ModifyPermission;

public class ModifyPermissionCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public DateTime PermissionDate { get; set; }
}
