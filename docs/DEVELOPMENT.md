# 开发指南

## 环境

- Windows 10/11 x64；
- .NET SDK 10.0.300；
- Visual Studio 2026、Rider 或 VS Code；
- 当前固定 Avalonia 12.0.5。

## 常用命令

```powershell
$env:AVALONIA_TELEMETRY_OPTOUT = "1"
dotnet restore ScreenshotAssistant.slnx
dotnet build ScreenshotAssistant.slnx --no-restore
dotnet test ScreenshotAssistant.slnx --no-build
dotnet run --project src/ScreenshotAssistant.App/ScreenshotAssistant.App.csproj
```

发布命令将在 M4 确定运行时、单文件和自包含策略后补充。

## 分支与评审

- 功能分支使用 `feature/<short-name>`；
- 修复分支使用 `fix/<short-name>`；
- PR 必须说明行为变化、验证证据、风险和回滚；
- 截图交互变化应附短视频或连续截图；
- 涉及第三方源码必须完成许可证检查。

## 配置和密钥

- 本地设置写入 `%LocalAppData%/ScreenshotAssistant`；
- 开发默认值可以入库，真实 API Key 不得入库；
- AI 凭据后续通过 Windows Credential Manager 或等价安全存储保存。
