FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
USER root
RUN apt-get update && apt-get install -y curl
ENV ASPNETCORE_URLS=http://+:5190
EXPOSE 5190
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/backend.csproj", "backend/"]
RUN dotnet restore "backend/backend.csproj"
COPY . .
WORKDIR "/src/backend"
RUN dotnet build "./backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "backend.dll"]
