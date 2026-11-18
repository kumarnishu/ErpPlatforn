FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG PROJECT_PATH
COPY Server/ ./Server/
RUN dotnet restore "Server/${PROJECT_PATH}"
RUN dotnet publish "Server/${PROJECT_PATH}" -c Release -o /app/publish --no-restore \
    && cp "/app/publish/$(basename "${PROJECT_PATH}" .csproj).dll" /app/publish/app.dll

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "app.dll"]
