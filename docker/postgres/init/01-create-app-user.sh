#!/bin/bash
# postgres 映像在 volume 首次初始化時執行 /docker-entrypoint-initdb.d/ 內的腳本（之後不再執行）。
# 建立應用程式專屬帳號與其擁有的資料庫：POSTGRES_USER 是超級使用者，應用程式不使用它（CLAUDE.md §3）。
# 資料庫擁有者可直接在 public schema 建表（PostgreSQL 15+ 的預設權限），EnsureCreated 不需另外授權。
#
# 值以 psql 變數傳入、heredoc 加引號不讓 shell 展開：密碼含特殊字元也不會被拼進 SQL 造成語法錯誤。
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres \
  -v app_db="$APP_DB_NAME" -v app_user="$APP_DB_USER" -v app_password="$APP_DB_PASSWORD" <<'SQL'
CREATE ROLE :"app_user" LOGIN PASSWORD :'app_password';
CREATE DATABASE :"app_db" OWNER :"app_user";
SQL
