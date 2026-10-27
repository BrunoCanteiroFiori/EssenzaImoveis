@Skill Creator

# Configuração do Projeto

Stack (dois projetos independentes, sem monorepo e sem Turborepo):
- frontend/: Next.js (porta 3000). Roda sozinho com `npm run dev` dentro de frontend/
- backend/: solution .NET 10 (versão estável mais recente, LTS, C# 14) com ASP.NET Core Web API (porta 4000). Roda sozinho com `dotnet run --project src/Backend.Api` dentro de backend/

Estrutura criada por esta skill:
```
<pasta atual>/
  frontend/                  (Next.js)
  backend/
    Backend.slnx
    Directory.Build.props
    .gitignore
    .env.example
    .env
    src/
      Backend.Api/           (ASP.NET Core Web API)
```

Pré-requisitos (verificar antes de qualquer passo; se algum falhar, abortar com mensagem clara sem alterar nada):
- Node.js e npm instalados
- `dotnet --list-sdks` com um SDK de versão major 10 ou superior (se não houver, pedir para instalar o SDK do .NET 10: https://dotnet.microsoft.com/download)
- As pastas frontend/ e backend/ não podem existir na pasta atual (se existirem, o projeto já foi criado: abortar)

Passo a passo (na pasta atual):
- npx create-next-app@latest frontend --yes --src-dir --use-npm --disable-git
- Criar a pasta backend e entrar nela
- dotnet new sln -n Backend
- dotnet new gitignore
- dotnet new webapi -n Backend.Api -o src/Backend.Api --framework net10.0 --use-controllers --no-https
- Remover os arquivos de exemplo do template: src/Backend.Api/WeatherForecast.cs, src/Backend.Api/Controllers/WeatherForecastController.cs e src/Backend.Api/Backend.Api.http
- dotnet sln Backend.slnx add src/Backend.Api/Backend.Api.csproj
- dotnet add src/Backend.Api package DotNetEnv

- Criar o arquivo (backend/Directory.Build.props). Warnings do compilador viram erro; alertas de vulnerabilidade de pacote (NU190x) continuam só como aviso:
```xml
<Project>

  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

</Project>
```

- Substituir o arquivo (backend/src/Backend.Api/Program.cs):
	- Carrega variáveis de ambiente do arquivo .env (procura a partir da pasta atual subindo até achar backend/.env)
	- Porta lida da variável PORT (padrão 4000)
	- CORS habilitado
	- Health check em /health
```csharp
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"] ?? "4000";
builder.WebHost.UseUrls($"http://localhost:{port}");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

- Substituir o arquivo (backend/src/Backend.Api/Properties/launchSettings.json):
```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:4000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- Criar arquivo frontend/.env.example com o conteúdo:

```env
NEXT_PUBLIC_API_URL=http://localhost:4000
```

- Copiar arquivo frontend/.env.example para frontend/.env

- O .gitignore do Next.js ignora `.env*` (inclusive o .env.example). Adicionar a linha `!.env.example` no final de frontend/.gitignore, se ainda não existir

- Criar arquivo backend/.env.example com o conteúdo:

```env
PORT=4000
```

- Copiar arquivo backend/.env.example para backend/.env

- Adicionar a linha `.env` no final de backend/.gitignore, se ainda não existir

- Verificação final (abortar com o erro exato se algum passo falhar):
	- dotnet build backend/Backend.slnx (sem erros e sem warnings)
	- Subir o backend (`dotnet run --project src/Backend.Api` dentro de backend/), conferir que GET http://localhost:4000/health retorna 200 e encerrar o processo
	- Subir o frontend (`npm run dev` dentro de frontend/), conferir que GET http://localhost:3000 retorna 200 e encerrar o processo

Informações Finais:
- Nome da SKill: config-project-fullstack
- Pasta: .claude/skills
- Orientações finais: Eu quero que você crie uma nova skill baseado em toda a estrutura que eu coloquei acima, que foi o passo a passo usado para configurar o projeto do zero.

Eu quero que você crie essa skill dentro da pasta do próprio projeto e não em outro lugar.

Então pode criar a pasta .claude/skills e colocar a skill lá dentro.

Eu quero que essa skill ela seja determinística.

Eu quero que você crie a skill dentro de um arquivo JavaScript que seja responsável por executar cada um dos passos acima e garantir que o projeto esteja configurado no final com dois projetos independentes: o frontend Next.js executando na porta 3000 e o backend C# .NET 10 (ASP.NET Core) executando na porta 4000, e o backend sendo capaz de ler variáveis de ambiente do arquivo .env conforme foi feito aí o passo a passo em cima. Não usar Turborepo, não criar package.json na raiz e não criar workspaces: cada projeto tem as suas próprias dependências e é executado separadamente. Seguir uma estrutura determinística, não divagar e fazer todas as verificações de segurança para garantir que ao executar esse comando dentro dessa pasta, o projeto seja criado do absoluto zero, incluindo a verificação do SDK do .NET antes de começar. Se eu passar no prompt do comando o namespace do projeto, eu quero que no final de executar todo esse script, você altere o "name" do arquivo frontend/package.json para o namespace informado (ex.: @meu-projeto/frontend). No backend .NET não renomear a solution, os projetos (Backend.*) nem os namespaces do código C#. Então se eu colocar um arroba, alguma coisa lá de namespace, você faça esse processo no final de uma forma que não atrapalhe o passo a passo anterior. Então seguir exatamente o passo a passo para criar uma nova skill com o nome que foi mencionado acima.
