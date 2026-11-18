FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG PROJECT_PATH
COPY Server/ ./Server/
RUN dotnet restore "Server/${PROJECT_PATH}"
RUN dotnet publish "Server/${PROJECT_PATH}" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ARG PROJECT_DLL
ENV PROJECT_DLL=${PROJECT_DLL}
ENTRYPOINT ["sh", "-c", "exec dotnet \"$PROJECT_DLL\""]
