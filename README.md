# GameLauncher (P0)

LongLongGames PC 官方启动器 — **P0 最小可用版**。

> 组织总览：[LongLongGames](https://github.com/LongLongGames) · 身份走 [MP](https://github.com/LongLongGames/MP)

## P0 范围

- 自定义无边框 WPF 壳（无 Windows 系统标题栏 / 最小化 / 最大化 / 关闭按钮）
- 底部状态栏：应用图标、显示/隐藏（托盘）、退出
- 启动默认登录页，走 MP `POST /api/v1/auth/login`（provider=`official`）
- 登录后主界面：从 MP Catalog 拉取官方注册游戏列表（图标 Tab）
- 切换游戏后展示：当前版本、购买/下载/更新状态（P0 为本地模拟状态机）

## 环境

- Windows 10/11
- .NET 8 SDK
- 本机已启动 MP（默认 `http://localhost:11080`）

## 运行

```bash
cd src/GameLauncher
dotnet restore
dotnet run
```

或打开根目录 `GameLauncher.sln` 用 Visual Studio 2022。

## 配置

`appsettings.json`：

| 键 | 说明 | 默认 |
|----|------|------|
| MpBaseUrl | MP 网关 | http://localhost:11080 |
| AppId | 登录 app_id | game_launcher |
| DeviceId | 设备标识 | pc-launcher-p0 |

## MP 登录示例

```bash
curl -s -X POST http://localhost:11080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{
    "provider": "official",
    "app_id": "game_launcher",
    "device_id": "pc-launcher-p0",
    "auth_payload": { "username": "tester1", "password": "test1234" }
  }'
```

## 目录结构

```
src/GameLauncher/
├── App.xaml / App.xaml.cs          # DI、启动
├── MainWindow.xaml                 # 无边框壳 + 状态栏
├── Views/                          # LoginView, MainView, GameDetailView
├── ViewModels/
├── Services/                       # MpAuthService, CatalogService, GameInstallService
├── Models/
├── appsettings.json
└── Assets/
```

## 后续（非 P0）

- 真实增量补丁 / version-check 对接 game-core
- 购买与 entitlement
- Steam / 其他渠道登录
- 系统托盘完整菜单、自动更新启动器自身
