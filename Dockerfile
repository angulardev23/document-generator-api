FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/DocumentGenerator.Api/DocumentGenerator.Api.csproj", "src/DocumentGenerator.Api/"]
COPY ["src/DocumentGenerator.Application/DocumentGenerator.Application.csproj", "src/DocumentGenerator.Application/"]
COPY ["src/DocumentGenerator.Domain/DocumentGenerator.Domain.csproj", "src/DocumentGenerator.Domain/"]
COPY ["src/DocumentGenerator.Infrastructure/DocumentGenerator.Infrastructure.csproj", "src/DocumentGenerator.Infrastructure/"]
RUN dotnet restore "src/DocumentGenerator.Api/DocumentGenerator.Api.csproj"

COPY . .
RUN dotnet publish "src/DocumentGenerator.Api/DocumentGenerator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentGenerator.Api.dll"]

