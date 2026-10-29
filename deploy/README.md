# Deploy em produção

Workflow: [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Roda a cada push na `main` e manualmente em
**Actions > Deploy > Run workflow**.

1. **Validar backend**: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` e migrations em dia com o modelo.
2. **Validar frontend**: `npm ci` e `npm run lint` (o `npm run build` roda dentro da imagem).
3. **Gerar imagens** no GitHub Container Registry, com a tag do commit e `latest`:
   `ghcr.io/<dono>/<repo>-api`, `-migrations` e `-web`.
4. **Publicar no servidor** por SSH: copia `compose.yaml`, `Caddyfile`, `deploy.sh` e `.env.example` para `DEPLOY_PATH`
   e roda `deploy.sh`, que sobe Postgres → migrations → API → site → Caddy e confere se a API e o site respondem.

Só o Caddy fica exposto (portas 80, 443 e 8080). Postgres, API e site ficam na rede interna do Docker. O banco e os
certificados ficam em volumes (`postgres-data`, `caddy-data`) e sobrevivem aos deploys. As fotos e os vídeos dos imóveis
ficam no Google Drive (veja [Google Drive](#google-drive-fotos-e-vídeos)).

## 1. Preparar o servidor (uma vez)

Servidor Linux ARM (aarch64): as imagens são geradas no runner `ubuntu-24.04-arm` do GitHub. Se o servidor mudar para
x86_64 (confira com `uname -m`), trocar o `runs-on` do job "Gerar imagens" para `ubuntu-latest`.

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

As credenciais do Google Drive não vão no `.env`: ficam nos secrets do GitHub (próxima seção e seção 3).

## Google Drive (fotos e vídeos)

As fotos e os vídeos ficam no Google Drive de uma conta Google pessoal. O backend cria, na raiz do Meu Drive, a pasta
`GoogleDrive__RootFolderName` (padrão `SiteCorretora`) com leitura pública para quem tiver o link. Dentro dela fica a
pasta `properties`, com uma subpasta por imóvel (o nome da subpasta é o id do imóvel). O site não usa links do Drive: a
API lê o arquivo do Drive e o entrega ao navegador, com cache de um ano. Excluir uma foto, um vídeo ou o imóvel manda o
arquivo ou a subpasta para a lixeira do Drive, onde fica por 30 dias.

O acesso usa OAuth com o escopo `drive.file`: o backend só enxerga o que ele mesmo criou, e não o resto do seu Drive.
Para gerar as credenciais (uma vez só):

1. Em [console.cloud.google.com](https://console.cloud.google.com), criar um projeto (ex.: `site-corretora`).
2. **APIs e serviços > Biblioteca**: ativar a **Google Drive API**.
3. **APIs e serviços > Tela de consentimento OAuth** (Google Auth Platform):
   - Tipo de usuário **Externo**; preencher nome do app e e-mails de contato.
   - Em **Acesso a dados**, adicionar o escopo `https://www.googleapis.com/auth/drive.file`.
   - Em **Público**, clicar em **Publicar app** (status **Em produção**). Com só o escopo `drive.file`, o app não
     passa por verificação, mas o Google só deixa publicar com o **Branding** completo: página inicial, política de
     privacidade e termos de serviço num domínio autorizado (IP não vale), além dos e-mails. Não enviar logotipo: com
     logotipo o Google exige verificação.
   - Enquanto o site não tiver domínio, deixar em **Teste** e adicionar a conta Google em **Usuários de teste**. Funciona,
     mas o refresh token **expira em 7 dias**: é preciso gerar outro (passo 5) e atualizar o secret toda semana.
4. **APIs e serviços > Credenciais > Criar credenciais > ID do cliente OAuth**:
   - Tipo **Aplicativo da Web**.
   - Em **URIs de redirecionamento autorizados**, adicionar `https://developers.google.com/oauthplayground`.
   - Guardar o **ID do cliente** e a **Chave secreta do cliente**.
5. Gerar o refresh token no [OAuth 2.0 Playground](https://developers.google.com/oauthplayground):
   - Engrenagem (canto superior direito): marcar **Use your own OAuth credentials** e colar o ID e a chave secreta.
   - Em **Step 1**, digitar `https://www.googleapis.com/auth/drive.file` no campo de escopo e clicar em **Authorize APIs**.
   - Entrar com a conta Google que vai guardar as fotos e permitir o acesso (a tela avisa que o app não foi verificado:
     **Avançado > Acessar**).
   - Em **Step 2**, clicar em **Exchange authorization code for tokens** e copiar o **Refresh token**.
6. Produção: cadastrar os três valores como secrets do environment `production` no GitHub (seção 3):
   `GOOGLEDRIVE__CLIENTID`, `GOOGLEDRIVE__CLIENTSECRET` e `GOOGLEDRIVE__REFRESHTOKEN`. A cada deploy, o workflow grava
   esses valores (com `Storage__Provider=GoogleDrive`) em `google-drive.env` no servidor, legível só pelo dono, e a API lê
   esse arquivo depois do `.env`. Trocou um secret? Rode **Actions > Deploy > Run workflow**.
7. Desenvolvimento: colocar os mesmos valores em `backend/.env`:
   ```
   Storage__Provider=GoogleDrive
   GoogleDrive__ClientId=...
   GoogleDrive__ClientSecret=...
   GoogleDrive__RefreshToken=...
   ```

Para desenvolver sem conta Google, usar `Storage__Provider=Local` em `backend/.env`: os arquivos ficam em
`src/Backend.Api/uploads`.

Limites: até 30 fotos e vídeos por imóvel, dos quais até 5 vídeos. Fotos JPG, PNG ou WebP de até 10 MB; vídeos MP4 ou
WebM de até 200 MB (o MOV do iPhone precisa ser exportado em MP4). A conta pessoal tem 15 GB grátis, divididos com o
Gmail.

As fotos antigas não são migradas: continuam no volume `sitecorretora_uploads` do servidor, que não é mais usado. Depois
de reenviar pelo admin as que forem reais, o volume pode ser apagado com `docker volume rm sitecorretora_uploads`.

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

Secrets do environment `production` (**Settings > Environments > production > Environment secrets**):

| Nome | Valor |
| --- | --- |
| `GOOGLEDRIVE__CLIENTID` | ID do cliente OAuth (seção Google Drive, passo 4) |
| `GOOGLEDRIVE__CLIENTSECRET` | chave secreta do cliente OAuth |
| `GOOGLEDRIVE__REFRESHTOKEN` | refresh token gerado no OAuth Playground (passo 5) |

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
