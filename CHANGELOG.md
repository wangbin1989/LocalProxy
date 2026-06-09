# Changelog

## [Unreleased] — Sprint 0 (项目初始化)

### 2026-06-09
- 创建敏捷迭代规划文档（`docs/agile/`）
- 搭建解决方案骨架：7 个项目（Core, Config, Engine, Service, Ipc, Cli, App）
- 配置项目依赖关系（App/Cli → Ipc → Service → Engine → Config → Core）
- 创建核心领域模型：`ITunnelHandler`, `TunnelStats`, `ProxyConfig`, 枚举类型
- 配置 AOT 编译：Core/Config/Engine/Service/Ipc 标记 IsAotCompatible，Cli 启用 PublishAot
- 注意：MAUI App 项目需安装 `dotnet workload install maui` 后方可构建
- 6 个核心项目构建验证通过
- 创建跨平台构建脚本（`build.sh` / `build.ps1`）
- 配置 GitHub Actions CI/CD（PR 构建 + Tag 发布）
- Sprint 0 完成

## Sprint 1 — TCP 代理核心

### 2026-06-09
- 实现 `TcpForwardHandler`：TCP 端口转发、并发连接支持、优雅关闭
- 连接池限制（SemaphoreSlim）、字节统计、ILogger 日志集成
- `ITunnelHandler` 扩展 `IDisposable`
- 添加 NuGet 包：`Microsoft.Extensions.Logging.Abstractions`、`Microsoft.Extensions.Hosting`
- 创建 xUnit 测试项目（`LocalProxy.Engine.Tests`、`LocalProxy.Config.Tests`）
- 4 个集成测试全部通过：数据转发、并发连接、连接限制、资源泄漏检测
- Sprint 1 完成

## Sprint 2 — UDP + HTTP 代理

### 2026-06-09
- 实现 `UdpForwardHandler`：UDP 数据报转发、客户端会话映射、超时清理
- 实现 `HttpForwardHandler`：HTTP 层 TCP 双向转发（轻量级，无 HttpListener 依赖）
- 4 个新集成测试（UDP 转发、UDP 多客户端、HTTP GET、HTTP 并发）
- 全部 8 个测试通过
- Sprint 2 完成

## Sprint 3 — 配置系统与预置模板

### 2026-06-09
- 实现 `PresetProfiles`：8 套预置模板（SQL Server/Redis/MySQL/PG/MongoDB/RabbitMQ/ES/Kafka）
- 实现 `ConfigManager`：JSON 配置加载/保存、原子写入、自动备份（保留 5 份）
- 配置校验：端口范围、名称唯一性、端口冲突、必填字段
- `FileSystemWatcher` 热加载（500ms 防抖）
- `ConfigJsonContext` 源生成器（AOT 安全序列化）
- 20 个单元测试全部通过（校验 + 预设 + 增删）
- Sprint 3 完成

## Sprint 4 — 服务宿主与守护进程

### 2026-06-09
- 实现 `TunnelService`：基于 GenericHost 的 IHostedService，管理隧道生命周期
- 隧道故障自动重启（指数退避：1s→2s→4s→...→60s）
- 配置热加载联动：新增/删除隧道自动启停
- 实现 JSON-RPC 2.0 over Named Pipes IPC 协议
- `IpcServer`（服务端）和 `IpcClient`（客户端）实现
- 支持方法：list_tunnels / start_tunnel / stop_tunnel / get_stats
- 全部 28 个测试通过
- **里程碑 M1 达成**：功能性代理服务（TCP/UDP/HTTP 转发 + 配置 + 守护进程）
- Sprint 4 完成

## Sprint 5 — CLI 核心命令

### 2026-06-09
- 实现 CLI 命令系统（轻量级手动解析器，AOT 安全）
- 支持命令：`config add/remove/list`、`start/stop`、`status`、`--version`、`--help`
- `--json` 输出模式，`--proto` 支持 tcp/udp/http
- CLI 通过 IPC 客户端与服务进程通信
- Sprint 5 完成

## Sprint 6 — CLI 完善

### 2026-06-09
- 服务管理命令：`service install/uninstall/start/stop/status`
- 多平台服务定义（macOS LaunchAgent / Linux systemd / Windows）
- Shell 自动补全脚本：bash / zsh / pwsh
- 配置导入导出：`config import <file>` / `config export <file>`
- 统一退出码（0=成功, 1=一般错误, 2=配置错误, 3=服务未运行）
- **里程碑 M2 达成**：CLI v0.9 可用
- Sprint 6 完成
