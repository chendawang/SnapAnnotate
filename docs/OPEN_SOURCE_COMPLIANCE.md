# 开源合规

## 发行模式

SnapAnnotate 直接链接 ShareX 源项目，因此按 GPL 衍生作品发布。项目源码、构建脚本和对应的 ShareX 补丁一并通过 GitHub 提供，整体采用 GNU GPL v3 or later。

| 组件 | 用途 | 许可证 | 处理方式 |
|---|---|---|---|
| Avalonia | UI 与桌面运行时 | MIT | NuGet 依赖，保留包元数据和许可证 |
| xUnit.net | 单元测试 | Apache-2.0 | 仅开发和测试使用 |
| ShareX | 捕获、选区、标注、热键、剪贴板、保存和历史 | GPL v3 | 固定源码版本、保留许可证并公开补丁 |

具体 NuGet 版本由 `Directory.Packages.props` 锁定。

## ShareX 对应源码

- 上游：`https://github.com/ShareX/ShareX`
- 固定 commit：`f6d7f687673b28b1999382009ce4c0c4e9fa3258`
- 同步脚本：`eng/sync-sharex.ps1`
- 使用项目：`ShareX.ScreenCaptureLib`、`ShareX.HelpersLib`、`ShareX.Avalonia`、`ShareX.ImageEditor`、`ShareX.HistoryLib`
- 上游许可证：构建和发布输出包含 `licenses/ShareX-LICENSE.txt`

本仓库不提交 `.vendor/ShareX` 工作副本。同步脚本检出固定 commit 后，自动应用 `patches/sharex` 中的补丁，因此公开仓库包含重建发行程序所需的完整修改信息。

### 修改清单

`patches/sharex/0001-custom-selection-border.patch` 修改：

- `ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionSelectionOverlay.cs`
- 为选区边框增加可配置的线宽和虚线样式；
- 用配置边框替代固定的蚂蚁线绘制。

其余集成代码位于本仓库的 `ScreenshotAssistant.Capture` 和 App 工作流中。

## 发布检查

- Release 必须提供本项目源代码或指向同版本公开源代码；
- Release 必须包含 GNU GPL 和 ShareX 许可证文本；
- ShareX commit、补丁及构建脚本必须能重建对应二进制；
- 不得移除第三方版权或许可证头；
- 不得加入来源或许可证不明确的图标、字体和图片。

如果未来需要闭源分发，必须停止直接链接 ShareX，完成独立实现或取得兼容授权，并在发布前进行法律审查。
