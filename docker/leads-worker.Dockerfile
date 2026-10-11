FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY CodeInspector.slnx ./
COPY src/CodeInspector.Leads.Core ./src/CodeInspector.Leads.Core
COPY src/CodeInspector.Leads.Infrastructure ./src/CodeInspector.Leads.Infrastructure
COPY src/CodeInspector.Leads.Worker ./src/CodeInspector.Leads.Worker
RUN dotnet restore src/CodeInspector.Leads.Worker/CodeInspector.Leads.Worker.csproj
RUN dotnet publish src/CodeInspector.Leads.Worker/CodeInspector.Leads.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
RUN apt-get update && apt-get install -y libnss3 libatk-bridge2.0-0 libdrm2 libxkbcommon0 libgbm1 libasound2t64 libxshmfence1 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install chromium --with-deps
ENV PATH="/root/.dotnet/tools:${PATH}"
ENTRYPOINT ["dotnet", "CodeInspector.Leads.Worker.dll"]
