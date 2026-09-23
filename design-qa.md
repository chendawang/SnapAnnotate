# Desktop floating toolbar design QA

## Evidence

- Source visual truth: `C:\Users\cdw\AppData\Local\Temp\codex-clipboard-98c3a65c-878b-450b-aeac-a9413f783e01.png`
- Source dimensions: 432 x 192 px; referenced floating pill is approximately 312 x 72 px.
- Implementation: `src/ScreenshotAssistant.App/FloatingToolbarWindow.axaml`
- Implementation logical size: 190 x 68 Avalonia pixels, rendered at the active monitor DPI.
- State: light theme, desktop toolbar visible, main window hidden.
- Implementation screenshot path: unavailable. The Windows capture helper excludes this transparent, non-taskbar tool window from its targetable-window list.

## Full-view comparison

The reference was opened and inspected. The implementation uses the same core composition: a white rounded horizontal pill, subtle elevation, equal icon areas, dark line icons, separators, and a close action at the far right. The implementation intentionally replaces the reference product logo and unrelated pin/timer tools with the requested capture, main-window, and close actions.

## Focused-region comparison

Blocked for the floating pill because the implementation window cannot be captured by the available desktop automation surface. The surrounding settings and quick-capture screens were visually inspected: checkbox outlines and checked states are visible, the new setting appears in the System section, and the quick-capture card appears beside Screenshot to File.

## Functional checks

- Settings checkbox toggles and persists.
- Quick-capture entry enables the toolbar and hides the main window.
- Build passed with 0 warnings and 0 errors.
- Automated tests passed: 24/24.
- Capture-start hide, capture-finish restore, tray toggle, main-window action, and toolbar-close state synchronization are wired in code but require manual desktop interaction acceptance.

## Findings

- [P2] Native floating-toolbar pixels could not be captured for a normalized visual comparison.
  - Location: `FloatingToolbarWindow`.
  - Evidence: the window is intentionally transparent, topmost, and excluded from the taskbar; the capture helper returned the main window only.
  - Impact: spacing, shadow softness, and DPI rendering cannot be signed off automatically.
  - Fix: manually inspect the currently visible toolbar on each required DPI, or use a native desktop capture source that includes tool windows.

## Implementation checklist

- Manually verify the three icons at 100%, 125%, and 150% DPI.
- Verify capture hides the toolbar and restores it after completion or cancellation.
- Verify the close icon clears both the Settings checkbox and tray-menu check state.

## Follow-up polish

- Fine-tune pill width and shadow after manual comparison if the user wants a closer pixel match to the reference.

final result: blocked
