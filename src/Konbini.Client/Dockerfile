# 建置階段（Node 主版本與 .nvmrc 一致；Docker 讀不到 .nvmrc，升版時兩處一起改）
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# 版號由 CI 依標籤傳入；寫成 dist/version.json，部署後可直接開網址確認前端是哪一版
ARG APP_VERSION=0.0.0-dev
ARG GIT_SHA=local
RUN npm run build \
    && printf '{"version":"%s","commit":"%s"}\n' "$APP_VERSION" "$GIT_SHA" > dist/version.json

# 執行階段：nginx 供應靜態檔並反代 /api
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
