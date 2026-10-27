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
