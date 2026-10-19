# v0.1.1 — 全屏醒目提醒

这是针对“全屏操作时看不到提醒”的增强版本。

## 变化

- 60 秒 / 30 秒提醒改为**全屏置顶**深色提醒：
  - 超大倒计时（01:00 / 00:30）
  - 红色/橙色脉冲边框
  - 全屏遮罩 + 居中提示卡片
- 提示音升级：60 秒双音重复 2 次，30 秒三音重复 3 次。
- 检测到全屏游戏/全屏应用时，**先最小化当前全屏窗口**，再强制显示提醒，保证一定可见。
- 全屏提醒点击穿透、4 秒后自动关闭，游戏内点击不会误关。
- 设置页新增“全屏应用时强制切出以显示提醒”，默认开启；不需要时可关闭。
- 版本号升级到 0.1.1；其余计时、锁屏、休息解除、本地记录逻辑不变。

## 权衡

“强制切出”会把全屏游戏/全屏播放器最小化。这是为了保证提醒一定可见；如果不想打断全屏应用，请在设置中取消该选项。取消后仍会显示置顶全屏提醒并播放声音，但部分独占全屏程序可能盖住提醒。

## 验证

- 25 个单元测试通过；CI build / test / publish 通过。
- 真机：1 分钟周期，30 秒提醒在全屏游戏上显示，日志 `forceForeground=true`；提醒覆盖游戏画面。
- 截图见 `docs/images/reminder-fullscreen.png`。
- 其余 v0.1.0 验收项（真实锁屏、解锁确认、手动解除、锁屏清零、睡眠恢复、自启、单实例、本地记录）继续有效，见 `docs/ACCEPTANCE.md`。

## 安装

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

或直接运行 Release 中的 `RestWorkTimer.exe`（需要 .NET 8 Desktop Runtime）。

## 校验

```powershell
Get-FileHash .\RestWorkTimer.exe -Algorithm SHA256
```

哈希见同目录 `RestWorkTimer.exe.sha256`。
