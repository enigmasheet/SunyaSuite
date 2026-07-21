FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
EXPOSE 8080

FROM base AS final
WORKDIR /app
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    addgroup --system --gid 1001 app && \
    adduser --system --uid 1001 --ingroup app app && \
    chown -R app:app /app
USER app
ENV ASPNETCORE_URLS=http://+:8080
LABEL org.opencontainers.image.title="SunyaSuite" \
      org.opencontainers.image.description="SunyaSuite - Business Management Platform" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.source="https://github.com/anomalyco/SunyaSuite"
COPY --chown=app:app --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SunyaSuite.Web.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG CONFIGURATION=Release
WORKDIR /src

COPY Directory.Packages.props Directory.Build.props ./
COPY src/SunyaSuite.Domain/SunyaSuite.Domain.csproj src/SunyaSuite.Domain/
COPY src/SunyaSuite.Application/SunyaSuite.Application.csproj src/SunyaSuite.Application/
COPY src/SunyaSuite.Infrastructure/SunyaSuite.Infrastructure.csproj src/SunyaSuite.Infrastructure/
COPY src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj src/SunyaSuite.Web/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj -r linux-x64

COPY . .
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj \
    -c $CONFIGURATION \
    -o /app/publish \
    -p:RuntimeIdentifier=linux-x64

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS development
WORKDIR /src
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

COPY Directory.Packages.props Directory.Build.props ./
COPY src/SunyaSuite.Domain/SunyaSuite.Domain.csproj src/SunyaSuite.Domain/
COPY src/SunyaSuite.Application/SunyaSuite.Application.csproj src/SunyaSuite.Application/
COPY src/SunyaSuite.Infrastructure/SunyaSuite.Infrastructure.csproj src/SunyaSuite.Infrastructure/
COPY src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj src/SunyaSuite.Web/
RUN dotnet restore src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "run", "--no-launch-profile", "-c", "Debug", "--project", "src/SunyaSuite.Web/SunyaSuite.Web.Api.csproj"]
