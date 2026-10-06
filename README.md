# GameLauncher

LongLongGames PC 官方启动器。依赖 [MP](https://github.com/LongLongGames/MP)（登录/Catalog）与 [LocalCDN](https://github.com/LongLongGames/LocalCDN)（版本与安装包）。

## 启动

```bash
# 依赖（本机）
# MP      → http://localhost:11080
# LocalCDN → http://localhost:12280   （docker compose up -d）

cd src/GameLauncher
dotnet restore
dotnet run
```

## 配置（`appsettings.json`）

| 键 | 含义 | 本地默认 |
|----|------|----------|
| MpBaseUrl | MP 网关 | `http://localhost:11080` |
| CdnBaseUrl | 资源根（建议尾斜杠） | `http://localhost:12280/` |
| AppId / DeviceId | 登录用 | `game_launcher` / `pc-launcher-p0` |
| Platform / Channel | 拼 CDN 路径用 | `windows` / `official` |
| InstallRoot | 游戏库平台根；**空**则取启动器父目录 | `""` |
| InstallLibraryFolder | 库子目录名 | `common` |

换环境只改 URL，路径约定不变：

```json
// 本地
"MpBaseUrl": "http://localhost:11080",
"CdnBaseUrl": "http://localhost:12280/"

// 生产示例
"MpBaseUrl": "https://mp.example.com",
"CdnBaseUrl": "https://cdn.example.com/"
```

## 公共约定

- **安装目录**（类 Steam）：`{InstallRoot}/{InstallLibraryFolder}/{gameId}/`  
  例：启动器在 `E:\LongLongGames\GameLauncher\` 且 InstallRoot 为空 → `E:\LongLongGames\common\match3\`
- **版本权威**：`GET {CdnBaseUrl}config/{gameId}/version.json`（静态文件，不查 SQL）
- **全量包 / 补丁**：manifest 里的 `path` 相对 CDN 根；完整 URL = CdnBaseUrl + path  
  目录约定见 LocalCDN `data/README.md`
- **version.json 由各游戏 Release CI 生成**，Launcher 只消费；脚本见 `scripts/generate-version-json.ps1`

更多：`docs/version.schema.md`、`docs/install-and-cdn.md`、合并说明 `APPLY.md`。
