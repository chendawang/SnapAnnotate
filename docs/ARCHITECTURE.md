# 系统架构

## 设计原则

- 截图能力与 UI 解耦；
- Core 只描述用例和数据，不引用 Avalonia、Win32 或数据库；
- 平台能力通过接口注入；
- 捕获原始图、标注图和最终输出使用明确的数据所有权；
- 所有外部发送动作必须由用户显式触发。

## 目标模块

```text
ScreenshotAssistant.App             Avalonia 壳、窗口和组合根
        │
        ├── ScreenshotAssistant.Core
        │     截图结果和单会话互斥
        │
        ├── ScreenshotAssistant.Capture
        │     ShareX 捕获、热键、输出和历史适配层
        │
        └── ScreenshotAssistant.AI
              AI 目标适配器；仅接收用户确认后的图片
```

当前只保留 App、Core、Capture 和 Core.Tests。Windows 截图基础设施与历史持久化直接复用固定版本的 ShareX，不再维护平行实现。AI 在对应里程碑开始时创建。

主界面采用 PixPin 配置窗口相近的 152 px 左侧导航和单一右侧内容区，窗口外框目标尺寸约为 769×630。快捷截图、截图历史和设置是同一个 `MainWindow` 内的页面；托盘入口只恢复主窗口并切换内容，不创建导航子窗口。截图时由 ShareX 创建的全屏捕获/标注覆盖层属于截图流程，不属于页面导航窗口。

`ScreenshotAssistant.Capture` 是唯一适配边界，直接链接固定版本的 `ShareX.ScreenCaptureLib`、`ShareX.HelpersLib`、`ShareX.Avalonia`、`ShareX.ImageEditor` 与 `ShareX.HistoryLib`。虚拟桌面冻结、自动窗口识别、选区、标注、全局热键、剪贴板、PNG 保存、文件命名、SQLite 历史、单实例通信、启动快捷方式和托盘图标均由 ShareX 提供。App 只负责新主界面、托盘菜单和用例编排，不直接操作 ShareX 类型。

设置模型继承 ShareX `SettingsBase<T>`，使用其临时文件校验、原子替换、普通备份、周备份和损坏回退。设置文件位于 `%LocalAppData%/ScreenshotAssistant/data/Settings.json`，UI 只编辑本产品实际使用的 ShareX 选项，不引入完整 `ApplicationConfig` 或上传器配置。

## 核心边界

`CaptureResult` 表示完成或取消；取消是正常结果，不使用异常表示。`CaptureSessionGate` 防止按钮和快捷键同时创建多个覆盖层。Core 不理解 HWND、Skia、SQLite 或 Avalonia Bitmap。

ShareX `HistoryManagerSQLite` 只保存元数据和文件路径，不把 PNG 二进制写入 SQLite。数据库位于 `%LocalAppData%/ScreenshotAssistant/data/History.db`，图片仍位于 `captures/YYYY/MM`。

`ShareXCaptureDiagnosticsService` 位于 Windows 适配边界内，读取每块显示器的物理边界与有效 DPI，并连续调用 ShareX 全虚拟桌面捕获。诊断只在内存中验证位图尺寸与像素可读性，不保存图片、不写入剪贴板或历史数据库。报告将“通过”和“当前硬件未覆盖”分开，避免在单屏或统一缩放机器上误报多屏/混合 DPI 已验证。

## ShareX 接入策略

1. 上游固定为 commit `f6d7f687673b28b1999382009ce4c0c4e9fa3258`；
2. `eng/sync-sharex.ps1` 仅检出五个相关项目；
3. 不修改上游文件，差异全部位于本项目适配层；
4. ShareX `HotkeyForm` 负责“保存 PNG 并复制”的 `Ctrl+Shift+A` 注册和防连发，主窗口负责单会话互斥；
5. ShareX `ClipboardHelpers`、`ImageHelpers`、`FileHelpers`、`NameParser` 和 `HistoryManagerSQLite` 负责输出链路；
6. ShareX `SingleInstanceManager`、`ShortcutHelpers` 和 `LucideTrayIcon` 分别负责单实例、开机启动快捷方式及 DPI/主题适配托盘图标；
7. 对捕获、取消、连续截图、多屏和混合 DPI 继续执行手工与自动验收；
8. 在分发前完成 GPL 发行决定和对应源码提供方案。

`.vendor/ShareX` 不进入主仓库，由同步脚本按 commit 恢复。发行决定完成前不得发布二进制安装包。

## 数据目录规划

```text
%LocalAppData%/ScreenshotAssistant/
├── captures/YYYY/MM/
├── data/History.db
├── logs/
└── temp/
```

临时文件使用唯一名称并在成功提交或取消后清理。
