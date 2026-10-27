@.claude/skills/config-new-module/SKILL.md

Destino: backend/src/Backend.Api/Controllers/<Modulo>Controller.cs (ex.: reports → ReportsController.cs, user-profiles → UserProfilesController.cs). Eu quero que você extenda a capacidade dessa skill que foi referenciada pelo arquivo skill.md.

Ela deve continuar fazendo exatamente tudo que ela está fazendo, que é criar o módulo de negócio nas camadas Domain, Application e Repository do backend, com os testes, de uma forma determinística.

E eu quero que você expanda a capacidade dessa skill para expor o módulo na API do backend ASP.NET Core (.NET 10), criando um controller com os endpoints de CRUD que chamam o service do módulo (I<Entidade>Service). O controller não acessa repository, DbContext nem entidade do Domain: só recebe DTO, chama o service e converte o Result em resposta HTTP.

Os arquivos devem ser criados de forma determinística a partir de templates literais dentro da skill (não usar geradores como dotnet new apicontroller). Usando o módulo reports como exemplo:

- Criar o arquivo backend/src/Backend.Api/Controllers/ApiControllerBase.cs somente se ele ainda não existir (é compartilhado por todos os controllers e converte um Error em ProblemDetails):
```csharp
using Backend.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Base dos controllers da API. Converte um <see cref="Error"/> da camada de aplicação em ProblemDetails.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
```

- Criar o arquivo backend/src/Backend.Api/Controllers/ReportsController.cs. A rota é o nome do módulo em kebab-case (para o frontend chamar `${NEXT_PUBLIC_API_URL}/reports`). As actions não usam sufixo Async, porque o MVC remove esse sufixo e quebraria o CreatedAtAction. Os comentários XML aparecem no Swagger:
```csharp
using Backend.Application.Reports;
using Backend.Application.Reports.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Endpoints do módulo reports.
/// </summary>
[Route("reports")]
public sealed class ReportsController(IReportService reportService) : ApiControllerBase
{
    /// <summary>
    /// Lista todos os registros do módulo reports.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReportResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReportResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var reports = await reportService.GetAllAsync(cancellationToken);
        return Ok(reports);
    }

    /// <summary>
    /// Busca um registro do módulo reports pelo id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await reportService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Cria um registro no módulo reports.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportResponse>> Create(CreateReportRequest request, CancellationToken cancellationToken)
    {
        var result = await reportService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ToProblem(result.Error);
    }

    /// <summary>
    /// Atualiza um registro do módulo reports.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportResponse>> Update(Guid id, UpdateReportRequest request, CancellationToken cancellationToken)
    {
        var result = await reportService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>
    /// Remove um registro do módulo reports.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await reportService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToProblem(result.Error);
    }
}
```

- Não editar Program.cs: o controller é descoberto pelo `app.MapControllers()` e o service já é registrado por convenção.

- Verificações de segurança:
	- Se o arquivo do controller já existir, não sobrescrever
	- No final, executar dentro de backend/: `dotnet build Backend.slnx` (sem warnings) e `dotnet test Backend.slnx` (inclui os testes de arquitetura que proíbem o controller de depender do Domain, da camada Repository e do EF Core)
	- Subir a API e conferir que GET http://localhost:4000/openapi/v1.json contém os caminhos /reports e /reports/{id}; encerrar o processo. Não chamar os endpoints de CRUD na verificação, porque eles precisam do PostgreSQL rodando

Não precisa criar testes para o controller, apenas o controller dentro da minha aplicação ASP.NET Core.

Não mudar absolutamente nada do que já foi feito, simplesmente criar uma nova capacidade: expor o módulo na API do backend do meu projeto.
