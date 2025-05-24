using MediatR;
using System;

namespace N5Now.Application.Commands.RequestPermission
{
    public class RequestPermissionCommand : IRequest<Guid>
    {
        public string EmployeeName { get; set; } = string.Empty;
        public int PermissionTypeId { get; set; }
        public DateTime PermissionDate { get; set; }
    }
}
