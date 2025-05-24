using MediatR;
using Microsoft.EntityFrameworkCore;
using N5Now.Application.Commands.ModifyPermission;
using N5Now.Domain.Kafka;
using N5Now.Infrastructure.Data;
using N5Now.Infrastructure.Kafka;
using Nest; // Elasticsearch
using Microsoft.Extensions.Logging;

namespace N5Now.Infrastructure.Handlers.ModifyPermission;

public class ModifyPermissionHandler : IRequestHandler<ModifyPermissionCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IKafkaProducerService _kafka;
    private readonly IElasticClient _elastic;
    private readonly ILogger<ModifyPermissionHandler> _logger;

    public ModifyPermissionHandler(
        AppDbContext context,
        IKafkaProducerService kafka,
        IElasticClient elastic,
        ILogger<ModifyPermissionHandler> logger)
    {
        _context = context;
        _kafka = kafka;
        _elastic = elastic;
        _logger = logger;
    }

    public async Task<bool> Handle(ModifyPermissionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (entity == null)
            return false;

        entity.EmployeeName = request.EmployeeName;
        entity.PermissionTypeId = request.PermissionTypeId;
        entity.PermissionDate = request.PermissionDate;

        await _context.SaveChangesAsync(cancellationToken);

        // Enviar a Kafka
        var eventDto = new PermissionEventDto
        {
            Id = entity.Id,
            Operation = "modify",
            EmployeeName = entity.EmployeeName,
            PermissionTypeId = entity.PermissionTypeId,
            PermissionDate = entity.PermissionDate
        };

        await _kafka.SendMessageAsync(eventDto, cancellationToken);

        // Indexar en Elasticsearch
        var response = await _elastic.IndexDocumentAsync(entity);

        if (!response.IsValid)
        {
            _logger.LogError("Elasticsearch indexing failed: {Error}", response.OriginalException.Message);
        }

        return true;
    }
}