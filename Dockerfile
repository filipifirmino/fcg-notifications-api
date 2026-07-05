FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj", "src/FCG.Notifications.Worker/"]
COPY ["src/FCG.Notifications.Application/FCG.Notifications.Application.csproj", "src/FCG.Notifications.Application/"]
COPY ["src/FCG.Notifications.Domain/FCG.Notifications.Domain.csproj", "src/FCG.Notifications.Domain/"]
COPY ["src/FCG.Notifications.Infra/FCG.Notifications.Infra.csproj", "src/FCG.Notifications.Infra/"]
RUN dotnet restore "src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj"

COPY . .
RUN dotnet publish "src/FCG.Notifications.Worker/FCG.Notifications.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system --gid 1001 appgroup && \
    useradd --system --uid 1001 --gid appgroup --no-create-home appuser

COPY --from=build /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "FCG.Notifications.Worker.dll"]
