FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG API_PROJECT=Services/Platform/Platform.Api/Platform.Api.csproj
WORKDIR /src

COPY Server/ ./Server/
WORKDIR /src/Server
RUN dotnet restore "$API_PROJECT"
RUN dotnet publish "$API_PROJECT" --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG API_DLL=Platform.Api.dll
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
ENV API_DLL=${API_DLL}
EXPOSE 8080
COPY --from=build /app/publish ./
ENTRYPOINT ["sh", "-c", "exec dotnet \"$API_DLL\""]
