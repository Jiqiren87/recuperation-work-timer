# 休养期间工作计时提醒期

Windows 托盘工具：每次真正开始工作时启动 30 分钟倒计时，提前 60 秒 / 30 秒提醒，归零后锁屏进入休息；休息期间不结束后台自动化，并阻止系统空闲睡眠。解锁后必须手动确认才解除休息。

> 开发中。当前仓库处于 M0/M1 阶段，功能和验收记录会持续更新。

## 核心行为

- 登录、解锁、睡眠恢复后，首次**真实**键盘/鼠标输入启动完整倒计时。
- 默认 30 分钟；设置页可改 1–180 分钟。
- 剩余 60 秒和 30 秒各提醒一次（声音 + 非抢焦点弹窗），可单独关闭。
- 归零调用 Windows 锁屏，进入 Rest；`SetThreadExecutionState` 阻止系统空闲睡眠。
- 解锁后弹出确认框：手动点“结束休息”才开启新周期；15 秒不操作自动重新锁屏。
- 工作途中系统锁屏、睡眠、休眠：倒计时清零，下次解锁并产生真实输入后重新开始。
- 低级键鼠钩子过滤 `LLKHF_INJECTED` / `LLMHF_INJECTED`，自动化模拟输入不算工作。
- 工作/休息记录只写本机 `%APPDATA%\RestWorkTimer\sessions.jsonl`，可查看和导出 CSV，不上传网络。

## 限制

- 真正的系统锁屏会阻断依赖桌面窗口/GUI 点击的自动化；本项目按“后台自动化”场景设计。若自动化必须操作桌面，需要改 `overlay` 模式或把自动化放到独立会话。
- 硬件级 KVM/Arduino 模拟键鼠、部分驱动宏不带 injected 标志，无法与真人输入区分。
- 不提升管理员权限；UAC 安全桌面、登录界面不收全局钩子。
- 防休眠可能被电源策略/电池模式覆盖。

## 构建

需要 .NET 8 SDK（或使用仓库内脚本安装到 `.dotnet/`）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
```

发布单文件：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish.ps1
```

产物：`artifacts\publish\RestWorkTimer.exe`

## 运行

```powershell
dotnet run --project src\RestWorkTimer
```

首次运行会创建 `%APPDATA%\RestWorkTimer\config.json`，并按设置写入 HKCU 开机自启项。

## 隐私

- 不记录键值、窗口标题、进程名、剪贴板、屏幕内容。
- 只记录工作/休息的时间、时长和结束原因。
- GitHub 只用于代码、Issue、PR、CI 和 Release；运行记录不进入仓库。

## 开发管理

- 仓库：<https://github.com/Jiqiren87/recuperation-work-timer>
- 方案：[docs/DEVELOPMENT-PLAN.md](docs/DEVELOPMENT-PLAN.md)
- 测试计划：[docs/TEST-PLAN.md](docs/TEST-PLAN.md)
- CI：GitHub Actions `ci` workflow（Windows，build + test + publish）
