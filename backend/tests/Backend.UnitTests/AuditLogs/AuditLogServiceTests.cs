using System.Text.Json;
using Backend.Application.AuditLogs;
using Backend.Application.AuditLogs.Dtos;
using Backend.Application.Common;
using Backend.Domain.AuditLogs;
using Backend.Domain.Common;
using Backend.UnitTests.Common;
using NSubstitute;

namespace Backend.UnitTests.AuditLogs;

public sealed class AuditLogServiceTests
{
    private readonly IAuditLogRepository _auditLogRepository = Substitute.For<IAuditLogRepository>();
    private readonly AuditLogService _auditLogService;

    public AuditLogServiceTests()
    {
        _auditLogService = new AuditLogService(_auditLogRepository);
    }

    [Fact]
    public async Task ListAsync_WithFilter_ShouldPassFilterAndParseChanges()
    {
        var auditLog = AuditLog.Create("Property", "1", AuditAction.Updated, """{"Title":{"old":"A","new":"B"}}""", null, null, TestData.Now);
        _auditLogRepository
            .SearchAsync(Arg.Any<AuditLogFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedList<AuditLog>([auditLog], 1, 1, 50));

        var page = await _auditLogService.ListAsync(new ListAuditLogsRequest(EntityName: "Property", Action: AuditAction.Updated));

        var response = Assert.Single(page.Items);
        Assert.Equal("B", response.Changes.GetProperty("Title").GetProperty("new").GetString());
        Assert.Equal(JsonValueKind.Object, response.Changes.ValueKind);
        await _auditLogRepository.Received(1).SearchAsync(
            Arg.Is<AuditLogFilter>(filter => filter.EntityName == "Property" && filter.Action == AuditAction.Updated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenAuditLogDoesNotExist_ShouldReturnNotFound()
    {
        var id = Guid.CreateVersion7();
        _auditLogRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((AuditLog?)null);

        var result = await _auditLogService.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
