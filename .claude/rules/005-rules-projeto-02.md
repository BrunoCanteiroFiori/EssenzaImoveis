Objetivo: criar as rules do projeto (validação do projeto, SOLID, DDD e nomenclatura) na pasta atual, para que qualquer ferramenta de IA generativa (Codex, Claude Code, Cursor ou outra) siga as mesmas regras em todas as tarefas.

Língua: português do Brasil.

Instruções:
- Criar os arquivos abaixo na pasta atual, com exatamente o conteúdo indicado. Não criar nenhum outro arquivo.
- As rules ficam em .claude/rules, que o Claude Code carrega automaticamente. AGENTS.md é lido pelo Codex, Cursor e outras ferramentas e aponta para as mesmas rules. CLAUDE.md só importa o AGENTS.md, para o Claude Code ter o mesmo resumo.
- Se algum desses arquivos já existir, mostrar a diferença e perguntar antes de sobrescrever.
- As rules descrevem o projeto criado pela skill config-project-fullstack (frontend/ e backend/ independentes). Elas podem ser criadas antes do projeto existir.

## Arquivo: AGENTS.md

~~~markdown
# Regras do projeto

Dois projetos independentes, sem monorepo:
- `frontend/`: Next.js (porta 3000). Rodar com `npm run dev` dentro de `frontend/`.
- `backend/`: .NET 10 / ASP.NET Core em camadas DDD (porta 4000). Rodar com `dotnet run --project src/Backend.Api` dentro de `backend/`.

Antes de escrever ou alterar código, ler e seguir:
- `.claude/rules/arquitetura-ddd.md`
- `.claude/rules/solid.md`
- `.claude/rules/nomenclatura.md`
- `.claude/rules/validacao-projeto.md`

Usar as skills de `.claude/skills` sempre que existir uma para a tarefa (criar projeto, módulo, entidade ou DTO), em vez de criar os arquivos à mão.

Uma tarefa só está concluída quando toda a validação de `.claude/rules/validacao-projeto.md` passa. Se uma regra impedir a tarefa, parar e perguntar; nunca desligar regra, teste ou warning para fazer passar.
~~~

## Arquivo: CLAUDE.md

~~~markdown
@AGENTS.md
~~~

## Arquivo: .claude/rules/arquitetura-ddd.md

~~~markdown
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
~~~

## Arquivo: .claude/rules/solid.md

~~~markdown
# SOLID

## S: Responsabilidade única
- Cada classe tem um motivo para mudar: entidade (regra de negócio), service (caso de uso), repository (persistência), controller (HTTP), mappings (conversão), configuration (mapeamento do banco), infrastructure (recurso técnico reutilizável).
- Um service por agregado. Se um service passar de ~200 linhas ou de 4 dependências no construtor, dividir.
- Não misturar conversão de DTO, regra de negócio e acesso a dados no mesmo método.

## O: Aberto/fechado
- Funcionalidade nova entra como classe nova (módulo, service, repository específico), sem editar as classes base (`Entity`, `AggregateRoot`, `RepositoryBase<T>`, `Result`).
- Services e repositories são registrados por convenção (`AddApplication`/`AddRepository`): um módulo novo não exige editar `DependencyInjection.cs` nem `Program.cs`.
- Novo comportamento por tipo: polimorfismo ou estratégia, não `switch` crescendo a cada caso novo.

## L: Substituição de Liskov
- Implementações respeitam o contrato da interface: mesmo significado de retorno, sem exigir pré-condições extras.
- Proibido `throw new NotImplementedException()` ou métodos vazios para "cumprir" interface.
- Subclasse de entidade não enfraquece invariante da base.

## I: Segregação de interfaces
- Interfaces pequenas e focadas: `IRepository<T>` (CRUD genérico), `I<Entidade>Repository` (consultas do agregado), `IUnitOfWork` (transação).
- Não adicionar ao `IRepository<T>` método que só um agregado usa: colocar no repository específico.
- Cada service expõe sua própria interface `I<Entidade>Service`.

## D: Inversão de dependência
- Application depende de abstrações do Domain (`I<Entidade>Repository`, `IUnitOfWork`), nunca de classes da camada Repository.
- Recursos da Infrastructure são usados pelas interfaces (`IHttpRequestClient`, `IDbConnectionFactory`), nunca pelas classes concretas.
- Dependências entram pelo construtor (primary constructor). Proibido `new` de service, repository ou DbContext fora dos testes.
- Proibido service locator (`IServiceProvider.GetService`) em código de negócio.
- Recursos externos (relógio, e-mail, arquivos, APIs) entram por interface; para data e hora usar `TimeProvider`.
~~~

## Arquivo: .claude/rules/nomenclatura.md

~~~markdown
# Nomenclatura

## Idioma
- Código (tipos, métodos, variáveis, rotas, tabelas, arquivos) em inglês.
- Mensagens para pessoas (exceções de domínio, `Error.Message`, textos da interface, comentários XML) em português do Brasil.

## C#
| Elemento | Padrão | Exemplo |
| --- | --- | --- |
| Namespace | igual ao caminho da pasta, com escopo de arquivo | `namespace Backend.Application.Reports;` |
| Classe, record, enum, struct | PascalCase, substantivo | `Report`, `ReportService` |
| Interface | `I` + PascalCase | `IReportRepository` |
| Método | PascalCase, verbo | `GetByIdAsync` |
| Método assíncrono | sufixo `Async` (exceto actions de controller) | `CreateAsync` |
| Propriedade, constante, campo público | PascalCase | `NameMaxLength` |
| Campo privado | `_camelCase` | `_domainEvents` |
| Parâmetro e variável local | camelCase, nome completo (também em lambdas) | `report => report.ToResponse()` |
| Parâmetro genérico | `T` + PascalCase | `TEntity` |

- Um tipo por arquivo e nome do arquivo igual ao do tipo. Exceção: `Result` e `Result<TValue>` em `Result.cs`.
- Sem abreviações (`rep`, `svc`, `dto`, `tmp`, `obj`), exceto as consagradas (`Id`, `Url`, `Api`).

## Sufixos por papel
| Papel | Padrão | Exemplo |
| --- | --- | --- |
| Entidade (aggregate root) | substantivo no singular, sem sufixo | `Report` |
| Value object | substantivo, sem sufixo | `Email`, `Money` |
| Evento de domínio | `<Entidade><Fato no passado>DomainEvent` | `ReportCreatedDomainEvent` |
| Interface de repository | `I<Entidade>Repository` | `IReportRepository` |
| Repository | `<Entidade>Repository` | `ReportRepository` |
| Interface de service | `I<Entidade>Service` | `IReportService` |
| Service | `<Entidade>Service` | `ReportService` |
| DTO de entrada | `Create<Entidade>Request`, `Update<Entidade>Request` | `CreateReportRequest` |
| DTO de saída | `<Entidade>Response` | `ReportResponse` |
| Mapping | `<Entidade>Mappings` | `ReportMappings` |
| Erros esperados | `<Entidade>Errors` com código `<Entidade>.<Motivo>` | `Report.NotFound` |
| Configuração do EF Core | `<Entidade>Configuration` | `ReportConfiguration` |
| Classe base abstrata | sufixo `Base` | `RepositoryBase<TEntity>`, `ApiControllerBase` |
| Conversões (Infrastructure) | `<Tipo>Conversions`, classe estática com métodos de extensão | `StringConversions.ToSnakeCase()` |
| Cliente técnico (Infrastructure) | `I<Nome>Client` / `<Nome>Client` | `IHttpRequestClient` |
| Fábrica (Infrastructure) | `I<Nome>Factory` / `<Tecnologia><Nome>Factory` | `NpgsqlConnectionFactory` |
| Controller | `<Modulo>Controller` (plural) | `ReportsController` |
| Classe de teste | `<ClasseTestada>Tests` | `ReportServiceTests` |
| Método de teste | `Metodo_Cenario_ResultadoEsperado` | `CreateAsync_WithValidRequest_ShouldPersistReport` |
| Migration | `Add<Modulo>`, `Add<Campo>To<Entidade>` | `AddReports` |

## Módulos
| Item | Padrão | `reports` | `user-profiles` |
| --- | --- | --- | --- |
| Nome do módulo | kebab-case, plural | `reports` | `user-profiles` |
| Pasta nas camadas (Domain, Application, Repository, UnitTests) | PascalCase, plural | `Reports` | `UserProfiles` |
| Entidade | PascalCase, singular | `Report` | `UserProfile` |
| Rota da API | kebab-case, plural | `/reports` | `/user-profiles` |
| Tabela | snake_case, plural | `reports` | `user_profiles` |
| Colunas | snake_case (automático) | `name` | `display_name` |

## Frontend (Next.js)
- Pastas de rota em kebab-case, com o mesmo nome do módulo e da rota da API (`reports`, `user-profiles`).
- Arquivos do módulo no formato `<modulo>.<papel>.tsx` (`reports.page.tsx`, `reports.component.tsx`).
- Componentes React em PascalCase (`ReportsComponent`); hooks com prefixo `use` (`useReports`).
- Variáveis de ambiente expostas ao navegador com prefixo `NEXT_PUBLIC_`.

## Validação automática
- `backend/.editorconfig` + `EnforceCodeStyleInBuild`: nomenclatura errada (IDE1006) ou namespace em bloco (IDE0161) quebram o `dotnet build`.
- `tests/Backend.ArchitectureTests/NamingConventionTests.cs`: interfaces com `I`, controllers, services, DTOs e repositories no lugar e com o nome certo.
~~~

## Arquivo: .claude/rules/validacao-projeto.md

~~~markdown
# Validação do projeto

Rodar antes de concluir qualquer tarefa que altere código. A tarefa só está concluída se todos os passos passarem.

## Backend (dentro de `backend/`)
1. `dotnet tool restore`
2. `dotnet build Backend.slnx`: 0 erros e 0 warnings (warnings viram erro pelo `Directory.Build.props`; nomenclatura validada pelo `.editorconfig`)
3. `dotnet test Backend.slnx`: todos os testes de unidade e de arquitetura passando
4. `dotnet format Backend.slnx --verify-no-changes`: código formatado
5. Se mudou entidade ou configuração do EF Core:
   - criar a migration: `dotnet ef migrations add <Nome> --project src/Backend.Repository --startup-project src/Backend.Api --output-dir Migrations`
   - conferir que o modelo está em dia: `dotnet ef migrations has-pending-model-changes --project src/Backend.Repository --startup-project src/Backend.Api`
6. Se a tarefa envolve banco e o Docker está disponível: `docker compose up -d` e `dotnet ef database update --project src/Backend.Repository --startup-project src/Backend.Api`

## Frontend (dentro de `frontend/`)
1. `npm run lint`
2. `npm run build`

## Checklist da revisão
- Regras de `arquitetura-ddd.md`, `solid.md` e `nomenclatura.md` respeitadas.
- Toda regra de negócio nova tem teste de unidade (entidade e service).
- Nenhum segredo no código: configuração só por `.env`; `.env.example` sem senha real; `.env` fora do git.
- Nenhum arquivo, `using` ou código sem uso.

## Proibido
- Desligar ou ignorar teste, regra de arquitetura ou warning para fazer passar (`#pragma warning disable`, `NoWarn`, `[Fact(Skip = ...)]`, apagar teste), sem aprovação explícita de quem pediu a tarefa.
- Editar migration gerada à mão.
- Dizer que a tarefa está pronta sem ter rodado esta validação; se algum passo não puder rodar (ex.: Docker indisponível), dizer qual e por quê.
~~~
