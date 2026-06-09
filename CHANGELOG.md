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
