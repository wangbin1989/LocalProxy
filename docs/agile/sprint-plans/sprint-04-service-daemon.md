# Sprint 4 — 服务宿主与守护进程

**周期：** 第 8-9 周（2 周）  
**目标：** 实现后台服务宿主，支持健康监控、自动重启和 IPC 管理接口

---

## 冲刺目标

实现 `LocalProxy.Service` 和 `LocalProxy.Ipc` 项目，将代理引擎包装为基于 Generic Host 的长时间运行后台服务，支持隧道健康监控、故障自动重启、命名管道 IPC 通信和文件日志。

---

## 用户故事

### SVC-001: 后台服务运行（8 点）

> 作为运维，我希望代理引擎作为后台服务运行。

**验收标准：**
- `localproxy service start` 启动服务，在后台运行
- 关闭终端后服务继续运行
- `localproxy service stop` 停止服务
- 基于 `Microsoft.Extensions.Hosting.GenericHost`

**技术要点：**
- `IHostedService` 实现，管理引擎生命周期
- 服务启动时自动加载配置并启动所有启用的隧道

### SVC-002: 故障自动重启（5 点）

> 作为运维，我希望故障隧道自动重启，无需人工干预。

**验收标准：**
- 手动断开隧道的远程连接，10 秒内自动重连
- 日志记录重启事件（级别：Warning）
- 退避策略：1s → 2s → 4s → 8s → ... → 最大 60s

### SVC-003: 隧道健康检查（5 点）

> 作为运维，我希望每条隧道有定期健康检查。

**验收标准：**
- 可配置健康检查间隔（默认 30s）
- 健康检查失败记录日志，触发重启流程
- 统计中显示健康状态（Healthy / Degraded / Failed）
- 健康检查方式：对 TCP/HTTP 隧道尝试建立连接

### SVC-004: 命名管道 IPC（8 点）

> 作为开发者（内部），我希望有命名管道 IPC 供 CLI/GUI 管理通信。

**验收标准：**
- 管道服务器接受 JSON-RPC 请求：`list_tunnels`、`start_tunnel`、`stop_tunnel`、`get_stats`
- CLI 可发送请求并收到正确响应
- 多客户端并发连接支持
- 管道名称：`LocalProxy.{username}`

**技术要点：**
- `System.IO.Pipes.NamedPipeServerStream`
- JSON-RPC 2.0 协议格式
- 请求/响应匹配（通过 `id` 字段）

### SVC-005: 文件日志轮转（3 点）

> 作为用户，我希望结构化日志输出到文件并自动轮转。

**验收标准：**
- 日志文件路径：`~/.localproxy/logs/localproxy.log`
- JSON Lines 格式（每行一个 JSON 对象）
- 每日轮转，保留 7 天
- 日志级别默认 Information

---

## 交付物

- [ ] `LocalProxy.Service` 项目（GenericHost + IHostedService）
- [ ] 隧道健康监控（可配置间隔 + 退避重试）
- [ ] 自动重启逻辑（指数退避）
- [ ] `LocalProxy.Ipc` 项目（JSON-RPC over Named Pipes）
- [ ] 文件日志提供程序（JSON Lines + 每日轮转）
- [ ] 服务生命周期集成测试
- [ ] IPC 协议文档（方法列表、参数、返回值）

---

## 完成标准

- [ ] 5 个用户故事验收标准全部通过
- [ ] 服务在 macOS + Windows 上后台运行验证通过
- [ ] IPC 通信集成测试通过（多轮请求/响应）
- [ ] 故障注入测试：模拟远端断开，验证自动重连
