# Dockerfile for FinBrief.Api.
# Build:  docker build -t finbrief-api .
# Run:    docker run -p 8080:8080 -e ConnectionStrings__Default="Host=...;..." finbrief-api
#         then open http://localhost:8080/health

# ---------- Stage 1: build with the full .NET SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first and restore packages. Docker caches this layer,
# so packages are downloaded again only when a .csproj changes, not on every code change.
COPY src/FinBrief.Core/FinBrief.Core.csproj src/FinBrief.Core/
COPY src/FinBrief.Infrastructure/FinBrief.Infrastructure.csproj src/FinBrief.Infrastructure/
COPY src/FinBrief.Api/FinBrief.Api.csproj src/FinBrief.Api/
RUN dotnet restore src/FinBrief.Api/FinBrief.Api.csproj

# Now copy the source code and publish a release build.
COPY src/FinBrief.Core/ src/FinBrief.Core/
COPY src/FinBrief.Infrastructure/ src/FinBrief.Infrastructure/
COPY src/FinBrief.Api/ src/FinBrief.Api/
RUN dotnet publish src/FinBrief.Api/FinBrief.Api.csproj --configuration Release --no-restore --output /app/publish

# ---------- Stage 2: run with the small ASP.NET runtime image (no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The API listens on port 8080 inside the container.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the non-root "app" user that the .NET images provide.
USER app

# Settings come from environment variables at runtime (never baked into the image):
#   ConnectionStrings__Default   - required
#   Cors__AllowedOrigins__0      - your frontend's URL, e.g. https://finbrief.example.com
ENTRYPOINT ["dotnet", "FinBrief.Api.dll"]
