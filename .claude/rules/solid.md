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
