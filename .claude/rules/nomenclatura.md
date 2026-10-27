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
