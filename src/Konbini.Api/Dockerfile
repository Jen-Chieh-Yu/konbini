# base：執行環境。VS 容器偵錯（Fast Mode）只建這一層，原始碼用掛載的
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# 以映像內建的非 root 帳號執行（最小權限，與資料庫不用 root 同理）；8080 非特權埠不受影響
USER $APP_UID
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
# 版號由 CI 依標籤傳入（.github/scripts/release-info.sh）；本機建置維持預設值。
# 寫進組件的 InformationalVersion（版號+commit），由 GET /api/version 讀出
ARG APP_VERSION=0.0.0-dev
ARG GIT_SHA=local
# 只帶目標架構的原生檔（SkiaSharp）：不指定 RID 時，各平台的原生檔與除錯符號都會放進映像，多出約 480 MB。
# TARGETARCH 由 BuildKit 依建置平台帶入（amd64／arm64）
ARG TARGETARCH
RUN case "$TARGETARCH" in \
      amd64) rid=linux-x64 ;; \
      arm64) rid=linux-arm64 ;; \
      *) echo "不支援的架構：'$TARGETARCH'（需要 BuildKit，只支援 amd64、arm64）" >&2; exit 1 ;; \
    esac \
    && dotnet publish "Konbini.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
    -r "$rid" --self-contained false \
    /p:Version=$APP_VERSION /p:InformationalVersion=$APP_VERSION+$GIT_SHA

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Konbini.Api.dll"]
