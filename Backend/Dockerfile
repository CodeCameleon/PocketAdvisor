# syntax=docker/dockerfile:1

# -----------------------------------------------------------------------------
# Build stage: compiles and publishes the application with the .NET SDK.
# It runs on the platform of the build machine and cross-compiles for the target architecture.
# -----------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY PocketAdvisor.DbContexts/PocketAdvisor.DbContexts.csproj PocketAdvisor.DbContexts/
COPY PocketAdvisor.Entities/PocketAdvisor.Entities.csproj PocketAdvisor.Entities/
COPY PocketAdvisor.Enums/PocketAdvisor.Enums.csproj PocketAdvisor.Enums/
COPY PocketAdvisor.Repositories/PocketAdvisor.Repositories.csproj PocketAdvisor.Repositories/
COPY PocketAdvisor.Requests/PocketAdvisor.Requests.csproj PocketAdvisor.Requests/
COPY PocketAdvisor.Responses/PocketAdvisor.Responses.csproj PocketAdvisor.Responses/
COPY PocketAdvisor.Services/PocketAdvisor.Services.csproj PocketAdvisor.Services/
COPY PocketAdvisor.WebApplication/PocketAdvisor.WebApplication.csproj PocketAdvisor.WebApplication/
RUN dotnet restore PocketAdvisor.WebApplication/PocketAdvisor.WebApplication.csproj -a $TARGETARCH

ARG APP_VERSION=development
COPY . .
RUN dotnet publish PocketAdvisor.WebApplication/PocketAdvisor.WebApplication.csproj \
        -a $TARGETARCH \
        -c $BUILD_CONFIGURATION \
        --no-restore \
        -o /app/publish \
        -p:UseAppHost=false \
        -p:InformationalVersion=$APP_VERSION

# -----------------------------------------------------------------------------
# Runtime stage: only the ASP.NET Core runtime and the published output.
# The SDK, the source code and the intermediate build files are left behind.
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ARG APP_VERSION=development
LABEL org.opencontainers.image.title="PocketAdvisor API" \
      org.opencontainers.image.version=$APP_VERSION \
      org.opencontainers.image.source="https://github.com/CodeCameleon/PocketAdvisor"
ENV APP_VERSION=$APP_VERSION \
    ASPNETCORE_HTTP_PORTS=8080

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "PocketAdvisor.WebApplication.dll"]
