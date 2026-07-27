FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["PMS.API/PMS.API.csproj", "PMS.API/"]
COPY ["PMS.Application/PMS.Application.csproj", "PMS.Application/"]
COPY ["PMS.Domain/PMS.Domain.csproj", "PMS.Domain/"]
COPY ["PMS.Infrastructure/PMS.Infrastructure.csproj", "PMS.Infrastructure/"]
RUN dotnet restore "PMS.API/PMS.API.csproj"

COPY . .
WORKDIR "/src/PMS.API"
RUN dotnet publish "PMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
EXPOSE 8080
ENTRYPOINT ["dotnet", "PMS.API.dll"]
