using Backend.Domain.Abstractions;
using Backend.Domain.AuditLogs;
using Backend.Domain.Auth;
using Backend.Domain.Common;
using Backend.Domain.PropertyViews;
using Backend.Domain.Users;
using Backend.Infrastructure.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Backend.Repository.AuditLogs;

/// <summary>
/// Grava um <see cref="AuditLog"/> para cada inclusão, alteração ou exclusão salva, na mesma transação,
/// com quem fez (<see cref="ICurrentUser"/>) e o que mudou. Senhas são mascaradas. Registros de auditoria
/// não podem ser alterados nem removidos.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private const string MaskedValue = "***";

    private static readonly HashSet<Type> _ignoredTypes = [typeof(AuditLog), typeof(PropertyView), typeof(RefreshSession)];
    private static readonly HashSet<string> _sensitiveProperties = [nameof(User.PasswordHash)];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static IEnumerable<(string Name, PropertyEntry Entry)> AllProperties(EntityEntry entry) =>
        entry.Properties
            .Select(property => (DisplayName(property.Metadata.Name), property))
            .Concat(entry.ComplexProperties.SelectMany(complex => complex.Properties
                .Select(property => ($"{complex.Metadata.Name}.{property.Metadata.Name}", property))));

    private static string DisplayName(string name)
    {
        var trimmed = name.TrimStart('_');
        return trimmed.Length == 0 ? name : char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static object? Mask(string name, object? value) => _sensitiveProperties.Contains(name) ? MaskedValue : value;

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool useCurrentValues) =>
        AllProperties(entry).ToDictionary(
            property => property.Name,
            property => Mask(property.Name, useCurrentValues ? property.Entry.CurrentValue : property.Entry.OriginalValue));

    private static Dictionary<string, object?> Differences(EntityEntry entry) =>
        AllProperties(entry)
            .Where(property => property.Entry.IsModified
                && !property.Entry.Metadata.GetValueComparer().Equals(property.Entry.OriginalValue, property.Entry.CurrentValue))
            .ToDictionary(
                property => property.Name,
                property => (object?)new AuditChange(
                    Mask(property.Name, property.Entry.OriginalValue),
                    Mask(property.Name, property.Entry.CurrentValue)));

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        if (context.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Registros de auditoria não podem ser alterados nem removidos.");
        }

        var occurredAt = timeProvider.GetUtcNow();
        var auditLogs = context.ChangeTracker.Entries<AggregateRoot>()
            .Where(entry => !_ignoredTypes.Contains(entry.Entity.GetType()))
            .Select(entry => CreateAuditLog(entry, occurredAt))
            .OfType<AuditLog>()
            .ToList();

        context.Set<AuditLog>().AddRange(auditLogs);
    }

    private AuditLog? CreateAuditLog(EntityEntry<AggregateRoot> entry, DateTimeOffset occurredAt)
    {
        AuditAction action;
        Dictionary<string, object?> changes;
        switch (entry.State)
        {
            case EntityState.Added:
                action = AuditAction.Created;
                changes = Snapshot(entry, useCurrentValues: true);
                break;
            case EntityState.Deleted:
                action = AuditAction.Deleted;
                changes = Snapshot(entry, useCurrentValues: false);
                break;
            case EntityState.Modified:
                action = AuditAction.Updated;
                changes = Differences(entry);
                break;
            default:
                return null;
        }

        if (changes.Count == 0)
        {
            return null;
        }

        return AuditLog.Create(
            entry.Metadata.ClrType.Name,
            entry.Entity.Id.ToString(),
            action,
            changes.ToJson(),
            currentUser.UserId,
            currentUser.Email,
            occurredAt);
    }

    private sealed record AuditChange(object? Old, object? New);
}
