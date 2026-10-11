FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CodeInspector.sln ./
COPY src/CodeInspector.Leads.Core ./src/CodeInspector.Leads.Core
COPY src/CodeInspector.Leads.Infrastructure ./src/CodeInspector.Leads.Infrastructure
COPY src/CodeInspector.Leads.Api ./src/CodeInspector.Leads.Api
RUN dotnet restore src/CodeInspector.Leads.Api/CodeInspector.Leads.Api.csproj
RUN dotnet publish src/CodeInspector.Leads.Api/CodeInspector.Leads.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CodeInspector.Leads.Api.dll"]
