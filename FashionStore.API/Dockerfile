FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN apt-get update && apt-get install -y libgssapi-krb5-2 libkrb5-dev && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

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

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FashionStore.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FashionStore.API.dll"]