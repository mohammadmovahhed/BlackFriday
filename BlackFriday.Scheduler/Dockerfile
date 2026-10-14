FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["BlackFriday.Scheduler/BlackFriday.Scheduler.csproj", "BlackFriday.Scheduler/"]
COPY ["BlackFriday.Domain/BlackFriday.Domain.csproj", "BlackFriday.Domain/"]
COPY ["BlackFriday.Infrastructure/BlackFriday.Infrastructure.csproj", "BlackFriday.Infrastructure/"]
RUN dotnet restore "BlackFriday.Scheduler/BlackFriday.Scheduler.csproj"

COPY . .
WORKDIR /src/BlackFriday.Scheduler
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BlackFriday.Scheduler.dll"]