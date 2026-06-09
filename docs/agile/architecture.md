# 技术架构决策记录 (ADR)

## ADR-001: 解决方案项目结构

**决策：** 单解决方案 7 个项目，分层依赖，无循环引用。

```
src/
  LocalProxy.Core/          # 领域模型、接口、枚举（零依赖）
  LocalProxy.Config/        # 配置模型、校验、持久化（→ Core）
  LocalProxy.Engine/        # TCP/UDP/HTTP 转发引擎（→ Core, Config）
  LocalProxy.Service/       # Generic Host 服务宿主（→ Engine, Config）
  LocalProxy.Ipc/           # JSON-RPC 命名管道协议定义（→ Core）
  LocalProxy.Cli/           # CLI 命令行入口（→ Ipc）
  LocalProxy.App/           # .NET MAUI GUI 应用（→ Ipc）
```

依赖流向：`App/Cli` → `Ipc` → `Service` → `Engine` → `Config` → `Core`

**理由：** 清晰的依赖方向确保 Core 和 Engine 可独立测试。IPC 库作为共享契约层，CLI 和 GUI 通过它通信但不直接依赖 Service。

---

## ADR-002: IPC 协议 — JSON-RPC 2.0 over Named Pipes

**决策：** CLI 和 GUI 通过命名管道（Named Pipes）上的 JSON-RPC 2.0 协议与服务进程通信。

- 服务端：`System.IO.Pipes.NamedPipeServerStream`（始终由 Service 进程持有）
- 客户端：`System.IO.Pipes.NamedPipeClientStream`
- 协议：JSON-RPC 2.0 请求/响应模型
- 管道名称：`LocalProxy.{username}`（跨平台兼容）

**支持的方法：**

| 方法 | 参数 | 返回 |
|------|------|------|
| `list_tunnels` | — | TunnelInfo[] |
| `start_tunnel` | name: string | TunnelStatus |
| `stop_tunnel` | name: string | TunnelStatus |
| `get_stats` | name: string | TunnelStats |
| `reload_config` | — | ConfigReloadResult |
| `get_logs` | lines: int, level: string | LogEntry[] |

**理由：** 命名管道是 .NET 跨平台最可靠的本地 IPC 机制。JSON-RPC 简单、可调试、易于扩展。备选方案（Unix Domain Socket）仅在管道出现平台问题时启用。

---

## ADR-003: AOT 编译策略

**决策：** 除 MAUI App 外所有项目启用 `PublishAot`。MAUI 使用标准框架依赖编译。

- `LocalProxy.Core` → AOT
- `LocalProxy.Config` → AOT（`System.Text.Json` 源生成器）
- `LocalProxy.Engine` → AOT
- `LocalProxy.Service` → AOT
- `LocalProxy.Ipc` → AOT
- `LocalProxy.Cli` → AOT（`System.CommandLine` 源生成器）
- `LocalProxy.App` → 框架依赖（MAUI 不支持 AOT）

**理由：** AOT 提供更快的启动速度、更低的内存占用和单文件分发能力，这对 CLI 和守护进程至关重要。MAUI 依赖大量反射，无法 AOT 编译，但其作为桌面 GUI 应用对启动速度的要求不敏感。IPC 边界恰好是 AOT 和非 AOT 代码的自然分界。

**风险：** 需要确保所有 AOT 项目使用的 NuGet 包支持裁剪。在 CI 中持续验证。

---

## ADR-004: 配置存储策略

**决策：** 单 JSON 文件，原子写入，自动备份。

- 文件位置：
  - macOS/Linux: `~/.localproxy/config.json`
  - Windows: `%APPDATA%\LocalProxy\config.json`
- 写入方式：先写 `config.json.tmp`，再 `File.Move`（原子操作）
- 备份策略：保留最近 5 个版本（`config.json.bak.1` ~ `.bak.5`）
- 序列化：`System.Text.Json` + 源生成器（AOT 安全）
- 版本字段：`"version": 1`，支持迁移函数

**理由：** JSON 格式用户可直接编辑，源生成器确保 AOT 兼容。原子写入和备份防止配置损坏。

---

## ADR-005: 预置配置分发方式

**决策：** 预置模板以嵌入式资源编译到 `LocalProxy.Config` 程序集中。

```csharp
// 通过 API 获取，无需外部文件
var presets = PresetProfiles.List();
var redis = PresetProfiles.Get("Redis");
```

**理由：** 模板随二进制分发，无需额外文件，确保 CLI 和 GUI 开箱即用。用户可在此基础上修改生成自定义配置。

---

## ADR-006: 测试策略

**决策：** 分层测试，集成测试使用真实 socket。

| 层级 | 框架 | 范围 | 目标覆盖率 |
|------|------|------|------------|
| 单元测试 | xUnit + NSubstitute | 业务逻辑、校验、模型 | >= 80% |
| 集成测试 | xUnit | 网络 I/O、IPC、文件系统 | 核心路径 100% |
| 冒烟测试 | Shell 脚本 | 全平台构建 + 基本功能 | 每个 PR |

**原则：**
- 集成测试不 mock 网络 I/O，使用 localhost 真实端口。
- 每个 PR 在 macOS + Windows + Linux 上运行 CI。
- 发布前在真实设备上手动测试 GUI。

---

## ADR-007: 日志策略

**决策：** 使用 `Microsoft.Extensions.Logging` 抽象，结合控制台和文件提供程序。

- 控制台：文本格式，仅 CLI 前台模式和调试时使用
- 文件：JSON Lines 格式（结构化），路径 `~/.localproxy/logs/localproxy.log`
- 轮转：每日轮转，保留 7 天
- 级别：默认 Information，可通过配置调整

**理由：** 结构化日志便于分析。`Microsoft.Extensions.Logging` 是 .NET 生态标准，与 Generic Host 无缝集成。

---

## ADR-008: GUI 技术选型

**决策：** .NET MAUI + CommunityToolkit.Mvvm + H.NotifyIcon。

| 组件 | 选择 | 备选 |
|------|------|------|
| UI 框架 | .NET MAUI | Avalonia, WinUI 3 |
| MVVM | CommunityToolkit.Mvvm（源生成器） | ReactiveUI, Prism |
| 系统托盘 | H.NotifyIcon | 平台原生 P/Invoke |
| IPC 客户端 | LocalProxy.Ipc（自研） | — |

**理由：** MAUI 是 .NET 官方跨平台桌面方案。CommunityToolkit.Mvvm 源生成器减少样板代码。H.NotifyIcon 是目前最成熟的 .NET 跨平台托盘库。如果遇到重大兼容性问题，备选方案为平台原生实现。

---

## 决策记录索引

| ADR | 标题 | 日期 |
|-----|------|------|
| ADR-001 | 解决方案项目结构 | 2026-06-09 |
| ADR-002 | JSON-RPC over Named Pipes IPC | 2026-06-09 |
| ADR-003 | AOT 编译策略 | 2026-06-09 |
| ADR-004 | 配置存储策略 | 2026-06-09 |
| ADR-005 | 预置配置分发方式 | 2026-06-09 |
| ADR-006 | 测试策略 | 2026-06-09 |
| ADR-007 | 日志策略 | 2026-06-09 |
| ADR-008 | GUI 技术选型 | 2026-06-09 |
