# GameLauncher

LongLongGames PC 官方启动器。

> 组织总览：[LongLongGames](https://github.com/LongLongGames) · 身份 [MP](https://github.com/LongLongGames/MP) · 资源 [LocalCDN](https://github.com/LongLongGames/LocalCDN)

## 当前能力

- 无边框 WPF 壳 + 系统托盘
- MP 登录（`provider=official`）+ Token 自动登录
- MP Catalog 拉取游戏列表
- **CDN 版本检查 + 安装包 / 补丁下载**（LocalCDN / 生产 CDN 只换 `CdnBaseUrl`）
- 游戏安装路径对齐 Steam 风格：`{InstallRoot}/common/{gameId}/`
- Velopack：启动器自身更新

## 环境依赖

| 服务 | 默认地址 | 说明 |
|------|----------|------|
| MP | `http://localhost:11080` | 登录 + Catalog |
| LocalCDN | `http://localhost:12280` | 版本清单、安装包、补丁 |

本机需已启动 MP 与 LocalCDN：

```bash
cd D:\LongLongGames\LocalCDN
docker compose up -d
curl http://localhost:12280/health
```

## 配置

`src/GameLauncher/appsettings.json`：

| 键 | 说明 | 本地默认 |
|----|------|----------|
| MpBaseUrl | MP 网关 | `http://localhost:11080` |
| CdnBaseUrl | 资源根（建议末尾带 `/`） | `http://localhost:12280/` |
| AppId | 登录 app_id | `game_launcher` |
| DeviceId | 设备标识 | `pc-launcher-p0` |
| Platform | 平台目录名 | `windows` |
| Channel | 渠道目录名 | `official` |
| InstallRoot | 游戏库根目录；空则自动推算 | `""` |
| InstallLibraryFolder | 库子目录名（类比 steamapps） | `common` |

### 安装路径规则（对齐 Steam）

Steam 形态：

```text
{Steam}/steamapps/common/{Game}/
```

本启动器：

```text
{InstallRoot}/common/{gameId}/
  version.txt
  Game.exe
  ...
```

- `InstallRoot` **为空**：取「启动器 exe 所在目录的父目录」作为平台根，再拼 `common`。  
  例：启动器在 `E:\LongLongGames\GameLauncher\GameLauncher.exe`  
  → 游戏在 `E:\LongLongGames\common\match3\`
- `InstallRoot` **有值**：直接使用该绝对路径作为库根（其下仍是 `common/{gameId}`，或若你已把 `InstallRoot` 指到 `...\common` 则见代码注释）。
- 用户可在设置中改库路径（多库后续扩展）。

本地已装版本文件：

```text
{InstallRoot}/common/{gameId}/version.txt
```

## 版本与下载约定

### 权威来源：CDN 静态 JSON（不是 SQL）

Launcher **只** `GET` CDN 上的清单，不直连游戏库、不查 SQL。

| 来源 | 职责 |
|------|------|
| MP Catalog | 游戏列表、展示名、图标；`latest_version` 仅展示兜底 |
| `{CdnBaseUrl}config/{gameId}/version.json` | **安装/更新权威** |
| `{CdnBaseUrl}download/...` | 全量安装包 |
| `{CdnBaseUrl}ab/...` | 增量补丁 |

### version.json 路径

```text
{CdnBaseUrl}config/{gameId}/version.json
```

例：`http://localhost:12280/config/match3/version.json`

### URL 拼装

```text
完整地址 = CdnBaseUrl.TrimEnd('/') + '/' + path.TrimStart('/')
```

`path` 来自 manifest，与 [LocalCDN data 约定](https://github.com/LongLongGames/LocalCDN/blob/main/data/README.md) 一致。

### 客户端决策

1. 拉 `version.json` → `latest` / `packages.full` / `patches[]`
2. 读本地 `version.txt`
3. 无本地或低于 `min_supported` → 全量  
   可升级 → 按 `from→to` 链式补丁  
   已最新 → 可启动

## version.json 生成（自动化，不在 Launcher 内）

由**各游戏 Release / tag CI** 生成并上传，**不是**日常 PR 构建、也不是 Launcher。

推荐：

| 步骤 | 位置 |
|------|------|
| 构建安装包 | `game-match3-client`（或 act）Release |
| 算 patch（可选） | 同 Release job |
| 生成 `version.json` | `scripts/generate-version-json.ps1` |
| 上传到 `download/`、`ab/`、`config/` | 写 LocalCDN `data/` 或生产 OSS |

样例与脚本见本仓库 `scripts/` 与 `docs/version.schema.md`。

## 运行

```bash
cd src/GameLauncher
dotnet restore
dotnet run
```

## 目录结构（本设计相关）

```
src/GameLauncher/
├── Helpers/CdnPaths.cs
├── Helpers/InstallPathResolver.cs
├── Models/VersionModels.cs
├── Services/IVersionService.cs
├── Services/VersionService.cs
├── Services/GameInstallService.cs   # 真实下载骨架（替换纯模拟）
├── appsettings.json
└── ...
scripts/
├── generate-version-json.ps1
└── sample-version-match3.json
docs/
└── version.schema.md
```

## 本地联调清单

1. 启动 MP、LocalCDN  
2. 将 `scripts/sample-version-match3.json` 复制为 LocalCDN  
   `data/config/match3/version.json`  
3. 按 manifest 中 `path` 放入安装包 / 补丁  
4. 启动 Launcher → 登录 → 选游戏 → 应能拉到版本并下载  

## 相关仓库

- [MP](https://github.com/LongLongGames/MP)
- [LocalCDN](https://github.com/LongLongGames/LocalCDN)
- [GameTemplate](https://github.com/LongLongGames/GameTemplate)
- [game-match3-client](https://github.com/LongLongGames/game-match3-client)
