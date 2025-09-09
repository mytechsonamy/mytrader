# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file
COPY MyTrader.sln ./

# Copy project files
COPY MyTrader.Api/MyTrader.Api.csproj MyTrader.Api/
COPY MyTrader.Core/MyTrader.Core.csproj MyTrader.Core/
COPY MyTrader.Infrastructure/MyTrader.Infrastructure.csproj MyTrader.Infrastructure/
COPY MyTrader.Services/MyTrader.Services.csproj MyTrader.Services/

# Restore packages
RUN dotnet restore MyTrader.sln

# Copy everything else and build
COPY . .
WORKDIR /src/MyTrader.Api
RUN dotnet build MyTrader.Api.csproj -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish MyTrader.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyTrader.Api.dll"]