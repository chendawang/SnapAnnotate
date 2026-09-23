# SnapAnnotate（截图助手）

SnapAnnotate 是一款面向 Windows 的开源截图与标注工具。它提供窗口智能识别、区域截图、屏上标注、剪贴板、本地 PNG 保存、截图记录、全局快捷键、托盘和悬浮工具。

> 当前版本：`0.1.0`。项目处于早期公开测试阶段，请在正式工作流中使用前自行验证。

[English](README.en.md) · [下载安装](https://github.com/chendawang/SnapAnnotate/releases) · [反馈问题](https://github.com/chendawang/SnapAnnotate/issues)

## 功能

- `Ctrl+Shift+A` 冻结屏幕并开始区域截图；
- 自动识别窗口和窗口内控件；
- 多显示器选区和混合 DPI 坐标处理；
- 箭头、矩形、画笔、文字等屏上标注；
- 保存 PNG 时自动复制到剪贴板；
- 按日期展示本地截图记录，支持多选复制和删除；
- 可配置截图框样式、颜色、粗细、保存目录和文件名；
- 托盘、单实例、开机启动和可移动悬浮工具；
- 在设置页通过 GitHub Releases 手动检查新版本。

所有截图、配置和历史记录默认只保存在本机。SnapAnnotate 不包含 AI 上传功能，也不会自动上传截图。

## 安装

从 [GitHub Releases](https://github.com/chendawang/SnapAnnotate/releases) 下载最新的 `ScreenshotAssistant-Setup-*-win-x64.exe`，运行后按向导安装。

要求：Windows 10/11 x64。安装包为自包含发布，不要求单独安装 .NET Runtime。

## 使用

1. 按 `Ctrl+Shift+A`，或点击主界面、托盘、悬浮工具中的截图按钮；
2. 单击智能识别的窗口，或拖动创建选区；
3. 调整选区并使用底部工具栏标注；
4. 点击绿色对勾保存 PNG 并复制到剪贴板，或点击剪贴板按钮仅复制；
5. 在“截图记录”中查看、复制、打开或删除本地截图。

快捷键、保存目录和截图框外观可在“设置”中修改。

## 从源码构建

需要 Windows 10/11、Git 和 .NET SDK 10.0.300 或兼容 Feature Band。

```powershell
$env:AVALONIA_TELEMETRY_OPTOUT = "1"
./eng/sync-sharex.ps1
dotnet restore ScreenshotAssistant.slnx
dotnet build ScreenshotAssistant.slnx --no-restore
dotnet test ScreenshotAssistant.slnx --no-build
dotnet run --project src/ScreenshotAssistant.App/ScreenshotAssistant.App.csproj
```

生成 Windows 安装包还需要 Inno Setup 6：

```powershell
./eng/package-windows.ps1 -Version 0.1.0
```

安装包和校验文件输出到 `dist/`，这些文件不提交到 Git，应上传到对应的 GitHub Release。

## 项目结构

- `src/ScreenshotAssistant.App`：Avalonia 主界面和应用生命周期；
- `src/ScreenshotAssistant.Capture`：ShareX 适配、截图、历史和平台服务；
- `src/ScreenshotAssistant.Core`：不依赖 UI/Win32 的核心模型；
- `patches/sharex`：应用到固定 ShareX 源码版本的项目补丁；
- `tests`：核心及截图适配层单元测试；
- `eng`、`installer`：依赖同步、打包和安装脚本。

更多内容见[开发指南](docs/DEVELOPMENT.md)、[架构说明](docs/ARCHITECTURE.md)、[发布流程](docs/RELEASING.md)和[隐私说明](docs/PRIVACY.md)。

## 开源与第三方代码

SnapAnnotate 直接链接固定版本的 ShareX 源码并包含少量公开补丁，因此项目整体以 **GNU GPL v3 or later** 发布。完整许可证见 [LICENSE](LICENSE)，ShareX 来源、固定 commit 和修改清单见[开源合规说明](docs/OPEN_SOURCE_COMPLIANCE.md)。

项目与 ShareX、PixPin、微信没有隶属或官方合作关系；界面中的产品名称仅用于说明交互参考。

## 参与贡献

提交代码前请阅读 [CONTRIBUTING.md](CONTRIBUTING.md)。安全问题请按 [SECURITY.md](SECURITY.md) 私下报告。
