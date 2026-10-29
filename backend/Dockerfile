# syntax=docker/dockerfile:1

# Imagens de produção do backend. Targets:
#   api        -> API ASP.NET Core
#   migrations -> bundle do EF Core que aplica as migrations no banco e encerra

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .editorconfig dotnet-tools.json ./
COPY src/Backend.Api/Backend.Api.csproj src/Backend.Api/
COPY src/Backend.Application/Backend.Application.csproj src/Backend.Application/
COPY src/Backend.Domain/Backend.Domain.csproj src/Backend.Domain/
COPY src/Backend.Infrastructure/Backend.Infrastructure.csproj src/Backend.Infrastructure/
COPY src/Backend.Repository/Backend.Repository.csproj src/Backend.Repository/
RUN dotnet restore src/Backend.Api/Backend.Api.csproj && dotnet tool restore
COPY src/ src/
RUN dotnet publish src/Backend.Api/Backend.Api.csproj --configuration Release --no-restore --output /app/publish
# O EF Core monta o host da API para ler o modelo: os valores abaixo só servem para isso (o bundle não guarda nenhum).
RUN ConnectionStrings__Default="Host=localhost;Database=build;Username=build;Password=build" \
    Jwt__Secret="segredo-usado-apenas-para-montar-o-modelo-no-build" \
    Storage__Provider=Local \
    dotnet ef migrations bundle --project src/Backend.Repository --startup-project src/Backend.Api \
    --configuration Release --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrations
WORKDIR /app
COPY --from=build /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app/publish ./
USER $APP_UID
# Fotos e vídeos ficam no Google Drive (GoogleDrive__* no .env do servidor), não no container.
ENV HOST=0.0.0.0 \
    PORT=4000
EXPOSE 4000
ENTRYPOINT ["dotnet", "Backend.Api.dll"]
