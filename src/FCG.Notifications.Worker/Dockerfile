# syntax=docker/dockerfile:1

# build 
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FCG.Notifications.Worker.csproj", "./"]
RUN dotnet restore "FCG.Notifications.Worker.csproj"

COPY . .
RUN dotnet publish "FCG.Notifications.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# runtime 
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FCG.Notifications.Worker.dll"]
