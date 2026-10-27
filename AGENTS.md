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
