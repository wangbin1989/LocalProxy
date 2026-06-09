# Sprint 5 — CLI 核心命令

**周期：** 第 10-11 周（2 周）  
**目标：** 实现 CLI 命令行工具的核心管理命令，通过 IPC 与服务通信

---

## 冲刺目标

实现 `LocalProxy.Cli` 项目，提供代理配置管理和隧道控制的命令行界面，通过命名管道 IPC 与后台服务通信。

---

## 用户故事

### CLI-001: config add（5 点）

> 作为用户，我希望 `localproxy config add` 创建代理配置。

**验收标准：**
```bash
localproxy config add --name "my-redis" --proto tcp \
  --local-port 6379 --remote-host redis.example.com --remote-port 6379
```
- 命令成功后将配置写入 `config.json`
- `localproxy config list` 显示新配置
- 参数校验：必填字段缺失时给出明确提示

### CLI-002: config remove / list（3 点）

> 作为用户，我希望 `localproxy config remove/list` 管理配置。

**验收标准：**
- `config list` 以表格展示所有配置（名称、协议、本地端口、远程地址、状态）
- `config remove <name>` 删除指定配置，需确认（`--force` 跳过确认）
- `--output json` 输出 JSON 格式

### CLI-003: start / stop（5 点）

> 作为用户，我希望 `localproxy start/stop` 控制隧道。

**验收标准：**
- `localproxy start <name>` 启动指定隧道（通过 IPC 通知服务）
- `localproxy stop <name>` 停止指定隧道
- 启动后可通过连接本地端口验证
- 服务未运行时给出明确错误："服务未运行，请先执行 localproxy service start"

### CLI-004: status（5 点）

> 作为用户，我希望 `localproxy status` 显示实时统计。

**验收标准：**
- 表格输出列：Name, Protocol, Local, Remote, Status, Uptime, Bytes In/Out
- 运行中的隧道状态为绿色"Running"，停止的为灰色"Stopped"
- `--output json` 输出机器可读 JSON 数组
- 不加参数时显示所有隧道

### CLI-005: logs --follow（3 点）

> 作为用户，我希望 `localproxy logs --follow` 实时跟踪日志。

**验收标准：**
- `logs` 显示最近 50 条日志
- `logs --follow` 持续输出新日志直到 Ctrl+C
- `logs --level Error` 按级别过滤

### CLI-006: --help（2 点）

> 作为用户，我希望 `localproxy --help` 和各命令有完整帮助。

**验收标准：**
- 每个命令和子命令有 `--help` 输出（由 System.CommandLine 自动生成）
- 帮助中包含使用示例
- 覆盖所有命令和选项

### CLI-012: --output json（3 点）

> 作为用户，我希望 `--output json` 输出机器可读格式。

**验收标准：**
- `status --output json` 输出 JSON 数组
- `config list --output json` 输出 JSON 数组
- 无 `--output` 时默认表格/文本格式

---

## 交付物

- [ ] `LocalProxy.Cli` 项目（System.CommandLine + 源生成器）
- [ ] 7 个命令全部实现并测试
- [ ] IPC 客户端（通过 `LocalProxy.Ipc` 连接服务管道）
- [ ] 服务未运行时的友好错误提示
- [ ] CLI 帮助文档（嵌入二进制，`--help` 可查看）

---

## 完成标准

- [ ] 所有 CLI 命令在 macOS + Windows + Linux 上手动测试通过
- [ ] IPC 与服务联调：启动服务 → CLI status 正确显示 → start/stop 控制隧道
- [ ] `--help` 全部命令帮助信息完整
