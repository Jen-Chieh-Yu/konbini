#!/usr/bin/env bash
# 決定這次打包的版號與成品名稱（ci.yml 的「打包：決定版號」job 使用）。
#
#   正式發行：推 v 開頭的標籤（v0.1.0、v0.2.0-rc.1）
#     版號＝標籤去掉 v；成品名稱尾碼＝標籤。標籤指向的 commit 必須已在 main 上（不能從功能分支發行）
#   測試打包：Actions 手動執行並勾選「測試打包」（任何分支）
#     版號＝0.0.0-test.<執行編號>；成品名稱尾碼＝test-<執行編號>。成品只能用於測試，不能當成發行版
#
# 輸出（$GITHUB_OUTPUT）：version、kind（release／test）、suffix
# 需要 checkout 時 fetch-depth: 0（取回 origin/main 才能比對）。
set -euo pipefail

# .NET 的 Version 與映像標籤都不接受 v 開頭；trigger 只要求 v 開頭，vABC、v1.2 也會觸發，在這裡擋下
SEMVER_RE='^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$'

if [[ "${GITHUB_REF}" == refs/tags/v* ]]; then
  TAG="${GITHUB_REF#refs/tags/}"
  VERSION="${TAG#v}"
  if [[ ! "$VERSION" =~ $SEMVER_RE ]]; then
    echo "::error::版號格式不合法：'$TAG'。應為 vX.Y.Z 或 vX.Y.Z-字尾，例如 v0.1.0、v0.2.0-rc.1"
    exit 1
  fi
  if ! git merge-base --is-ancestor "$GITHUB_SHA" origin/main; then
    echo "::error::標籤 $TAG 指向的 commit 不在 main 上。正式發行只能從 main 打標籤；要在分支上試跑打包，請手動執行 CI 並勾選「測試打包」"
    exit 1
  fi
  KIND=release
  SUFFIX="$TAG"
else
  VERSION="0.0.0-test.${GITHUB_RUN_NUMBER}"
  KIND=test
  SUFFIX="test-${GITHUB_RUN_NUMBER}"
fi

{
  echo "version=$VERSION"
  echo "kind=$KIND"
  echo "suffix=$SUFFIX"
} >> "$GITHUB_OUTPUT"
echo "## 版號：\`$VERSION\`（$KIND，成品尾碼 \`$SUFFIX\`）" >> "$GITHUB_STEP_SUMMARY"
echo "版號：$VERSION（$KIND）"
