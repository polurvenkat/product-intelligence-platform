# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/ProductIntelligence.TeamsBot/ProductIntelligence.TeamsBot.csproj", "src/ProductIntelligence.TeamsBot/"]
COPY ["src/ProductIntelligence.Application/ProductIntelligence.Application.csproj", "src/ProductIntelligence.Application/"]
COPY ["src/ProductIntelligence.Infrastructure/ProductIntelligence.Infrastructure.csproj", "src/ProductIntelligence.Infrastructure/"]
COPY ["src/ProductIntelligence.Domain/ProductIntelligence.Domain.csproj", "src/ProductIntelligence.Domain/"]
COPY ["src/ProductIntelligence.Core/ProductIntelligence.Core.csproj", "src/ProductIntelligence.Core/"]

RUN dotnet restore "src/ProductIntelligence.TeamsBot/ProductIntelligence.TeamsBot.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/src/ProductIntelligence.TeamsBot"
RUN dotnet build "ProductIntelligence.TeamsBot.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ProductIntelligence.TeamsBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductIntelligence.TeamsBot.dll"]
