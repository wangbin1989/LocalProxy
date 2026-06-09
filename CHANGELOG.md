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
