# ADR-0001：主 UI 采用 Avalonia

- 状态：Accepted
- 日期：2026-09-22

## 背景

产品需要全屏透明覆盖层、复杂绘制、多显示器与混合 DPI 支持。待评估的 ShareX 截图模块当前也包含 Avalonia 依赖。如果主界面改用 WinForms + AntdUI，将同时维护两套 UI 生命周期、主题和坐标体系。

## 决策

第一版所有 UI 使用 Avalonia 12，Windows 为首要平台。Core 保持与 Avalonia 无关，为未来替换捕获或增加其他平台保留空间。

## 结果

优点：

- 减少 UI 框架混用；
- 覆盖层、标注画布和主窗口共享渲染技术；
- 保留 macOS/Linux 的长期可能性。

代价：

- 主界面需要自建更完整的视觉主题；
- 托盘、全局快捷键和部分 Windows 行为仍需平台适配；
- Avalonia API 升级必须通过截图交互回归测试。

## 回滚

如果 M1 证明 Avalonia 无法满足 Windows 捕获性能或系统集成要求，保留 Core 接口，将 App 和平台实现替换为 WinForms/AntdUI；领域模型与测试无需重写。

