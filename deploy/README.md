# Deploy em produção

Workflow: [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Roda a cada push na `main` e manualmente em
**Actions > Deploy > Run workflow**.

1. **Validar backend**: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` e migrations em dia com o modelo.
2. **Validar frontend**: `npm ci` e `npm run lint` (o `npm run build` roda dentro da imagem).
3. **Gerar imagens** no GitHub Container Registry, com a tag do commit e `latest`:
   `ghcr.io/<dono>/<repo>-api`, `-migrations` e `-web` (com o repositório `EssenzaImoveis`: `essenzaimoveis-*`).
4. **Publicar no servidor** por SSH:
   - copia `compose.yaml`, `Caddyfile`, `deploy.sh` e `migrate-from-sitecorretora.sh` para `DEPLOY_PATH`;
   - gera o `.env` a partir do environment `production` do GitHub (seção 3), conferindo antes se não falta nada;
   - roda `deploy.sh`, que sobe Postgres → migrations → API → site → Caddy e confere se a API e o site respondem.

Toda a configuração de produção fica no GitHub: o `.env` do servidor é sobrescrito a cada deploy (o anterior fica em
`.env.anterior`). Para mudar um valor, edite o secret ou a variable e rode **Actions > Deploy > Run workflow**.

Só o Caddy fica exposto (portas 80, 443 e 8080). Postgres, API e site ficam na rede interna do Docker. O banco e os
certificados ficam em volumes (`essenzaimoveis_postgres-data`, `essenzaimoveis_caddy-data`) e sobrevivem aos deploys.
As fotos e os vídeos dos imóveis ficam no Google Drive (veja [Google Drive](#google-drive-fotos-e-vídeos)).

## 1. Preparar o servidor (uma vez)

Servidor Linux ARM (aarch64): as imagens são geradas no runner `ubuntu-24.04-arm` do GitHub. Se o servidor mudar para
x86_64 (confira com `uname -m`), trocar o `runs-on` do job "Gerar imagens" para `ubuntu-latest`.

```bash
# Docker Engine + plugin compose
curl -fsSL https://get.docker.com | sh
# Usuário do SSH pode usar o Docker sem sudo (sair e entrar de novo depois)
sudo usermod -aG docker $USER
# Pasta do deploy
sudo mkdir -p /opt/essenzaimoveis && sudo chown $USER /opt/essenzaimoveis
```

Liberar na regra da subnet/firewall da nuvem:
- Só IP: TCP **80** (site) e TCP **8080** (API).
- Com domínio: TCP **80**, TCP e UDP **443**. A 8080 pode ser fechada.

## Google Drive (fotos e vídeos)

As fotos e os vídeos ficam no Google Drive de uma conta Google pessoal. O backend cria, na raiz do Meu Drive, a pasta
`GoogleDrive__RootFolderName` (padrão `EssenzaImoveis`) com leitura pública para quem tiver o link. Dentro dela fica a
pasta `properties`, com uma subpasta por imóvel (o nome da subpasta é o id do imóvel). O site não usa links do Drive: a
API lê o arquivo do Drive e o entrega ao navegador, com cache de um ano. Excluir uma foto, um vídeo ou o imóvel manda o
arquivo ou a subpasta para a lixeira do Drive, onde fica por 30 dias.

O acesso usa OAuth com o escopo `drive.file`: o backend só enxerga o que ele mesmo criou, e não o resto do seu Drive.
Para gerar as credenciais (uma vez só):

1. Em [console.cloud.google.com](https://console.cloud.google.com), criar um projeto (ex.: `essenza-imoveis`).
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
6. Produção: cadastrar os três valores como secrets do environment `production` (seção 3).
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

## 2. Chave SSH do GitHub

Dá para usar a chave que você já tem, mas uma chave só para o deploy é mais segura: se vazar, basta removê-la do
servidor sem perder o seu acesso. No seu computador:

```bash
ssh-keygen -t ed25519 -C "github-deploy-essenzaimoveis" -f deploy_key -N ""
cat deploy_key.pub | ssh usuario@SEU_IP "cat >> ~/.ssh/authorized_keys"
ssh-keyscan -p 22 SEU_IP
```

## 3. Configurar o GitHub

O GitHub guarda os nomes em maiúsculas; o .NET lê as configurações sem diferenciar maiúsculas de minúsculas.

**Repository secrets** (**Settings > Secrets and variables > Actions > Secrets**):

| Nome | Valor |
| --- | --- |
| `SSH_HOST` | IP do servidor |
| `SSH_PORT` | porta do SSH (opcional, padrão 22) |
| `SSH_USER` | usuário do SSH |
| `SSH_PRIVATE_KEY` | conteúdo do arquivo `deploy_key` (chave privada) |
| `SSH_KNOWN_HOSTS` | saída do `ssh-keyscan` (garante que o GitHub está falando com o seu servidor) |

**Repository variables** (**Settings > Secrets and variables > Actions > Variables**). O job que gera as imagens não
usa environment, então estas precisam ficar no repositório:

| Nome | Só IP | Com domínio |
| --- | --- | --- |
| `NEXT_PUBLIC_SITE_URL` | `http://SEU_IP` | `https://seudominio.com.br` |
| `NEXT_PUBLIC_API_URL` | `http://SEU_IP:8080` | `https://api.seudominio.com.br` |
| `NEXT_PUBLIC_SITE_NAME` | nome da corretora | |
| `NEXT_PUBLIC_WHATSAPP_NUMBER` | DDI + DDD + número, só dígitos | |
| `DEPLOY_PATH` | opcional, padrão `/opt/essenzaimoveis` | |

**Environment secrets** (**Settings > Environments > production > Environment secrets**). Nenhum valor pode ter aspas
simples nem quebra de linha:

| Nome | Valor |
| --- | --- |
| `POSTGRES_PASSWORD` | senha do banco, só letras e números (`openssl rand -hex 24`). Depois que o banco existe, não muda: o Postgres só a usa ao criar o volume |
| `JWT__SECRET` | pelo menos 32 bytes (`openssl rand -base64 48`). Trocar desloga todo mundo |
| `SUPERADMIN__EMAIL` | e-mail da conta fixa de super admin, criada ao subir a API se ainda não existir |
| `SUPERADMIN__PASSWORD` | 10 a 128 caracteres, com letras e números. Só vale na criação: depois, troque a senha pelo admin |
| `GOOGLEDRIVE__CLIENTID` | ID do cliente OAuth (seção Google Drive, passo 4) |
| `GOOGLEDRIVE__CLIENTSECRET` | chave secreta do cliente OAuth |
| `GOOGLEDRIVE__REFRESHTOKEN` | refresh token gerado no OAuth Playground (passo 5) |

**Environment variables** (**Settings > Environments > production > Environment variables**):

| Nome | Só IP | Com domínio | Padrão se não definir |
| --- | --- | --- | --- |
| `SITE_ADDRESS` | `:80` | `seudominio.com.br, www.seudominio.com.br` | obrigatória |
| `API_ADDRESS` | `:8080` | `api.seudominio.com.br` | obrigatória |
| `AUTH__SECURECOOKIES` | `false` | `true` | obrigatória |
| `CORS__ALLOWEDORIGINS` | `http://SEU_IP` | `https://seudominio.com.br,https://www.seudominio.com.br` | `NEXT_PUBLIC_SITE_URL` |
| `POSTGRES_USER` | | | `essenzaimoveis` |
| `POSTGRES_DB` | | | `essenzaimoveis` |
| `JWT__ISSUER` | | | `corretora-api` |
| `JWT__AUDIENCE` | | | `corretora-web` |
| `JWT__ACCESSTOKENMINUTES` | | | `15` |
| `SUPERADMIN__NAME` | | | `Super Admin` |
| `GOOGLEDRIVE__ROOTFOLDERNAME` | | | `EssenzaImoveis` |

Em **Settings > Environments > production** também dá para exigir aprovação antes de cada deploy e restringir o deploy
à branch `main`.

## Mudança de sitecorretora para essenzaimoveis (uma vez)

O projeto se chamava `sitecorretora`, com banco e usuário `corretora`. O primeiro deploy com o nome novo migra sozinho
(script `migrate-from-sitecorretora.sh`): para os containers antigos, copia o volume do banco para
`essenzaimoveis_postgres-data` e renomeia o banco e o usuário para `POSTGRES_DB` e `POSTGRES_USER`, mantendo a senha. Os
volumes antigos ficam intactos como backup. Antes do merge que traz o nome novo:

1. No servidor, ver os valores atuais (a senha do banco tem que ser copiada **igual**):
   ```bash
   cat /opt/sitecorretora/.env
   ```
2. Cadastrar no GitHub os secrets e variables da seção 3 com esses valores.
3. Renomear o repositório no GitHub para `EssenzaImoveis` (**Settings > General > Repository name**). Os links antigos
   redirecionam; as imagens passam a se chamar `essenzaimoveis-*`.
4. Se a variable `DEPLOY_PATH` existir com `/opt/sitecorretora`, apagar ou trocar para `/opt/essenzaimoveis`.
5. No servidor, mover a pasta do deploy:
   ```bash
   sudo mv /opt/sitecorretora /opt/essenzaimoveis && sudo chown -R "$USER" /opt/essenzaimoveis
   ```
6. Fazer o merge. Depois de conferir que o site, o login e os imóveis estão certos, apagar o que sobrou do nome antigo:
   ```bash
   docker volume rm sitecorretora_postgres-data sitecorretora_caddy-data sitecorretora_caddy-config sitecorretora_uploads
   rm /opt/essenzaimoveis/.env.anterior
   ```
   O volume `sitecorretora_uploads` tem as fotos antigas, de antes do Google Drive (não migradas): reenvie pelo admin as
   que forem reais antes de apagar. Os pacotes `sitecorretora-*` do GHCR podem ser apagados em **Packages** no GitHub.

## Enquanto estiver só no IP

Sem domínio não há certificado, então tudo trafega em HTTP: senha do login e cookies de sessão passam sem criptografia.
Por isso `AUTH__SECURECOOKIES` fica `false` (o navegador recusa cookie `Secure` em HTTP). Evite entrar no admin por redes
públicas e configure o domínio assim que puder.

## Quando tiver domínio

1. No DNS, criar registros **A** apontando para o IP do servidor: `seudominio.com.br`, `www.seudominio.com.br` e
   `api.seudominio.com.br`. A API precisa ser subdomínio do mesmo domínio do site: os cookies de sessão são
   `SameSite=Strict`.
2. Na regra da subnet/firewall, liberar TCP 80 e TCP/UDP 443.
3. No environment `production`, trocar as variables `SITE_ADDRESS`, `API_ADDRESS`, `CORS__ALLOWEDORIGINS` e
   `AUTH__SECURECOOKIES` para os valores da coluna "Com domínio" (seção 3).
4. Nas repository variables, trocar `NEXT_PUBLIC_SITE_URL` e `NEXT_PUBLIC_API_URL` para os endereços `https://`.
5. Rodar **Actions > Deploy > Run workflow**. O site é gerado de novo com os endereços novos e o Caddy emite os
   certificados do Let's Encrypt sozinho (e redireciona HTTP para HTTPS).

Depois de confirmar que o HTTPS funciona, a porta 8080 pode ser fechada.

## Operação no servidor

```bash
cd /opt/essenzaimoveis
docker compose ps                 # estado dos serviços
docker compose logs -f api        # logs da API (também: web, migrations, caddy, postgres)
docker compose restart api        # reiniciar um serviço
docker compose exec postgres pg_dump -U essenzaimoveis essenzaimoveis > backup.sql   # backup do banco
```

Voltar para uma versão anterior: em **Actions**, abrir a execução do commit desejado e usar **Re-run jobs** no job
**Publicar no servidor**. As imagens antigas continuam no GHCR.
