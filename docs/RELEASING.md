# 发布流程

## 版本准备

1. 将 App 项目、安装脚本和命令中的版本更新为同一个语义化版本；
2. 将 `CHANGELOG.md` 的 Unreleased 内容归入新版本和发布日期；
3. 运行 `./eng/sync-sharex.ps1`，确认依赖 commit 和补丁状态；
4. 运行 `dotnet build ScreenshotAssistant.slnx -c Release`；
5. 运行 `dotnet test ScreenshotAssistant.slnx -c Release --no-build`；
6. 在 Windows 10/11 实机验收快捷键、多屏、混合 DPI、连续截图、托盘和安装/卸载。

## 创建 Release

1. 合并发布改动后创建带 `v` 前缀的标签，例如 `v0.2.0`；
2. 推送标签，GitHub `release.yml` 会构建安装包并创建草稿 Release；
3. 下载并安装草稿附件，验证版本、快捷方式、卸载入口和 SHA-256；
4. 将对应版本的更新日志复制到 Release Notes；
5. 确认源码归档、`LICENSE`、ShareX 许可证和对应源码补丁均可访问后发布草稿。

GitHub 的 latest release 接口不返回草稿或预发布版本。因此只有正式发布后，客户端“检查更新”才会提示该版本。

## 回滚

发现严重问题时，应撤下有问题的安装附件并在 Release 页面明确标记，不复用已有版本号。修复后增加补丁版本并重新发布；已经公开的 Git 标签不得移动。
