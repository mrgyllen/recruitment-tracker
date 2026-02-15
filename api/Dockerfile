FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props global.json ./
COPY api.slnx ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Web/Web.csproj src/Web/
COPY tests/Application.FunctionalTests/Application.FunctionalTests.csproj tests/Application.FunctionalTests/
COPY tests/Application.UnitTests/Application.UnitTests.csproj tests/Application.UnitTests/
COPY tests/Domain.UnitTests/Domain.UnitTests.csproj tests/Domain.UnitTests/
COPY tests/Infrastructure.IntegrationTests/Infrastructure.IntegrationTests.csproj tests/Infrastructure.IntegrationTests/

RUN dotnet restore api.slnx

COPY . .
RUN dotnet publish src/Web/Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "api.Web.dll"]
