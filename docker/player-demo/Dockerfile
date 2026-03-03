FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore examples/MLambda.Actors.PlayerDemo/MLambda.Actors.PlayerDemo.csproj
RUN dotnet publish examples/MLambda.Actors.PlayerDemo/MLambda.Actors.PlayerDemo.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MLambda.Actors.PlayerDemo.dll"]
