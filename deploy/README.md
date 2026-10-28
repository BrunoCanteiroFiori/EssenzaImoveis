# Deploy em produção

Workflow: [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Roda a cada push na `main` e manualmente em
**Actions > Deploy > Run workflow**.

1. **Validar backend**: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` e migrations em dia com o modelo.
2. **Validar frontend**: `npm ci` e `npm run lint` (o `npm run build` roda dentro da imagem).
3. **Gerar imagens** no GitHub Container Registry, com a tag do commit e `latest`:
   `ghcr.io/<dono>/<repo>-api`, `-migrations` e `-web`.
4. **Publicar no servidor** por SSH: copia `compose.yaml`, `Caddyfile`, `deploy.sh` e `.env.example` para `DEPLOY_PATH`
   e roda `deploy.sh`, que sobe Postgres → migrations → API → site → Caddy e confere se a API e o site respondem.

Só o Caddy fica exposto (portas 80, 443 e 8080). Postgres, API e site ficam na rede interna do Docker. O banco, as fotos
dos imóveis e os certificados ficam em volumes (`postgres-data`, `uploads`, `caddy-data`) e sobrevivem aos deploys.

## 1. Preparar o servidor (uma vez)

Servidor Linux x86_64 (amd64). Se for ARM, adicionar `platforms: linux/arm64` nos passos `docker/build-push-action`.

```bash
# Docker Engine + plugin compose
curl -fsSL https://get.docker.com | sh
# Usuário do SSH pode usar o Docker sem sudo (sair e entrar de novo depois)
sudo usermod -aG docker $USER
# Pasta do deploy
sudo mkdir -p /opt/sitecorretora && sudo chown $USER /opt/sitecorretora
```

Liberar na regra da subnet/firewall da nuvem:
- Só IP: TCP **80** (site) e TCP **8080** (API).
- Com domínio: TCP **80**, TCP e UDP **443**. A 8080 pode ser fechada.

Criar o `.env` de produção a partir do [.env.example](.env.example) (do seu computador, na raiz do repositório):

```bash
scp deploy/.env.example usuario@SEU_IP:/opt/sitecorretora/.env
```

Depois, no servidor, editar `/opt/sitecorretora/.env` e preencher as senhas e o IP. Para gerar os segredos:

```bash
openssl rand -hex 24     # POSTGRES_PASSWORD
openssl rand -base64 48  # Jwt__Secret
```

## 2. Chave SSH do GitHub

Dá para usar a chave que você já tem, mas uma chave só para o deploy é mais segura: se vazar, basta removê-la do
servidor sem perder o seu acesso. No seu computador:

```bash
ssh-keygen -t ed25519 -C "github-deploy-sitecorretora" -f deploy_key -N ""
cat deploy_key.pub | ssh usuario@SEU_IP "cat >> ~/.ssh/authorized_keys"
ssh-keyscan -p 22 SEU_IP
```

## 3. Configurar o GitHub

Em **Settings > Secrets and variables > Actions**.

Secrets:

| Nome | Valor |
| --- | --- |
| `SSH_HOST` | IP do servidor |
| `SSH_PORT` | porta do SSH (opcional, padrão 22) |
| `SSH_USER` | usuário do SSH |
| `SSH_PRIVATE_KEY` | conteúdo do arquivo `deploy_key` (chave privada) |
| `SSH_KNOWN_HOSTS` | saída do `ssh-keyscan` (garante que o GitHub está falando com o seu servidor) |

Variables:

| Nome | Só IP | Com domínio |
| --- | --- | --- |
| `NEXT_PUBLIC_SITE_URL` | `http://SEU_IP` | `https://seudominio.com.br` |
| `NEXT_PUBLIC_API_URL` | `http://SEU_IP:8080` | `https://api.seudominio.com.br` |
| `NEXT_PUBLIC_SITE_NAME` | nome da corretora | |
| `NEXT_PUBLIC_WHATSAPP_NUMBER` | DDI + DDD + número, só dígitos | |
| `DEPLOY_PATH` | opcional, padrão `/opt/sitecorretora` | |

O job de publicação usa o environment `production` (criado sozinho no primeiro deploy). Em **Settings > Environments >
production** dá para exigir aprovação antes de cada deploy.

## Enquanto estiver só no IP

Sem domínio não há certificado, então tudo trafega em HTTP: senha do login e cookies de sessão passam sem criptografia.
Por isso o `.env` usa `Auth__SecureCookies=false` (o navegador recusa cookie `Secure` em HTTP). Evite entrar no admin
por redes públicas e configure o domínio assim que puder.

## Quando tiver domínio

1. No DNS, criar registros **A** apontando para o IP do servidor: `seudominio.com.br`, `www.seudominio.com.br` e
   `api.seudominio.com.br`. A API precisa ser subdomínio do mesmo domínio do site: os cookies de sessão são
   `SameSite=Strict`.
2. Na regra da subnet/firewall, liberar TCP 80 e TCP/UDP 443.
3. No servidor, editar `/opt/sitecorretora/.env`:
   ```
   SITE_ADDRESS=seudominio.com.br, www.seudominio.com.br
   API_ADDRESS=api.seudominio.com.br
   Cors__AllowedOrigins=https://seudominio.com.br,https://www.seudominio.com.br
   Auth__SecureCookies=true
   ```
4. No GitHub, trocar as variables `NEXT_PUBLIC_SITE_URL` e `NEXT_PUBLIC_API_URL` para os endereços `https://`.
5. Rodar **Actions > Deploy > Run workflow**. O site é gerado de novo com os endereços novos e o Caddy emite os
   certificados do Let's Encrypt sozinho (e redireciona HTTP para HTTPS).

Depois de confirmar que o HTTPS funciona, a porta 8080 pode ser fechada.

## Operação no servidor

```bash
cd /opt/sitecorretora
docker compose ps                 # estado dos serviços
docker compose logs -f api        # logs da API (também: web, migrations, caddy, postgres)
docker compose restart api        # reiniciar um serviço
docker compose exec postgres pg_dump -U corretora corretora > backup.sql   # backup do banco
```

Voltar para uma versão anterior: em **Actions**, abrir a execução do commit desejado e usar **Re-run jobs** no job
**Publicar no servidor**. As imagens antigas continuam no GHCR.
