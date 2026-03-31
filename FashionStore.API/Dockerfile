# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["FashionStore.API/FashionStore.API.csproj", "FashionStore.API/"]
COPY ["FashionStore.Infrastructure/FashionStore.Infrastructure.csproj", "FashionStore.Infrastructure/"]
COPY ["FashionStore.Application/FashionStore.Application.csproj", "FashionStore.Application/"]
COPY ["FashionStore.Domain/FashionStore.Domain.csproj", "FashionStore.Domain/"]
COPY ["FashionStore.Shared/FashionStore.Shared.csproj", "FashionStore.Shared/"]
RUN dotnet restore "./FashionStore.API/FashionStore.API.csproj"
COPY . .
WORKDIR "/src/FashionStore.API"
RUN dotnet build "./FashionStore.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FashionStore.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FashionStore.API.dll"]