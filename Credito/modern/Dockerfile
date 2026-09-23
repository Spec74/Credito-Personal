# Build context: carpeta modern/ (misma que el workflow CI).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . ./
# CI Linux suele versionar nombres en minúsculas; checkout Windows trae PascalCase.
# Solo crear symlinks si falta el nombre canónico de publish/restore.
RUN set -e; \
    [ -f Credito.Modern.sln ] || ln -s credito.modern.sln Credito.Modern.sln; \
    [ -f Credito.Modern.Domain/Credito.Modern.Domain.csproj ] || ln -s credito.modern.domain.csproj Credito.Modern.Domain/Credito.Modern.Domain.csproj; \
    [ -f Credito.Modern.Application/Credito.Modern.Application.csproj ] || ln -s credito.modern.application.csproj Credito.Modern.Application/Credito.Modern.Application.csproj; \
    [ -f Credito.Modern.Infrastructure/Credito.Modern.Infrastructure.csproj ] || ln -s credito.modern.infrastructure.csproj Credito.Modern.Infrastructure/Credito.Modern.Infrastructure.csproj; \
    [ -f Credito.Modern.Api/Credito.Modern.Api.csproj ] || ln -s credito.modern.api.csproj Credito.Modern.Api/Credito.Modern.Api.csproj; \
    [ -f Credito.Modern.Tests/Credito.Modern.Tests.csproj ] || ln -s credito.modern.tests.csproj Credito.Modern.Tests/Credito.Modern.Tests.csproj
RUN dotnet restore Credito.Modern.sln
RUN dotnet publish Credito.Modern.Api/Credito.Modern.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Linux es case-sensitive: ASPNETCORE_ENVIRONMENT=Staging carga appsettings.Staging.json
# (publish desde Windows/CI suele dejar el archivo en minúsculas).
RUN set -e; \
    [ -f appsettings.Staging.json ] || ln -s appsettings.staging.json appsettings.Staging.json; \
    [ -f appsettings.Development.json ] || ln -s appsettings.development.json appsettings.Development.json; \
    [ -f appsettings.Production.json ] || ln -s appsettings.production.json appsettings.Production.json; \
    [ -f appsettings.PreProduction.json ] || ln -s appsettings.preproduction.json appsettings.PreProduction.json
ENTRYPOINT ["dotnet", "Credito.Modern.Api.dll"]
