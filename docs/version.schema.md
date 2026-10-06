# config/{game}/version.json 约定

权威版本清单，由游戏 **Release CI** 生成，Launcher 只读。

## 路径

```
{CdnBaseUrl}config/{gameId}/version.json
```

## 字段

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| game_id | string | 是 | 与 MP Catalog、目录名一致 |
| platform | string | 是 | `windows` / `osx` / … |
| channel | string | 是 | `official` / `steam` / … |
| latest | string | 是 | 当前对外最新版本（SemVer 字符串即可） |
| min_supported | string | 否 | 低于此版本强制全量；缺省则不强制 |
| packages.full | object | 是* | 全量包；首次安装必用 |
| packages.full.version | string | 是 | 通常等于 latest |
| packages.full.path | string | 是 | 相对 CDN 根，如 `download/match3/windows/official/Game_Setup_1.0.2.exe` |
| packages.full.size | number | 建议 | 字节 |
| packages.full.sha256 | string | 建议 | 小写 hex |
| patches | array | 否 | 增量链 |
| patches[].from | string | 是 | 起始版本 |
| patches[].to | string | 是 | 目标版本 |
| patches[].path | string | 是 | 相对 CDN 根 |
| patches[].size | number | 建议 | |
| patches[].sha256 | string | 建议 | |
| exe_relative | string | 建议 | 安装目录内启动相对路径，如 `Game.exe` |
| changelog | string | 否 | 展示用 |

\* 若某渠道只发补丁、不提供全量，可省略 full，但 Launcher 对「未安装」用户将无法安装。

## 示例

见 `scripts/sample-version-match3.json`。

## 生成

```powershell
pwsh scripts/generate-version-json.ps1 `
  -GameId match3 `
  -Version 1.0.2 `
  -Platform windows `
  -Channel official `
  -FullPackagePath "D:\build\Game_Setup_1.0.2.exe" `
  -FullCdnRelativePath "download/match3/windows/official/Game_Setup_1.0.2.exe" `
  -OutFile "D:\LongLongGames\LocalCDN\data\config\match3\version.json"
```
