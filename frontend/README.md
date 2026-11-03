# Frontend

Site público e painel admin da corretora, em Next.js 16 (App Router com Cache Components). Conversa com a API .NET de
`../backend`.

## Rodar

```bash
cp .env.example .env
npm install
npm run dev
```

O site abre em http://localhost:3000. A API precisa estar no ar em `NEXT_PUBLIC_API_URL` (padrão
http://localhost:4000). Antes de concluir uma tarefa, rodar `npm run lint` e `npm run build`
(veja [validacao-projeto.md](../.claude/rules/validacao-projeto.md)).

## Variáveis de ambiente

| Variável | Para quê |
| --- | --- |
| `NEXT_PUBLIC_API_URL` | Endereço da API usado pelo navegador (e pelo servidor, se `API_URL` faltar) |
| `NEXT_PUBLIC_SITE_URL` | Endereço público do site |
| `NEXT_PUBLIC_SITE_NAME` | Nome da corretora no cabeçalho e nos títulos |
| `NEXT_PUBLIC_WHATSAPP_NUMBER` | WhatsApp da corretora: DDI + DDD + número, só dígitos |
| `API_URL` | Opcional, só no servidor: endereço da API visto de dentro do servidor (no Docker, `http://api:4000`) |

As `NEXT_PUBLIC_*` entram no build: mudar uma delas exige gerar o site de novo.

## Cache da área pública

As leituras que o servidor faz para montar a vitrine ficam em
[src/lib/properties.server.ts](src/lib/properties.server.ts), com `"use cache"`:

| Função | Página | Endpoint da API |
| --- | --- | --- |
| `getFeaturedProperties` | Home: Destaques, Compra, Aluguel, Mais buscados e Recém-chegados | `GET /properties/featured` |
| `searchProperties` | Busca (`/properties`) | `GET /properties?...`, uma entrada por combinação de filtros |
| `getPropertyDetails` | Página do imóvel (`/properties/[id]`) | `GET /properties/{id}`, a mesma entrada serve o título da aba e a página |

Como funciona:
- O cache fica na memória do servidor Next e é o mesmo para todos os visitantes.
- Cada resposta vale 1 minuto (`cacheLife({ stale: 60, revalidate: 60, expire: 300 })`). Depois disso, a próxima
  visita busca de novo na API. Por isso, o que o admin altera (preço, status, fotos, imóvel novo) aparece no site
  em até 1 minuto. Salvar no admin não limpa o cache.
- Imóvel que sai da vitrine (vendido, alugado, inativo) passa a dar 404 na API e some do site no mesmo prazo. O
  404 também fica no cache.
- Erro da API (5xx ou API fora do ar) não fica no cache: o visitante vê o aviso de erro e a visita seguinte tenta de
  novo.
- `connection()` antes do cache faz a leitura acontecer só quando alguém pede a página. O build não chama a API (no CI
  ela nem existe), e nenhum imóvel vai para o HTML estático.
- Reiniciar o servidor ou fazer deploy zera o cache.

Fica sem cache, sempre direto do navegador para a API (`src/lib/*.api.ts`): salvos, vistos por último, sugestões,
registro de visualização, localidades da busca e todo o painel admin.

Para mudar:
- **Tempo**: trocar o `cacheLife` em `fetchPublic`. `revalidate` é quanto a resposta vale, e `expire` precisa ser
  maior que `revalidate`.
- **Nova leitura pública com cache**: exportar em `properties.server.ts` usando `getPublic`. Não importar esse arquivo
  em componente `"use client"`, porque `"use cache"` só roda no servidor.
- **Nunca** colocar dentro de `"use cache"` algo que dependa do visitante (cookies, `X-Visitor-Id`, sessão). A
  resposta seria mostrada para todos os outros visitantes.
