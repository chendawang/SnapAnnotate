# 参与贡献

感谢参与 SnapAnnotate。提交修改前请先搜索现有 Issue，较大的功能建议先创建 Issue 说明使用场景和交互方案。

## 开发流程

1. Fork 仓库并从最新主分支创建功能分支；
2. 运行 `./eng/sync-sharex.ps1` 获取固定版本的 ShareX 源码并应用项目补丁；
3. 遵循 `docs/CODING_STANDARDS.md`，保持 Core 层不依赖 Avalonia、Win32 或文件系统实现；
4. 为行为改动补充测试；
5. 运行 `dotnet build ScreenshotAssistant.slnx` 和 `dotnet test ScreenshotAssistant.slnx`；
6. 提交 Pull Request，说明变更、验证结果、风险和界面截图。

不要提交 `.vendor/`、`.artifacts/`、`dist/`、用户截图、数据库、密钥或本机配置。修改 ShareX 源码时，应新增或更新 `patches/sharex` 中的补丁，并同步更新 `docs/OPEN_SOURCE_COMPLIANCE.md`。

提交贡献即表示你有权提供该代码，并同意按本项目 GNU GPL v3 or later 许可证发布。
