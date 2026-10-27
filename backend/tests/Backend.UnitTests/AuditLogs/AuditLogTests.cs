using Backend.Domain.AuditLogs;
using Backend.Domain.Common;
using Backend.UnitTests.Common;

namespace Backend.UnitTests.AuditLogs;

public sealed class AuditLogTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateAuditLog()
    {
        var userId = Guid.CreateVersion7();

        var auditLog = AuditLog.Create("Property", "123", AuditAction.Updated, """{"Title":{"old":"A","new":"B"}}""", userId, "admin@corretora.com.br", TestData.Now);

        Assert.Equal("Property", auditLog.EntityName);
        Assert.Equal(AuditAction.Updated, auditLog.Action);
        Assert.Equal(userId, auditLog.UserId);
        Assert.Equal(TestData.Now, auditLog.OccurredAt);
    }

    [Fact]
    public void Create_WithBlankChanges_ShouldStoreEmptyJsonObject()
    {
        var auditLog = AuditLog.Create("Property", "123", AuditAction.Created, " ", null, null, TestData.Now);

        Assert.Equal("{}", auditLog.Changes);
        Assert.Null(auditLog.UserEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyEntityName_ShouldThrowDomainException(string entityName)
    {
        Assert.Throws<DomainException>(() => AuditLog.Create(entityName, "123", AuditAction.Created, "{}", null, null, TestData.Now));
    }
}
