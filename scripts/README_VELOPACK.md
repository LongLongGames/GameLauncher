# Velopack (vpk) 集成说明

Squirrel 的现代继任者。CLI 叫 **`vpk`**，包名 **Velopack**。

## 已改动的代码

| 文件 | 作用 |
|------|------|
| `src/GameLauncher/Program.cs` | 显式 `Main`，**最先**调用 `VelopackApp.Build().Run()` |
| `src/GameLauncher/GameLauncher.csproj` | 加 `Velopack` NuGet、`StartupObject` |
| `src/GameLauncher/Services/UpdateService.cs` | 从本仓库 GitHub Releases 检查/下载/应用更新 |
| `src/GameLauncher/App.xaml.cs` | DI 注册 `IUpdateService` |
| `.github/workflows/release.yml` | **tag `v*`** 触发：publish → `vpk pack` → `vpk upload github` |
| `scripts/pack.ps1` | 本地打包 |

**PackId**（全局唯一）：`LongLongGames.GameLauncher`  
**主程序**：`GameLauncher.exe`

## 发布流程

```bash
git tag v0.1.0
git push origin v0.1.0
```

Actions 会生成并上传：

- `LongLongGames.GameLauncher-*-full.nupkg`（完整包）
- `*-delta.nupkg`（有上一版时）
- `*-Setup.exe`（安装器）
- `*-Portable.zip`
- `releases.win.json` / `RELEASES`（客户端更新用）

## 本地打包

```powershell
dotnet tool install -g vpk --version 0.0.1298
.\scripts\pack.ps1 -Version 0.1.0
# 可选直接上传：
.\scripts\pack.ps1 -Version 0.1.0 -Upload
```

产物目录：`artifacts/Releases/`

## 客户端更新

`UpdateService` 已用 `GithubSource` 指向本仓库 Releases。  
可在主界面或启动后调用：

```csharp
await App.Services.GetRequiredService<IUpdateService>().CheckAndUpdateAsync();
```

从 VS 直接 `F5` 跑时 `IsInstalled == false`，会自动跳过，不报错。

## 注意

- 版本必须是 **semver**（`1.0.0`），不要用四段 `1.0.0.0`。
- self-contained 发布，**不要**再给 vpk 加 `--framework net*-desktop`。
- Velopack NuGet 与全局 `vpk` 工具版本尽量一致（当前示例 `0.0.1298`，可按需升级）。
