# 本机安装

## 标准安装包

仓库提供 Inno Setup 6 打包脚本，可生成当前用户级、无需管理员权限的单文件安装程序：

```powershell
.\eng\package-windows.ps1
```

输出文件位于 `dist\ScreenshotAssistant-Setup-0.1.0-win-x64.exe`，对应 SHA-256 校验值写入 `dist\SHA256SUMS.txt`。安装程序会创建开始菜单和可选桌面快捷方式，并在 Windows“已安装的应用”中注册标准卸载入口。

升级安装不会删除 `%LOCALAPPDATA%\ScreenshotAssistant` 中的截图、历史和设置数据。

## 发布

在项目根目录执行：

```powershell
dotnet publish src/ScreenshotAssistant.App/ScreenshotAssistant.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:DebugSymbols=false `
  -p:DebugType=None `
  -o .artifacts/publish/win-x64
```

这是包含 .NET 运行时的 Windows x64 版本，不要求目标电脑预装 .NET。

## 安装

```powershell
./eng/install-local.ps1 -PublishDirectory ./.artifacts/publish/win-x64
```

默认安装到 `%LOCALAPPDATA%\Programs\ScreenshotAssistant`，并创建：

- 开始菜单快捷方式；
- 桌面快捷方式；
- 当前用户“已安装的应用”卸载登记。

添加 `-NoDesktopShortcut` 可不创建桌面快捷方式。

## 卸载

可从 Windows“已安装的应用”中卸载。默认保留 `%LOCALAPPDATA%\ScreenshotAssistant` 中的截图、设置和历史数据库。

如需连同用户数据一起删除，可在安装目录执行：

```powershell
./Uninstall.ps1 -RemoveUserData
```

## 分发限制

当前版本直接链接固定版本的 ShareX 源码，仅用于本地安装与验证。对外分发前必须按 `OPEN_SOURCE_COMPLIANCE.md` 完成 GPL 对应源码和许可证方案。
