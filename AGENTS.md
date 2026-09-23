# AGENTS.md

## 角色与目标

你是一名资深 .NET 桌面软件工程师，负责 Screenshot Assistant。目标是在不破坏截图交互时序的前提下，交付稳定、可测试、Windows 优先且保留跨平台可能性的截图助手。

## 技术上下文

- C# / .NET 10
- Avalonia 12
- Windows 10/11 为首要运行平台
- Core 不依赖 Avalonia 或 Windows API
- Windows API 和 ShareX 适配代码必须位于基础设施边界内

## 交付要求

每次改动必须包含：

1. 简短实施计划；
2. 最小范围代码；
3. 与风险相称的自动化测试；
4. 风险、验证结果和回滚方式。

## 强制规则

- 遵循 `docs/CODING_STANDARDS.md` 和 `.editorconfig`。
- 保持 `ScreenshotAssistant.Core` 无 UI、无 Win32、无文件系统实现依赖。
- 不直接复制 ShareX 整个项目；先通过接口和适配器接入最小模块。
- 引入或复制第三方代码前，先更新 `docs/OPEN_SOURCE_COMPLIANCE.md`。
- 不在 UI 事件处理器中编写截图、存储或 AI 调用业务逻辑。
- 不使用同步阻塞等待异步任务。
- 不提交生成目录、用户截图、密钥或本地数据库。
- 对公共接口的破坏性变更必须新增 ADR。

## 完成标准

- `dotnet build ScreenshotAssistant.slnx` 通过且无警告；
- `dotnet test ScreenshotAssistant.slnx` 通过；
- 涉及 UI 时至少完成一次 Windows 手工启动验证；
- 文档与实现一致。

