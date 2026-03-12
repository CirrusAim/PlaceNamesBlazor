# Build stage - .NET 10
# Build context: repo root (standalone Blazor repo) or Root Directory "PlaceNamesBlazor" (when inside Project_DB)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/PlaceNamesBlazor/PlaceNamesBlazor.csproj", "src/PlaceNamesBlazor/"]
RUN dotnet restore "src/PlaceNamesBlazor/PlaceNamesBlazor.csproj"

COPY src/PlaceNamesBlazor/ src/PlaceNamesBlazor/
WORKDIR /src/src/PlaceNamesBlazor
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql may load GSSAPI for PostgreSQL auth; install so libgssapi_krb5.so.2 is available
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Render sets PORT at runtime; entrypoint script substitutes it into ASPNETCORE_URLS
COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh

EXPOSE 5000

ENTRYPOINT ["./docker-entrypoint.sh"]
