FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BlackFriday.Api/BlackFriday.Api.csproj", "BlackFriday.Api/"]
COPY ["BlackFriday.Domain/BlackFriday.Domain.csproj", "BlackFriday.Domain/"]
COPY ["BlackFriday.Infrastructure/BlackFriday.Infrastructure.csproj", "BlackFriday.Infrastructure/"]
RUN dotnet restore "BlackFriday.Api/BlackFriday.Api.csproj"

COPY . .
WORKDIR /src/BlackFriday.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BlackFriday.Api.dll"]