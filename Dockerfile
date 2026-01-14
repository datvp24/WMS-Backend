# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Wms.Api/Wms.Api.csproj", "Wms.Api/"]
COPY ["Wms.Application/Wms.Application.csproj", "Wms.Application/"]
COPY ["Wms.Domain/Wms.Domain.csproj", "Wms.Domain/"]
COPY ["Wms.Infrastructure/Wms.Infrastructure.csproj", "Wms.Infrastructure/"]
COPY ["Wms.Shared/Wms.Shared.csproj", "Wms.Shared/"]

RUN dotnet restore "Wms.Api/Wms.Api.csproj"

COPY . .
WORKDIR "/src/Wms.Api"
RUN dotnet build "Wms.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Wms.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Wms.Api.dll"]
