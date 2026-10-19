# v0.1.0 — 休养期间工作计时提醒期

Windows 托盘工具，面向“后台自动化继续运行、人需要按时休息”的场景。

## 功能

- 登录/解锁/睡眠恢复后，首次**真实**键鼠输入启动 30 分钟倒计时。
- 剩余 60 秒、30 秒各提醒一次；声音 + 右下角非抢焦点弹窗。
- 归零锁屏进入 Rest；Rest 期间阻止系统空闲睡眠，不结束后台自动化进程。
- 解锁后必须手动点“结束休息”才开启新周期；15 秒不操作自动重新锁屏。
- 工作途中系统锁屏/睡眠/休眠：倒计时清零，下次工作重新开始。
- 过滤 `SendInput`/AutoHotkey 等注入输入，自动化不触发计时。
- GUI 设置工作时长（1–180 分钟）、60/30 秒提醒开关、声音、锁屏、防休眠、自启。
- 工作/休息记录只存本机 `%APPDATA%\RestWorkTimer\sessions.jsonl`，可查看、导出 CSV。

## 安装

1. 下载 `RestWorkTimer.exe`。
2. 放到固定目录（例如 `%LOCALAPPDATA%\Programs\RestWorkTimer\`）。
3. 双击运行；首次运行会创建配置并写入 HKCU 开机自启项。
4. 托盘右键可打开主窗口、设置、历史记录或退出。

需要 .NET 8 Desktop Runtime（Windows 11 通常已带；如缺失请安装 .NET 8 Desktop Runtime x64）。

## 已知限制

- 真正的系统锁屏会阻断依赖桌面窗口/GUI 点击的自动化；本项目按后台任务设计。
- 硬件级 KVM/Arduino 模拟输入无法与真人输入区分。
- 普通权限下无法接收 UAC 安全桌面/登录界面的输入。
- 防休眠可能被电源策略或电池模式覆盖。

## 验证状态

- 单元测试 25/25 通过；CI build/test/publish 通过。
- 本机验证：计时启动、60/30 秒提醒、归零进入 Rest（锁屏关闭模式）、本地记录。
- **待验收**：真实锁屏/解锁、休息确认框、防休眠、注入输入、睡眠恢复。详见 `docs/ACCEPTANCE.md`。

## 校验

```powershell
Get-FileHash .\RestWorkTimer.exe -Algorithm SHA256
```

哈希见同目录 `RestWorkTimer.exe.sha256`。
