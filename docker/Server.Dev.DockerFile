FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

ARG PROJECT_PATH
ENV PROJECT_PATH=${PROJECT_PATH}
COPY Server/ ./Server/
RUN dotnet restore "Server/${PROJECT_PATH}"

ENTRYPOINT ["sh", "-c", "dotnet watch --project \"Server/${PROJECT_PATH}\" run --urls \"${ASPNETCORE_URLS}\""]
