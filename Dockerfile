FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj", "src/FCG.Notifications.Worker/"]
RUN dotnet restore "src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj"

COPY . .
RUN dotnet publish "src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

COPY --from=build /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "FCG.Notifications.Worker.dll"]
