# Arquitetura e DDD (backend)

## Camadas e dependências

```
Backend.Domain          → não depende de nada
Backend.Infrastructure  → não depende de nenhum projeto da solution (reutilizável)
Backend.Application     → Domain, Infrastructure
Backend.Repository      → Domain, Infrastructure
Backend.Api             → Application, Repository, Infrastructure
```

| Projeto | Responsabilidade | Pode depender de |
| --- | --- | --- |
| Backend.Domain | Entidades, value objects, regras de negócio, eventos de domínio, enums de negócio, interfaces de repository | nada (nem EF Core, nem ASP.NET, nem Npgsql, nem Infrastructure) |
| Backend.Application | Casos de uso (services), DTOs, mappings, erros esperados (`Result`) | Backend.Domain, Backend.Infrastructure |
| Backend.Repository | Somente interação com o banco: `AppDbContext`, `RepositoryBase<T>`, repositories e configurações do EF Core por módulo, migrations | Backend.Domain, Backend.Infrastructure |
| Backend.Infrastructure | Tudo que é reutilizável em qualquer projeto: cliente de conexão com o banco (`NpgsqlDataSource`, `IDbConnectionFactory`), requisições HTTP (`IHttpRequestClient`), enums genéricos, conversões genéricas (texto, enum, JSON) | só pacotes NuGet (sem EF Core e sem ASP.NET) |
| Backend.Api | Controllers, Swagger, composição das camadas (`Program.cs`) | Backend.Application, Backend.Repository, Backend.Infrastructure |

Validado automaticamente por `tests/Backend.ArchitectureTests` (LayerDependencyTests e DomainModelTests).

## Domain

- Toda entidade é raiz de agregado: herda `AggregateRoot`, é `sealed` e fica em `Backend.Domain.<Modulo>`.
- Sem setter público. O estado só muda por métodos com intenção de negócio (`Create`, `Update`, `Approve`, `Cancel`...).
- Criação sempre pelo método estático `Create`; o construtor sem parâmetros é privado e existe só para o EF Core.
- Invariantes são validadas dentro da entidade (métodos `Set<Campo>`) e violadas com `DomainException` com mensagem em pt-BR.
- Proibido modelo anêmico: regra de negócio fica na entidade ou num value object, nunca no service ou no controller.
- Value objects herdam `ValueObject`, são `sealed` e imutáveis.
- Um repository por raiz de agregado. A interface (`I<Entidade>Repository : IRepository<Entidade>`) fica no Domain; a implementação, na camada Repository.
- Um agregado referencia outro apenas pelo Id (`Guid CategoryId`), nunca por propriedade de navegação.
- Eventos de domínio implementam `IDomainEvent`, têm nome no passado (`ReportCreatedDomainEvent`) e são disparados com `RaiseDomainEvent`.
- Ids são `Guid` v7 gerados no domínio (classe `Entity`), nunca pelo banco.
- Datas com hora: `DateTimeOffset` em UTC. Datas sem hora: `DateOnly`. Não usar `DateTime`.

## Application

- Services orquestram o caso de uso: buscam pelo repository, chamam o domínio, persistem com `IUnitOfWork.SaveChangesAsync`. Não contêm regra de negócio.
- Falhas esperadas (não encontrado, validação, conflito) retornam `Result`/`Result<T>` com um `Error` de `<Entidade>Errors`. Exceção só para erro inesperado; `DomainException` é capturada no service e convertida em `Error.Validation`.
- Nunca retornar entidade para fora da Application: sempre DTO `<Entidade>Response`, montado por `<Entidade>Mappings.ToResponse()`.
- DTOs são `sealed record` em `Backend.Application.<Modulo>.Dtos`, com validação de entrada por DataAnnotations.
- Todo método assíncrono recebe e repassa `CancellationToken`.

## Repository

- Só acesso a dados: nenhuma regra de negócio, nenhum DTO, nenhuma chamada HTTP.
- Uma pasta por módulo (`Backend.Repository.<Modulo>`) com o `<Entidade>Repository` e a `<Entidade>Configuration`.
- Mapeamento do EF Core só por `IEntityTypeConfiguration<T>`; tabelas e colunas em snake_case.
- Repositories herdam `RepositoryBase<T>` (pasta `Common`). Consultas específicas vão no repository específico, não no genérico.
- O EF Core usa o `NpgsqlDataSource` registrado pela Infrastructure; a camada Repository não cria conexão própria.
- Migrations só pelo `dotnet ef` (pasta `Migrations`), nunca editadas à mão.

## Infrastructure

- Código genérico e reutilizável: não conhece nenhum módulo, entidade, DTO ou regra de negócio deste projeto. Deve poder ser copiada para outro projeto sem alteração.
- Pastas: `Database` (cliente de conexão), `Http` (requisições HTTP), `Enums` (enums genéricos, com `[Description]` em pt-BR), `Conversions` (métodos de extensão de conversão).
- Enum com significado de negócio (ex.: status de um pedido) fica no Domain, não aqui.
- Chamadas HTTP externas usam `IHttpRequestClient`; nunca criar `new HttpClient()`.
- Connection string só pelo `.env` (`ConnectionStrings__Default`); nunca no código ou no appsettings.

## Api

- Controllers finos: recebem o DTO, chamam o service e convertem o `Result` em HTTP com `ToProblem` (`ApiControllerBase`). Sem regra de negócio.
- Controllers nunca usam `AppDbContext`, EF Core, repositories ou entidades do Domain.
- Erros saem como ProblemDetails: 400 validação, 404 não encontrado, 409 conflito.
- Toda action tem comentário XML `/// <summary>` (aparece no Swagger) e `ProducesResponseType` para cada status possível.
