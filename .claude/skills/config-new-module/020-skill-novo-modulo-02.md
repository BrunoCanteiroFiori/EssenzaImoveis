Nome da Skill: config-new-module
Língua: Escrever em português do Brasil
Pasta: Dentro do projeto (.claude/skills)

Objetivo: criar um novo módulo de negócio no backend .NET (camadas Domain, Application e Repository + testes; a Infrastructure é reutilizável e não recebe nada de módulo), de forma determinística, a partir do nome do módulo. Não existe mais pasta modules/ nem pacote TypeScript compartilhado: o projeto não usa monorepo.

Entradas:
- modulo (obrigatório): kebab-case no plural (ex.: reports, transactions, user-profiles)
- entidade (opcional): PascalCase no singular. Padrão: singular do módulo em PascalCase. Regra do singular, aplicada na última palavra: termina com "ies" → troca por "y"; termina com "s" → remove o "s"; senão mantém (reports → Report, categories → Category, user-profiles → UserProfile)
- campos (opcional): mesmo formato e tipos da skill config-dtos-entities. Padrão: `Name:string`
- namespace: não é mais usado por esta skill; se for informado, ignorar sem erro

Derivações:
| Item | reports | user-profiles |
| --- | --- | --- |
| Pasta do módulo (<Modulo>) | Reports | UserProfiles |
| Entidade (<Entidade>) | Report | UserProfile |
| Tabela | reports | user_profiles |
| Migration | AddReports | AddUserProfiles |

Pré-condições (abortar sem alterar nada se falhar):
- Existe backend/Backend.slnx criado pela skill config-project-fullstack
- A pasta backend/src/Backend.Domain/<Modulo>/ não existe (se existir, o módulo já foi criado)
- A skill config-dtos-entities existe em .claude/skills

Passo a passo:
1. Executar o script da skill config-dtos-entities com entidade, modulo e campos (ler o SKILL.md dela para saber o comando). Ela cria as classes raiz que faltarem, a entidade, os DTOs, o mapping e os testes da entidade. Não duplicar essa lógica aqui.
2. Criar a interface do repository no Domain
3. Criar o repository e a configuração do EF Core na camada Repository, na pasta do módulo
4. Criar os erros, a interface do service e o service na Application
5. Criar os testes do service
6. Não editar DependencyInjection.cs nem Program.cs: service e repository são registrados por convenção pelo nome (I<Entidade>Service/<Entidade>Service e I<Entidade>Repository/<Entidade>Repository)
7. Dentro de backend/: `dotnet tool restore` e `dotnet ef migrations add Add<Modulo> --project src/Backend.Repository --startup-project src/Backend.Api --output-dir Migrations`
8. Dentro de backend/: `dotnet build Backend.slnx` (sem warnings), `dotnet test Backend.slnx` e `dotnet format Backend.slnx --verify-no-changes`
9. No resumo final, informar como aplicar a migration no banco (não executar automaticamente): `docker compose up -d` e `dotnet ef database update --project src/Backend.Repository --startup-project src/Backend.Api`

Regras da configuração do EF Core por tipo de campo:
| Tipo | Configuração |
| --- | --- |
| string | `.IsRequired().HasMaxLength(<Entidade>.<Campo>MaxLength)` |
| string? | `.HasMaxLength(<Entidade>.<Campo>MaxLength)` |
| decimal e decimal? | `.HasPrecision(18, 2)` |
| demais tipos | nenhuma linha (convenção do EF Core) |

Templates (exemplo para `modulo: reports`, entidade Report, `campos: Name:string`). Para outros campos, trocar só as listas de parâmetros (`Report.Create(request.Name)` vira `Entidade.Create(request.Campo1, request.Campo2, ...)` na ordem dos campos) e as linhas de configuração do EF Core:

- backend/src/Backend.Domain/Reports/IReportRepository.cs:
```csharp
using Backend.Domain.Abstractions;

namespace Backend.Domain.Reports;

public interface IReportRepository : IRepository<Report>;
```

- backend/src/Backend.Repository/Reports/ReportRepository.cs:
```csharp
using Backend.Domain.Reports;
using Backend.Repository.Common;

namespace Backend.Repository.Reports;

public sealed class ReportRepository(AppDbContext dbContext) : RepositoryBase<Report>(dbContext), IReportRepository;
```

- backend/src/Backend.Repository/Reports/ReportConfiguration.cs:
```csharp
using Backend.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Repository.Reports;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Id).ValueGeneratedNever();
        builder.Property(report => report.Name).IsRequired().HasMaxLength(Report.NameMaxLength);
        builder.Ignore(report => report.DomainEvents);
    }
}
```

- backend/src/Backend.Application/Reports/ReportErrors.cs:
```csharp
using Backend.Application.Common;

namespace Backend.Application.Reports;

public static class ReportErrors
{
    public static Error NotFound(Guid id) => Error.NotFound("Report.NotFound", $"Report com id '{id}' não foi encontrado.");

    public static Error Invalid(string message) => Error.Validation("Report.Invalid", message);
}
```

- backend/src/Backend.Application/Reports/IReportService.cs:
```csharp
using Backend.Application.Common;
using Backend.Application.Reports.Dtos;

namespace Backend.Application.Reports;

public interface IReportService
{
    Task<IReadOnlyList<ReportResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<ReportResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ReportResponse>> CreateAsync(CreateReportRequest request, CancellationToken cancellationToken = default);

    Task<Result<ReportResponse>> UpdateAsync(Guid id, UpdateReportRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

- backend/src/Backend.Application/Reports/ReportService.cs:
```csharp
using Backend.Application.Common;
using Backend.Application.Reports.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Common;
using Backend.Domain.Reports;

namespace Backend.Application.Reports;

public sealed class ReportService(IReportRepository reportRepository, IUnitOfWork unitOfWork) : IReportService
{
    public async Task<IReadOnlyList<ReportResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reports = await reportRepository.GetAllAsync(cancellationToken);
        return [.. reports.Select(report => report.ToResponse())];
    }

    public async Task<Result<ReportResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await reportRepository.GetByIdAsync(id, cancellationToken);
        return report is null ? ReportErrors.NotFound(id) : report.ToResponse();
    }

    public async Task<Result<ReportResponse>> CreateAsync(CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        Report report;
        try
        {
            report = Report.Create(request.Name);
        }
        catch (DomainException exception)
        {
            return ReportErrors.Invalid(exception.Message);
        }

        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return report.ToResponse();
    }

    public async Task<Result<ReportResponse>> UpdateAsync(Guid id, UpdateReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await reportRepository.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return ReportErrors.NotFound(id);
        }

        try
        {
            report.Update(request.Name);
        }
        catch (DomainException exception)
        {
            return ReportErrors.Invalid(exception.Message);
        }

        reportRepository.Update(report);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return report.ToResponse();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await reportRepository.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return ReportErrors.NotFound(id);
        }

        reportRepository.Remove(report);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

- backend/tests/Backend.UnitTests/Reports/ReportServiceTests.cs (valores de teste por tipo iguais aos da skill config-dtos-entities; o teste CreateAsync_WithInvalidRequest só é gerado se a entidade tiver pelo menos uma string obrigatória, usando string.Empty nela):
```csharp
using Backend.Application.Common;
using Backend.Application.Reports;
using Backend.Application.Reports.Dtos;
using Backend.Domain.Abstractions;
using Backend.Domain.Reports;
using NSubstitute;

namespace Backend.UnitTests.Reports;

public sealed class ReportServiceTests
{
    private readonly IReportRepository _reportRepository = Substitute.For<IReportRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _reportService = new ReportService(_reportRepository, _unitOfWork);
    }

    [Fact]
    public async Task GetAllAsync_WhenReportsExist_ShouldReturnAllReports()
    {
        var report = Report.Create("Valor de teste");
        _reportRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([report]);

        var reports = await _reportService.GetAllAsync();

        var response = Assert.Single(reports);
        Assert.Equal(report.Id, response.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenReportDoesNotExist_ShouldReturnNotFound()
    {
        var id = Guid.CreateVersion7();
        _reportRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Report?)null);

        var result = await _reportService.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldPersistReport()
    {
        var request = new CreateReportRequest("Valor de teste");

        var result = await _reportService.CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _reportRepository.Received(1).AddAsync(Arg.Any<Report>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ShouldReturnValidationError()
    {
        var request = new CreateReportRequest(string.Empty);

        var result = await _reportService.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenReportExists_ShouldUpdateReport()
    {
        var report = Report.Create("Valor de teste");
        _reportRepository.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await _reportService.UpdateAsync(report.Id, new UpdateReportRequest("Valor atualizado"));

        Assert.True(result.IsSuccess);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenReportExists_ShouldRemoveReport()
    {
        var report = Report.Create("Valor de teste");
        _reportRepository.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);

        var result = await _reportService.DeleteAsync(report.Id);

        Assert.True(result.IsSuccess);
        _reportRepository.Received(1).Remove(report);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Orientações finais: criar a skill dentro da pasta do próprio projeto (.claude/skills/config-new-module), dentro de um arquivo JavaScript que execute cada um dos passos acima de forma determinística, gerando os arquivos a partir de templates literais dentro da skill (sem geradores externos). Seguir as rules de .claude/rules (arquitetura DDD, SOLID, nomenclatura e validação).
