@Skill Creator

Nome da Skill: config-dtos-entities
Língua: Escrever em português do Brasil
Pasta: Dentro do projeto (.claude/skills)

Objetivo: criar, de forma determinística, as classes raiz do domínio (base de entidades, agregados, value objects, eventos e Result) e, a partir do nome de uma entidade e dos seus campos, a entidade de domínio (aggregate root), os DTOs de entrada e saída, o mapping e os testes da entidade no backend .NET criado pela skill config-project-fullstack.

Entradas:
- entidade (obrigatório): nome em PascalCase no singular (ex.: Report, Transaction, UserProfile)
- modulo (obrigatório): nome em kebab-case no plural (ex.: reports, transactions, user-profiles). A pasta do módulo é o nome em PascalCase (Reports, Transactions, UserProfiles)
- campos (opcional): lista `Nome:tipo` separada por vírgula, com Nome em PascalCase. Padrão: `Name:string`
	- Tipos aceitos: string, int, long, decimal, bool, Guid, DateOnly, DateTimeOffset
	- Sufixo `?` deixa o campo opcional (ex.: `Notes:string?`, `DueDate:DateOnly?`)
	- Não aceitar DateTime (usar DateTimeOffset em UTC). Qualquer outro tipo: abortar com mensagem listando os tipos aceitos
	- Não aceitar campo chamado Id (o Id vem da classe Entity)
- Exemplo de chamada: `entidade: Transaction, modulo: transactions, campos: Description:string, Amount:decimal, Notes:string?, OccurredAt:DateTimeOffset`

Pré-condições (abortar sem alterar nada se falhar):
- Existe backend/Backend.slnx com os projetos src/Backend.Domain, src/Backend.Application e tests/Backend.UnitTests
- Para a parte 2: o arquivo backend/src/Backend.Domain/<Modulo>/<Entidade>.cs não pode existir

## Parte 1: classes raiz (idempotente)

Criar cada arquivo abaixo somente se ele não existir. Nunca sobrescrever um arquivo existente. Essa parte roda sempre, antes da parte 2, e também pode ser executada sozinha (sem entidade) para preparar o projeto.

- backend/src/Backend.Domain/Common/Entity.cs (Id Guid v7 gerado no domínio e igualdade por Id):
```csharp
namespace Backend.Domain.Common;

public abstract class Entity : IEquatable<Entity>
{
    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    public Guid Id { get; private set; }

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);

    public bool Equals(Entity? other) => other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
```

- backend/src/Backend.Domain/Common/AggregateRoot.cs (raiz de agregado; único tipo que tem repository):
```csharp
namespace Backend.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
```

- backend/src/Backend.Domain/Common/ValueObject.cs (igualdade por valor):
```csharp
namespace Backend.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);

    public bool Equals(ValueObject? other) =>
        other is not null
        && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override bool Equals(object? obj) => obj is ValueObject valueObject && Equals(valueObject);

    public override int GetHashCode() =>
        GetEqualityComponents().Aggregate(0, (hash, component) => HashCode.Combine(hash, component));

    protected abstract IEnumerable<object?> GetEqualityComponents();
}
```

- backend/src/Backend.Domain/Common/IDomainEvent.cs:
```csharp
namespace Backend.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
```

- backend/src/Backend.Domain/Common/DomainException.cs (violação de regra de negócio):
```csharp
namespace Backend.Domain.Common;

public sealed class DomainException(string message) : Exception(message);
```

- backend/src/Backend.Application/Common/ErrorType.cs:
```csharp
namespace Backend.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}
```

- backend/src/Backend.Application/Common/Error.cs:
```csharp
namespace Backend.Application.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
```

- backend/src/Backend.Application/Common/Result.cs (falhas esperadas viram Result, não exceção; `Result` e `Result<TValue>` ficam no mesmo arquivo):
```csharp
using System.Diagnostics.CodeAnalysis;

namespace Backend.Application.Common;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if ((isSuccess && error is not null) || (!isSuccess && error is null))
        {
            throw new ArgumentException("Resultado de sucesso não pode ter erro e resultado de falha precisa de erro.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static implicit operator Result(Error error) => Failure(error);

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(true, null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(false, error)
    {
        _value = default;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado com falha.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error) => new(error);
}
```

- backend/tests/Backend.ArchitectureTests/DomainModelTests.cs (somente se o projeto tests/Backend.ArchitectureTests existir):
```csharp
using System.Reflection;
using Backend.Domain.Common;
using NetArchTest.Rules;

namespace Backend.ArchitectureTests;

public sealed class DomainModelTests
{
    [Fact]
    public void AggregateRoots_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(AggregateRoot))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void ValueObjects_ShouldBeSealed()
    {
        var result = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(ValueObject))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        ArchitectureAssert.Success(result);
    }

    [Fact]
    public void Entities_ShouldNotHavePublicSetters()
    {
        var violations = Types.InAssembly(Layers.Domain)
            .That()
            .Inherit(typeof(Entity))
            .GetTypes()
            .SelectMany(type => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.Name}.{property.Name}"))
            .ToList();

        Assert.Empty(violations);
    }
}
```

## Parte 2: entidade, DTOs, mapping e testes

Arquivos gerados (exemplo para entidade Report, modulo reports):
| Arquivo | Conteúdo |
| --- | --- |
| backend/src/Backend.Domain/Reports/Report.cs | Entidade (aggregate root) |
| backend/src/Backend.Application/Reports/Dtos/CreateReportRequest.cs | DTO de criação |
| backend/src/Backend.Application/Reports/Dtos/UpdateReportRequest.cs | DTO de atualização |
| backend/src/Backend.Application/Reports/Dtos/ReportResponse.cs | DTO de saída |
| backend/src/Backend.Application/Reports/ReportMappings.cs | Entidade → Response |
| backend/tests/Backend.UnitTests/Reports/ReportTests.cs | Testes da entidade |

Regras de geração por tipo de campo (<Campo> em PascalCase, <campo> em camelCase):
| Tipo | Propriedade na entidade | Validação no Set<Campo> | Atributos no Request | Valor de teste |
| --- | --- | --- | --- | --- |
| string | `public string <Campo> { get; private set; } = string.Empty;` + `public const int <Campo>MaxLength = 200;` | vazio/espaços → DomainException "O campo <Campo> é obrigatório."; maior que <Campo>MaxLength após Trim → DomainException; grava com Trim | `[Required, MaxLength(<Entidade>.<Campo>MaxLength)]` | `"Valor de teste"` (update: `"Valor atualizado"`) |
| string? | `public string? <Campo> { get; private set; }` + const MaxLength | vazio/espaços → grava null; maior que o máximo → DomainException; grava com Trim | `[MaxLength(<Entidade>.<Campo>MaxLength)]` | `"Valor de teste"` |
| Guid | `public Guid <Campo> { get; private set; }` | Guid.Empty → DomainException "O campo <Campo> é obrigatório." | nenhum | `new Guid("0199a000-0000-7000-8000-000000000001")` |
| DateTimeOffset / DateTimeOffset? | propriedade do mesmo tipo | grava em UTC com `.ToUniversalTime()` (no opcional, só se tiver valor) | nenhum | `new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)` |
| int, long, decimal, bool, DateOnly e opcionais (`?`) | propriedade do mesmo tipo | nenhuma, só atribui | nenhum | `1`, `1L`, `10.5m`, `true`, `new DateOnly(2026, 1, 1)` |

Regras da entidade:
- `public sealed class <Entidade> : AggregateRoot`, namespace `Backend.Domain.<Modulo>`
- Construtor privado sem parâmetros (usado pelo EF Core)
- `public static <Entidade> Create(<campos na ordem informada>)` cria a instância e chama Set<Campo> para cada campo
- `public void Update(<campos na ordem informada>)` chama Set<Campo> para cada campo
- Um método privado Set<Campo> por campo, mesmo quando não há validação
- Nenhum setter público, nenhum atributo de EF Core ou de validação na entidade

Regras dos DTOs:
- `public sealed record`, namespace `Backend.Application.<Modulo>.Dtos`
- Create<Entidade>Request e Update<Entidade>Request: os campos na ordem informada, com os atributos da tabela nos parâmetros
- <Entidade>Response: `Guid Id` seguido dos campos na ordem informada
- <Entidade>Mappings: `public static class` com o método de extensão `ToResponse(this <Entidade> <entidade>)`

Regras dos testes da entidade:
- Create_WithValidData_ShouldCreate<Entidade>: cria com os valores de teste e confere Id diferente de Guid.Empty e cada campo
- Para cada string obrigatória: Create_WithEmpty<Campo>_ShouldThrowDomainException (Theory com "" e "   ") e Create_With<Campo>AboveMaxLength_ShouldThrowDomainException
- Para cada Guid obrigatório: Create_WithEmpty<Campo>_ShouldThrowDomainException com Guid.Empty
- Update_WithValidData_ShouldChange<Entidade>: cria, chama Update com os valores de atualização e confere os campos string

Exemplo completo gerado para `entidade: Report, modulo: reports, campos: Name:string`:

- backend/src/Backend.Domain/Reports/Report.cs:
```csharp
using Backend.Domain.Common;

namespace Backend.Domain.Reports;

public sealed class Report : AggregateRoot
{
    public const int NameMaxLength = 200;

    private Report()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public static Report Create(string name)
    {
        var report = new Report();
        report.SetName(name);
        return report;
    }

    public void Update(string name)
    {
        SetName(name);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O campo Name é obrigatório.");
        }

        if (name.Trim().Length > NameMaxLength)
        {
            throw new DomainException($"O campo Name deve ter no máximo {NameMaxLength} caracteres.");
        }

        Name = name.Trim();
    }
}
```

- backend/src/Backend.Application/Reports/Dtos/CreateReportRequest.cs (UpdateReportRequest.cs é igual, trocando o nome do record):
```csharp
using System.ComponentModel.DataAnnotations;
using Backend.Domain.Reports;

namespace Backend.Application.Reports.Dtos;

public sealed record CreateReportRequest(
    [Required, MaxLength(Report.NameMaxLength)] string Name);
```

- backend/src/Backend.Application/Reports/Dtos/ReportResponse.cs:
```csharp
namespace Backend.Application.Reports.Dtos;

public sealed record ReportResponse(Guid Id, string Name);
```

- backend/src/Backend.Application/Reports/ReportMappings.cs:
```csharp
using Backend.Application.Reports.Dtos;
using Backend.Domain.Reports;

namespace Backend.Application.Reports;

public static class ReportMappings
{
    public static ReportResponse ToResponse(this Report report) => new(report.Id, report.Name);
}
```

- backend/tests/Backend.UnitTests/Reports/ReportTests.cs:
```csharp
using Backend.Domain.Common;
using Backend.Domain.Reports;

namespace Backend.UnitTests.Reports;

public sealed class ReportTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateReport()
    {
        var report = Report.Create("Valor de teste");

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal("Valor de teste", report.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        Assert.Throws<DomainException>(() => Report.Create(name));
    }

    [Fact]
    public void Create_WithNameAboveMaxLength_ShouldThrowDomainException()
    {
        var name = new string('a', Report.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => Report.Create(name));
    }

    [Fact]
    public void Update_WithValidData_ShouldChangeReport()
    {
        var report = Report.Create("Valor de teste");

        report.Update("Valor atualizado");

        Assert.Equal("Valor atualizado", report.Name);
    }
}
```

Exemplo dos métodos Set gerados para os outros tipos (entidade Transaction com `Description:string, Amount:decimal, Notes:string?, OccurredAt:DateTimeOffset`):
```csharp
    private void SetAmount(decimal amount)
    {
        Amount = amount;
    }

    private void SetNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            Notes = null;
            return;
        }

        if (notes.Trim().Length > NotesMaxLength)
        {
            throw new DomainException($"O campo Notes deve ter no máximo {NotesMaxLength} caracteres.");
        }

        Notes = notes.Trim();
    }

    private void SetOccurredAt(DateTimeOffset occurredAt)
    {
        OccurredAt = occurredAt.ToUniversalTime();
    }
```
E o Request correspondente:
```csharp
public sealed record CreateTransactionRequest(
    [Required, MaxLength(Transaction.DescriptionMaxLength)] string Description,
    decimal Amount,
    [MaxLength(Transaction.NotesMaxLength)] string? Notes,
    DateTimeOffset OccurredAt);
```

## Verificação final

Dentro de backend/, abortar com o erro exato se algum falhar:
- dotnet build Backend.slnx (sem erros e sem warnings)
- dotnet test Backend.slnx
- dotnet format Backend.slnx --verify-no-changes

Orientações finais: Eu quero que você crie essa skill dentro da pasta do próprio projeto (.claude/skills/config-dtos-entities), dentro de um arquivo JavaScript que seja responsável por executar cada um dos passos acima de forma determinística: os arquivos são gerados a partir de templates literais dentro da skill, sem usar geradores externos e sem inventar código fora das regras acima. O script deve aceitar os parâmetros entidade, modulo e campos pela linha de comando, para que outras skills (como a config-new-module) possam chamá-lo via node sem duplicar essa lógica. Não criar repository, service, controller, configuração do EF Core nem migration: isso é responsabilidade da skill config-new-module.
