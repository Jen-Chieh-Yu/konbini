---
name: create-pr
description: 把目前分支整理成可送審的 Pull Request：自我檢查、推送分支，產出 Pull Request 的 title、Markdown description，以及 Squash 時要貼的純文字 Extended description。預設由使用者自己在 GitHub 網頁建立；使用者明確要求且已安裝 gh 時才用 gh pr create。使用時機：使用者說「建立 PR」「開 Pull Request」「送審」「給我 PR 的 title 和 description」「準備 merge」。只準備與建立，不核准、不 merge。
---

# 建立 Pull Request（create-pr）

目的：把送審前重複、容易出錯的動作交給 AI，**merge 由使用者自己做**。
規則來源：`CLAUDE.md` §0（協作慣例）、§6（分支、CI、Merge 策略）。

## 步驟

1. **確認分支與工作目錄**
   - 不可以在 `main`。分支名稱須為 `feature/` `fix/` `refactor/` `docs/` `chore/` `hotfix/` ＋ `YYYYMMDD-描述`；不符時提出正確名稱，**問過再改名**（已推送過的分支改名要一併處理遠端）。
   - `git status` 有未 commit 的修改：先給 diff 與擬定的 commit message 請使用者確認，同意後才 commit。未追蹤的檔案列出來由使用者決定，不要自動加入。
   - commit message：Conventional Commits，type（與 scope）用英文、描述用繁體中文，結尾加 `Co-Authored-By` 行。
2. **這支分支是否已有 Pull Request**：`curl -s "https://api.github.com/repos/Jen-Chieh-Yu/konbini/pulls?state=open&head=Jen-Chieh-Yu:<分支>"`（公開 repo，不需要登入）。
   - 已有：不要產生「建立用」的內容，也不要再建一個。推送新的 commit 後 Pull Request 會自動更新、CI 重跑；description 需要跟著改時，走下方「更新既有 Pull Request 的說明」。
3. **與 main 的關係**（先檢查、不動工作目錄）：`git fetch origin`，然後
   - 落後幾個 commit：`git rev-list --count HEAD..origin/main`
   - 試合併：`git merge-tree --write-tree origin/main HEAD`，結束碼 1 代表有衝突，輸出會列出衝突的檔案。
   - **有衝突**：停下來列出衝突檔案，由使用者決定怎麼解；解完再繼續，不要帶著衝突送審。
   - **落後但沒有衝突**：告訴使用者落後幾個 commit，詢問要先同步還是直接送審（ruleset 要求分支與 main 同步，晚點也得在網頁按 Update branch）。
   - 同步一律 **merge**（`git merge origin/main`），不要 rebase——推送過的分支 rebase 要 force push；同步用的 merge commit 會在 Squash 時消失。還沒推送過的分支才可以 rebase。
4. **自我檢查**（失敗就停下來回報，不要推送）
   - 只有 push 時 `.githooks/pre-push` 會自動跑 actionlint、後端測試、前端型別檢查；尚未設定 `core.hooksPath` 時要提醒使用者，並說明這些檢查沒有在本機跑過。
   - 新增或修改會在容器內執行的腳本（`*.sh`）：`git ls-files --eol <檔案>` 應為 `w/lf`。Windows 的 `core.autocrlf=true` 可能把新檔寫成 CRLF，容器執行時會出現 `cannot execute: required file not found`；是 `w/crlf` 就用 `sed -i 's/\r$//' <檔案>` 轉回 LF。
   - 改到 compose 或 `.env.example`：`docker compose config -q` 與 `docker compose -f docker-compose.prod.yml config -q`（**加 `-q`**，不帶時會印出 `.env` 的實際值）。
   - 測試由使用者自己執行（CLAUDE.md §0），不要代跑 `dotnet test`／`npm` 指令；需要時把指令給使用者。pre-push hook 是 git 推送的一部分，不在此限。
5. **看變更範圍**：`git diff origin/main...HEAD --stat`。混了多個主題、或有意義的變更超過約 500 行時，建議拆分，由使用者決定。主題不同的改動（例如 `ci:` 與 `chore:`）不要擠在同一個 Pull Request——Squash 後只剩一個標題，寫不清楚。
6. **推送**：使用者同意後 `git push -u origin <分支>`。不 force push、不推送到 `main`（ruleset 也會擋）。
7. **產出三段內容**（見下方格式），內容以實際做過的事為準：
   - **Title**：`type(scope): 繁體中文描述`，會直接成為 main 上的 commit 標題。
   - **Description（Markdown）**：照 `.github/pull_request_template.md` 的章節填寫；不適用的章節不要硬填或留空，改寫成一行「不適用：原因」（例如純文件變更的「merge 後的影響」），沒有內容的「不在這次範圍」整節刪掉。不附加「Generated with Claude Code」之類的產生工具標記。
   - **Squash 用的 Extended description（純文字）**：見「為什麼要純文字版」。
8. **建立 Pull Request**
   - 預設：給使用者 `https://github.com/Jen-Chieh-Yu/konbini/pull/new/<分支>` 與上面三段，由使用者在網頁建立。
   - 使用者明確要求、且 `gh` 已安裝並登入時：`gh pr create --base main --title "<title>" --body-file <暫存檔>`（說明檔放暫存資料夾、UTF-8，不放進專案）。
9. **回報**：分支、push 結果、自我檢查做了什麼與沒做什麼、Pull Request 連結或建立網址，並提醒 Squash 的操作（見「Squash merge 的操作提醒」）。

## 更新既有 Pull Request 的說明

追加 commit、或審查後要補充內容時：
1. 先把新的 title／description 草稿（以及對應的 Squash 純文字版）給使用者看，標出改了哪幾處。
2. 使用者同意後，由使用者在 Pull Request 頁面按 `···` → **Edit** 修改；使用者明確要求且 `gh` 可用時，才用 `gh pr edit <編號> --title "<title>" --body-file <暫存檔>`。
3. 「驗證」新增的項目一樣只勾實際做過的。

## Squash merge 的操作提醒（每次都要說）

main 只允許 Squash merge（CLAUDE.md §6「Merge 策略」）：
- **Commit message 欄位不要動**。GitHub 會自動帶入 `<Pull Request title> (#N)`；手動改過這一欄，main 上的標題就會失去前綴與編號，而且之後無法修正（main 禁止 force push）。
- **只把 Extended description 換成純文字版**。
- 按 Confirm 前再看一次 Commit message 是否為 `type: 描述 (#N)`。
- 分支顯示 out-of-date 時按 **Update branch**，等 CI 綠燈再 merge。
- merge 後本機清理：`git switch main && git pull --ff-only`，再 `git branch -D <分支>`。Squash 後 git 認不出分支已合併，`-d` 會拒絕；刪除前先確認 `git diff <分支> origin/main` 沒有差異。

## 為什麼要純文字版

Squash 的 Extended description 會原樣成為 main 上的 commit 內文：
- `##` 開頭的行在 git 裡是註解：日後本機 amend、rebase、cherry-pick 時編輯訊息，會被整行刪掉。
- `git log` 不渲染 Markdown，粗體、反引號、`- [x]` 都是雜訊。
- 「驗證」是 merge 前的檢查紀錄，留在 Pull Request 頁面即可，不進 commit。

## 內容寫法

- 寫「改了什麼、為什麼、merge 後有什麼影響」，讓人不看 diff 也知道重點。
- **不寫只有當下對話才看得懂的指涉**：「參考專案」「另一個專案」「上一則」「剛剛討論的」。直接寫出內容本身。
- 驗證項目只勾實際做過的；沒在瀏覽器看過畫面、沒實際啟動服務，就不要勾，並寫明原因。
- 不寫入任何密碼、金鑰、連線字串，也不寫 `.env` 的值。

### 純文字版格式

```
<一句話摘要>

- <檔案或區域>：<改了什麼>
- ...

merge 後的影響：      ← 沒有就省略
- ...

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
```

## 不可以做的事

- 不核准、不 merge、不開 auto-merge、不關閉或刪除 Pull Request。
- 不 force push、不推送到 `main`、不用 `--no-verify` 跳過 pre-push hook（除非使用者明確要求）。
- 檢查失敗時不要「先建 Pull Request 再說」；把失敗原因告訴使用者。
- 不改寫 main 的歷史，就算 main 上的 commit 訊息寫錯了也一樣。

## 疑難排解

| 現象 | 原因與處理 |
|---|---|
| Squash and merge 按鈕是灰的、顯示 merging is blocked | 先看是不是 out-of-date（按 Update branch）或 CI 未完成；都不是時檢查 ruleset「Protect main」的 Allowed merge methods 是否只勾 Squash |
| `git branch -d` 說 not fully merged | Squash 的正常現象，確認無差異後用 `-D` |
| 切換分支被 `components.d.ts` 擋住 | dev server 重寫了檔案；`*.d.ts` 已設 `eol=lf`，若仍發生，內容無差異時 `git restore` 該檔 |
| `gh: command not found` | 尚未安裝：`winget install GitHub.cli`，再 `gh auth login`（由使用者自己執行） |
