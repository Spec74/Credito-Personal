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
RUN ln -s credito.modern.sln Credito.Modern.sln \
    && ln -s credito.modern.domain.csproj Credito.Modern.Domain/Credito.Modern.Domain.csproj \
    && ln -s credito.modern.application.csproj Credito.Modern.Application/Credito.Modern.Application.csproj \
    && ln -s credito.modern.infrastructure.csproj Credito.Modern.Infrastructure/Credito.Modern.Infrastructure.csproj \
    && ln -s credito.modern.api.csproj Credito.Modern.Api/Credito.Modern.Api.csproj \
    && ln -s credito.modern.tests.csproj Credito.Modern.Tests/Credito.Modern.Tests.csproj
RUN dotnet restore credito.modern.sln
RUN dotnet publish Credito.Modern.Api/credito.modern.api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "credito.modern.api.dll"]
