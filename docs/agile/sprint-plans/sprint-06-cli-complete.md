# Sprint 6 — CLI 完善

**周期：** 第 12-13 周（2 周）  
**目标：** 完成 CLI 全部功能，包括服务管理命令、Shell 补全、配置导入导出、前台调试模式

---

## 冲刺目标

CLI 功能达到 v0.9 里程碑，支持服务生命周期管理、配置导入导出、Shell Tab 补全和前台调试模式。

---

## 用户故事

### CLI-007: 服务管理命令（13 点）

> 作为运维，我希望 `localproxy service install/uninstall/start/stop/status` 管理守护进程。

**验收标准：**
- **macOS：** `service install` 创建 LaunchAgent plist → `~/Library/LaunchAgents/com.localproxy.plist`，`service start/stop` 通过 `launchctl` 控制，`service status` 查询运行状态
- **Windows：** `service install` 注册 Windows Service，`service start/stop` 通过 `sc` 控制
- **Linux：** `service install` 创建 systemd unit → `~/.config/systemd/user/localproxy.service`，`service start/stop` 通过 `systemctl --user` 控制
- `service uninstall` 清理所有注册信息
- 需要管理员/sudo 权限时给出明确提示

**技术要点：**
- 抽象 `IServiceManager` 接口
- 三个平台分别实现适配器
- 共享代码最大化（基类 + 平台差异最小化）

### CLI-008: 配置导入导出（5 点）

> 作为用户，我希望 `localproxy config import/export` 导入导出配置。

**验收标准：**
- `config export <file>` 将所有配置导出为 JSON 文件
- `config import <file>` 导入配置（合并模式，默认不覆盖同名配置）
- `--overwrite` 选项覆盖冲突配置
- 导入前校验 JSON 格式

### CLI-009: Shell 自动补全（5 点）

> 作为高级用户，我希望有 bash/zsh/PowerShell 的 Tab 补全。

**验收标准：**
- `localproxy completion bash` 输出 bash 补全脚本
- `localproxy completion zsh` 输出 zsh 补全脚本
- `localproxy completion pwsh` 输出 PowerShell 补全脚本
- 所有命令、子命令、选项、代理名称均可补全
- 补全脚本可由 System.CommandLine 自动生成

### CLI-010: 前台调试模式（3 点）

> 作为用户，我希望 `localproxy start --foreground` 在前台运行。

**验收标准：**
- `start <name> --foreground` 绕过服务，直接在终端运行隧道
- 日志输出到 stdout（非文件）
- Ctrl+C 优雅停止
- 适用于调试或一次性使用场景

### CLI-011: 统一退出码（2 点）

> 作为用户，我希望一致的退出码约定。

**验收标准：**

| 退出码 | 含义 |
|--------|------|
| 0 | 成功 |
| 1 | 一般错误 |
| 2 | 配置错误 |
| 3 | 服务未运行 |

- 所有命令严格遵守此约定
- 退出码在 `--help` 中说明

---

## 交付物

- [ ] 三大平台服务安装/管理（launchd / systemd / Windows Service）
- [ ] 配置导入导出（含合并策略和冲突检测）
- [ ] Shell 补全脚本（bash / zsh / pwsh）
- [ ] 前台调试模式
- [ ] 全生命周期集成测试（CLI 安装服务 → 添加配置 → 启动 → 验证 → 停止 → 卸载）
- [ ] CLI 端到端测试脚本

---

## 完成标准

- [ ] CLI 里程碑 M2：CLI v0.9 可用
- [ ] 服务安装/卸载在三大平台上手动验证通过
- [ ] Shell 补全在三大 Shell 上加载后功能正常
- [ ] 端到端测试脚本通过
