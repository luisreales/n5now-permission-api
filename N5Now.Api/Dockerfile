# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY N5Now.Domain/N5Now.Domain.csproj ./N5Now.Domain/
COPY N5Now.Application/N5Now.Application.csproj ./N5Now.Application/
COPY N5Now.Infrastructure/N5Now.Infrastructure.csproj ./N5Now.Infrastructure/
COPY N5Now.Api/N5Now.Api.csproj ./N5Now.Api/
RUN dotnet restore ./N5Now.Api/N5Now.Api.csproj

# Copy everything from the project
COPY N5Now.Api/serilog.json ./serilog.json


COPY . .
WORKDIR /src/N5Now.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "N5Now.Api.dll"]
