# 建置階段（Node 主版本與 .nvmrc 一致；Docker 讀不到 .nvmrc，升版時兩處一起改）
FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# 執行階段：nginx 供應靜態檔並反代 /api
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
