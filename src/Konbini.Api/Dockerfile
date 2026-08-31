# base：執行環境。VS 容器偵錯（Fast Mode）只建這一層，原始碼用掛載的
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# build（build context 為 repo 根目錄）
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Konbini.Api/Konbini.Api.csproj", "src/Konbini.Api/"]
RUN dotnet restore "src/Konbini.Api/Konbini.Api.csproj"
COPY src/ src/
WORKDIR /src/src/Konbini.Api
RUN dotnet build "Konbini.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Konbini.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Konbini.Api.dll"]
