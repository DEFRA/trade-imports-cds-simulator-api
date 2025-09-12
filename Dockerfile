# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt upgrade -y && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY .csharpierrc .csharpierrc
COPY .csharpierignore .csharpierignore

RUN dotnet tool restore

COPY src/CdsSimulator/CdsSimulator.csproj src/CdsSimulator/CdsSimulator.csproj
COPY tests/Testing/Testing.csproj tests/Testing/Testing.csproj
COPY tests/TestFixtures/TestFixtures.csproj tests/TestFixtures/TestFixtures.csproj
COPY tests/CdsSimulator.Tests/CdsSimulator.Tests.csproj tests/CdsSimulator.Tests/CdsSimulator.Tests.csproj
COPY tests/CdsSimulator.IntegrationTests/CdsSimulator.IntegrationTests.csproj tests/CdsSimulator.IntegrationTests/CdsSimulator.IntegrationTests.csproj
COPY Defra.TradeImportsCdsSimulator.sln Defra.TradeImportsCdsSimulator.sln
COPY Directory.Build.props Directory.Build.props

COPY NuGet.config NuGet.config
ARG DEFRA_NUGET_PAT

RUN dotnet restore

COPY src/CdsSimulator src/CdsSimulator
COPY tests/Testing tests/Testing
COPY tests/TestFixtures tests/TestFixtures
COPY tests/CdsSimulator.Tests tests/CdsSimulator.Tests

RUN dotnet csharpier check .

RUN dotnet build src/CdsSimulator/CdsSimulator.csproj --no-restore -c Release

RUN dotnet test --no-restore tests/CdsSimulator.Tests

FROM build AS publish

RUN dotnet publish src/CdsSimulator -c Release -o /app/publish /p:UseAppHost=false

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

EXPOSE 8085
ENTRYPOINT ["dotnet", "Defra.TradeImportsCdsSimulator.dll"]
