FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN mkdir -p /app/keys && chown $APP_UID /app/keys
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage — needs Node.js for Tailwind CSS compilation
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js (LTS)
RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy .csproj files first so the restore layer is cached independently of source changes.
# Domain must be included because Web references it.
COPY FellsideDigital.Domain/FellsideDigital.Domain.csproj FellsideDigital.Domain/
COPY FellsideDigital.UI/FellsideDigital.UI.csproj FellsideDigital.UI/
COPY FellsideDigital.Web/FellsideDigital.Web.csproj FellsideDigital.Web/

RUN dotnet restore FellsideDigital.Web/FellsideDigital.Web.csproj

# Copy remaining source (obj/ and bin/ are excluded via .dockerignore)
COPY FellsideDigital.Domain/ FellsideDigital.Domain/
COPY FellsideDigital.UI/ FellsideDigital.UI/
COPY FellsideDigital.Web/ FellsideDigital.Web/

WORKDIR /src/FellsideDigital.Web

# Install npm dependencies (needed for Tailwind)
RUN npm install
RUN npm run tailwind:build

# Publish — --no-restore is safe because .dockerignore keeps obj/ clean
RUN dotnet publish FellsideDigital.Web.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FellsideDigital.Web.dll"]
