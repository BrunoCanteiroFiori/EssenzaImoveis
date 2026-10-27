@.claude/skills/config-new-module

Alterar a skill atual para criar o módulo dentro do projeto frontend (pasta frontend/, projeto Next.js independente). Não alterar a funcionalidade que está correta, que é a criação do módulo no backend (camadas Domain, Application, Repository, controller na Api e testes).

No projeto frontend criar a seguinte estrutura:

- Rota Privada do Módulo: @page.tsx
- Página Principal do Módulo: @reports.page.tsx
- Componente Principal do Módulo: @reports.component.tsx

O exemplo acima foi feito como modelo usando o módulo de reports, mas os caminhos devem seguir o nome do módulo criado (em kebab-case, igual à rota da API).

O frontend não tem mais pacote compartilhado com o backend (não existe pasta modules/ nem dependência @namespace/<modulo>). Os dados do módulo vêm da API do backend, em `${process.env.NEXT_PUBLIC_API_URL}/<modulo>`, e os campos exibidos são os do <Entidade>Response gerado no backend.

No final, executar dentro de frontend/: `npm run lint` e `npm run build`.
