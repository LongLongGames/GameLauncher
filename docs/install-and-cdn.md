# 安装路径与 CDN 下载

## 安装路径

对齐 Steam：`{平台根}/common/{gameId}/`。

| InstallRoot | 结果 |
|-------------|------|
| 空 | 启动器 exe 父目录 + `InstallLibraryFolder`（默认 `common`） |
| 有值 | 展开环境变量后作为平台根，再拼 library 文件夹 |

本地版本文件：`{库根}/{gameId}/version.txt`。

## 版本与下载

1. Catalog（MP）只负责列表与展示；`latest_version` 仅兜底。
2. 进游戏详情 / 检查更新：`GET {CdnBaseUrl}config/{gameId}/version.json`。
3. 与本地 `version.txt` 比较：
   - 无本地或低于 `min_supported` → `packages.full`
   - 有补丁链 → 按 `from→to` 依次下 `patches[]`
   - 已最新 → 可启动
4. 下载文件 URL = `CdnBaseUrl` + manifest 中的 `path`。

## version.json 谁生成

各游戏 **Release / tag CI**（如 game-match3-client 发版流水线），调用 `scripts/generate-version-json.ps1`，上传到 LocalCDN `data/config/` 与 `download/`、`ab/`。日常 PR 不要覆盖正式清单。

字段说明见 `version.schema.md`。
