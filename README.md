# 休养期间工作计时提醒期

[![ci](https://github.com/Jiqiren87/recuperation-work-timer/actions/workflows/ci.yml/badge.svg)](https://github.com/Jiqiren87/recuperation-work-timer/actions/workflows/ci.yml)

Windows 托盘工具：每次真正开始工作时启动 30 分钟倒计时，提前 60 秒 / 30 秒提醒，归零后锁屏进入休息；休息期间不结束后台自动化，并阻止系统空闲睡眠。解锁后必须手动确认才解除休息。

> 状态：v0.1.2 **已发布**；25 个单元测试、CI 与真机核心链路验收通过。
>
> 下载：<https://github.com/Jiqiren87/recuperation-work-timer/releases/tag/v0.1.2>

![主窗口](docs/images/main.png)

## 核心行为

- 登录、解锁、睡眠恢复后，首次**真实**键盘/鼠标输入启动完整倒计时。
- 默认 30 分钟；设置页可改 1–180 分钟。
- 剩余 60 秒和 30 秒各提醒一次：**全屏置顶大字倒计时 + 脉冲边框 + 双音/三音提示音**。
- 检测到全屏游戏/全屏应用时，先最小化当前窗口再显示提醒，保证一定可见；可在设置中关闭“全屏应用时强制切出”。
- 全屏提醒点击穿透、自动关闭，游戏内点击不会误关。
- 归零调用 Windows 锁屏，进入 Rest；`SetThreadExecutionState` 阻止系统空闲睡眠。
- 解锁后弹出确认框：手动点“结束休息”才开启新周期；15 秒不操作自动重新锁屏。
- 工作途中系统锁屏、睡眠、休眠：倒计时清零，下次解锁并产生真实输入后重新开始。
- 低级键鼠钩子过滤 `LLKHF_INJECTED` / `LLMHF_INJECTED`，自动化模拟输入不算工作。
- 工作/休息记录只写本机 `%APPDATA%\RestWorkTimer\sessions.jsonl`，可查看和导出 CSV，不上传网络。
- 自动启动后只保留托盘图标；主窗口、历史记录和休息确认框不在任务栏显示。双击托盘打开主窗口，关闭后回到托盘。

## 限制

- 真正的系统锁屏会阻断依赖桌面窗口/GUI 点击的自动化；本项目按“后台自动化”场景设计。`overlay` 模式尚未实现，GUI 自动化建议放到独立会话或服务中。
- 硬件级 KVM/Arduino 模拟键鼠、部分驱动宏不带 injected 标志，无法与真人输入区分。
- 不提升管理员权限；UAC 安全桌面、登录界面不收全局钩子。
- 防休眠可能被电源策略/电池模式覆盖。

## 安装

从 Release 下载 `RestWorkTimer.exe`，或在本机执行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

脚本会复制到 `%LOCALAPPDATA%\Programs\RestWorkTimer\`，创建开始菜单快捷方式并启动。卸载：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\uninstall.ps1
```

首次运行会创建 `%APPDATA%\RestWorkTimer\config.json`，并按设置写入 HKCU 开机自启项。

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
- 验收记录：[docs/ACCEPTANCE.md](docs/ACCEPTANCE.md)
- CI：GitHub Actions `ci` workflow（Windows，build + test + publish）
- 版本：v0.1.0（<https://github.com/Jiqiren87/recuperation-work-timer/releases/tag/v0.1.0>）
