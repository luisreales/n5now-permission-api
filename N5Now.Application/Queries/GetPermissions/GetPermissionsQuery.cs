using MediatR;
using N5Now.Application.DTOs;
using System.Collections.Generic;

namespace N5Now.Application.Queries.GetPermissions
{
    public class GetPermissionsQuery : IRequest<IEnumerable<PermissionDto>> { }
}
