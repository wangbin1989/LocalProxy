# LocalProxy — 敏捷迭代规划

## 1. 产品愿景

LocalProxy 是一个跨平台本地端口转发工具，帮助开发者和 IT 专业人员将本地服务暴露到远程服务器。支持 TCP、UDP、HTTP 三种协议的代理转发，可作为后台服务运行并自动重启，提供 CLI 和 GUI 两种管理界面。内置常用软件（SQL Server、Redis、MySQL、PostgreSQL、MongoDB 等）的预置代理配置，同时支持完全自定义的代理配置。

**目标平台：**

| 平台 | 架构 | 界面 |
|------|------|------|
| macOS | Apple Silicon | CLI + GUI |
| Windows | x64, arm64 | CLI + GUI |
| Linux | x64, arm64 | CLI only |

**技术栈：** .NET 10 AOT、.NET MAUI、C# 14

---

## 2. 史诗分解

### Epic 1: 核心代理引擎 (ENGINE)

可靠、高性能的 TCP/UDP/HTTP 代理转发引擎，支持所有目标平台。

- TCP 连接转发（端口到端口、端口到远程）
- UDP 数据报转发
- HTTP 正向/反向代理
- 连接池与生命周期管理
- 带宽/吞吐量监控
- 优雅关闭与连接排空
- 结构化日志

### Epic 2: 配置与预置模板 (CONFIG)

灵活的配置系统，支持预置模板和自定义配置，持久化到磁盘并带校验。

- JSON 配置文件（含 JSON Schema 校验）
- 8 套预置代理配置模板
- 自定义代理配置（完整参数校验）
- 配置文件热加载（文件监控）
- 配置导入/导出
- 配置版本管理与迁移

### Epic 3: 服务运行时 (SERVICE)

后台守护进程，运行代理隧道，故障自动重启，暴露 IPC 管理接口。

- 基于 Generic Host 的长时间运行服务
- 进程崩溃自动重启（健康检查看门狗）
- 每条隧道的健康监控与自动重连
- 命名管道 IPC 用于 CLI/GUI 通信
- 优雅关闭处理
- 平台原生守护进程集成（launchd / systemd / Windows Service）

### Epic 4: CLI 接口 (CLI)

功能完备的命令行管理工具，适用于脚本化和无桌面的 Linux 环境。

- 启动/停止代理隧道
- 查看隧道状态与实时统计
- 配置增删改查
- 配置导入/导出
- 服务管理（安装/卸载/启动/停止）
- 日志查看（含 `--follow` 实时跟踪）
- JSON 输出模式（`--output json`）
- Shell 自动补全（bash / zsh / PowerShell）

### Epic 5: GUI 界面 (GUI)

.NET MAUI 桌面应用，提供可视化的代理管理、系统托盘集成和开机自启配置。

- 主窗口：代理列表与状态指示
- 添加/编辑/删除代理对话框（含校验）
- 一键启动/停止
- 实时连接统计
- 系统托盘图标与右键菜单
- 关闭最小化到托盘
- 开机自启开关
- 暗色模式
- 日志查看面板
- 首次启动快速设置向导

### Epic 6: 跨平台构建与发布 (BUILD)

自动化 CI/CD，为所有目标平台生成原生安装包。

- macOS: `.app` + `.dmg`（签名公证）
- Windows: MSIX 安装包 + 便携 ZIP
- Linux: `.deb`、`.rpm`、tar.gz（x64 + arm64）
- GitHub Actions 矩阵构建
- 版本号自动标记（基于 git tag）
- 发布说明自动生成

---

## 3. 冲刺计划总览（2 周一期）

| 冲刺 | 周期 | 目标 | 故事点 |
|------|------|------|--------|
| Sprint 0 | 第 1 周（1 周） | 项目初始化、架构设计、CI/CD | — |
| Sprint 1 | 第 2-3 周 | TCP 代理核心 | 19 |
| Sprint 2 | 第 4-5 周 | UDP + HTTP 代理 | 21 |
| Sprint 3 | 第 6-7 周 | 配置系统与预置模板 | 21 |
| Sprint 4 | 第 8-9 周 | 服务宿主与守护进程 | 29 |
| Sprint 5 | 第 10-11 周 | CLI 核心命令 | 23 |
| Sprint 6 | 第 12-13 周 | CLI 完善（服务管理、Shell 补全） | 28 |
| Sprint 7 | 第 14-15 周 | MAUI GUI 基础 | 31 |
| Sprint 8 | 第 16-17 周 | 系统托盘、开机自启、快速设置 | 31 |
| Sprint 9 | 第 18-19 周 | 跨平台构建与打包 | 26 |
| Sprint 10 | 第 20-21 周 | 文档、缺陷修复、v1.0 发布 | — |

**总计：约 21 周（~5 个月），3 人团队**

### 里程碑

```
Sprint 0  (第 1 周)    项目骨架搭建，CI 绿灯
Sprint 1  (第 2-3 周)  TCP 代理可运行
Sprint 2  (第 4-5 周)  三种协议全部可用
Sprint 3  (第 6-7 周)  JSON 配置 + 8 套预置模板
Sprint 4  (第 8-9 周)  后台服务 + 自动重启
─── M1: 功能性代理服务（第 9 周末）───
Sprint 5  (第 10-11 周) CLI 核心命令 + IPC 通信
Sprint 6  (第 12-13 周) CLI 服务管理 + Shell 补全
─── M2: CLI v0.9 可用（第 13 周末）───
Sprint 7  (第 14-15 周) MAUI 主窗口 + 代理编辑
Sprint 8  (第 16-17 周) 系统托盘 + 开机自启 + 向导
─── M3: GUI v0.9 可用（第 17 周末）───
Sprint 9  (第 18-19 周) 全平台打包 CI/CD
Sprint 10 (第 20-21 周) 文档完善 + 缺陷修复
─── M4: v1.0.0 正式发布（第 21 周末）───
```

---

## 4. 完成定义 (Definition of Done)

每一条用户故事在标记为"完成"前，必须满足以下所有条件：

### 代码质量
- [ ] 至少一人代码评审通过（独立工作则 24 小时冷却后自审）
- [ ] 所有已有单元测试通过
- [ ] 新增代码单元测试行覆盖率 >= 80%
- [ ] 跨进程/网络边界的功能需有集成测试
- [ ] 零编译警告（CI 中将警告视为错误）
- [ ] AOT 兼容性验证通过（Core/Engine/CLI 项目无反射警告）

### 功能
- [ ] 故事的验收标准全部满足并可演示
- [ ] 至少在 2 个目标平台上测试通过（macOS + Windows 或 Linux 之一）
- [ ] 手动探索性测试完成（正常路径 + 2 个边界情况）
- [ ] 性能：基准测试无超过 10% 的回归

### 文档
- [ ] 所有公开类型和方法有 XML 文档注释
- [ ] 面向用户的功能需更新 README 或文档
- [ ] 重大技术决策需添加 ADR（架构决策记录）

### 运维
- [ ] 日志级别恰当（Info=生命周期、Warn=可恢复错误、Error=故障）
- [ ] 错误信息对用户可操作（用户界面不展示堆栈跟踪）
- [ ] 可能造成破坏的功能需有配置开关

### GUI 故事附加条件
- [ ] 在 100% 和 125% 显示缩放下测试通过
- [ ] 键盘可导航（Tab 顺序、Enter 提交、Esc 取消）
- [ ] 暗色/亮色模式均渲染正确
- [ ] 服务进程未运行时无未处理异常

---

## 5. 风险评估

| ID | 风险 | 概率 | 影响 | 缓解措施 |
|----|------|------|------|----------|
| R1 | .NET 10 AOT 与关键库不兼容 | 中 | 高 | 从 Sprint 0 起在 CI 中持续 AOT 编译。维护 AOT 验证通过的 NuGet 包清单。备选方案：CLI 以非 AOT 模式运行 |
| R2 | MAUI 在 macOS/Windows 上的成熟度问题 | 中 | 中 | 在 Sprint 7 提前做技术调研。若有严重问题，降级为平台原生代码（P/Invoke） |
| R3 | 命名管道 IPC 跨平台可靠性 | 低 | 高 | .NET 三大平台对命名管道支持良好。IPC 通道加入心跳检测。备选：Unix domain socket |
| R4 | 小团队（1-3 人）关键人风险 | 高 | 中 | 强制代码评审。维护架构文档。轮换功能领域，避免知识孤岛 |
| R5 | 平台守护进程集成差异大 | 高 | 中 | 抽象 `IServiceManager` 接口，三个平台分别实现适配器 |
| R6 | 范围蔓延 / 过度设计 | 中 | 中 | 严格按优先级排序待办列表。MVP 定义清晰（TCP+UDP+配置+CLI+服务模式），之后不再随意扩展 |
| R7 | AOT 编译时间长，拖慢 CI 反馈 | 中 | 低 | 分层构建：PR 校验用 Debug（无 AOT），合并到 main 和打 tag 时才编译 Release+AOT |
| R8 | MAUI 不支持 AOT，两条构建路径可能分化 | 高 | 中 | 明确分离：Core/Engine/CLI=AOT；MAUI App=框架依赖。IPC 契约用集成测试保底 |
| R9 | Linux arm64 CI runner 不可用 | 中 | 中 | 从 x64 交叉编译 `linux-arm64`。发布前在树莓派或 QEMU 上手测 |
| R10 | 配置文件损坏导致用户数据丢失 | 低 | 高 | 原子写入（先写临时文件再重命名）。保留最近 5 份配置备份。写入前校验 |

---

## 6. 团队与节奏

### 团队配置（1-3 人）

| 角色 | 职责 | 负责史诗 |
|------|------|----------|
| Dev A（高级/组长） | 架构设计、引擎核心、服务宿主、构建流水线 | Epic 1, 3 |
| Dev B（全栈） | 配置系统、CLI 接口、跨平台测试 | Epic 2, 4 |
| Dev C（前端/MAUI） | GUI 应用、系统托盘、开机自启 | Epic 5 |

- 若仅 1-2 人：合并角色，时间线延长 30-50%

### Scrum 仪式（双周）

- **冲刺规划：** 冲刺第一周周一，2 小时
- **每日站会：** 15 分钟，小团队可异步（Slack/Teams）
- **冲刺评审：** 冲刺第二周周五，1 小时
- **冲刺回顾：** 冲刺第二周周五，45 分钟

### 速率假设

- 3 人团队：每冲刺 20-25 故事点
- 2 人团队：每冲刺 14-18 故事点
- 1 人团队：每冲刺 8-12 故事点
- 建议在 Sprint 3 后根据实际数据重新校准

---

## 7. 文档索引

| 文档 | 说明 |
|------|------|
| [README.md](README.md) | 本文件 — 敏捷规划总览 |
| [product-backlog.md](product-backlog.md) | 优先级排序的完整产品待办列表 |
| [architecture.md](architecture.md) | 技术架构决策记录 |
| [sprint-plans/sprint-00-project-init.md](sprint-plans/sprint-00-project-init.md) | Sprint 0 — 项目初始化 |
| [sprint-plans/sprint-01-tcp-core.md](sprint-plans/sprint-01-tcp-core.md) | Sprint 1 — TCP 代理核心 |
| [sprint-plans/sprint-02-udp-http.md](sprint-plans/sprint-02-udp-http.md) | Sprint 2 — UDP + HTTP 代理 |
| [sprint-plans/sprint-03-config-presets.md](sprint-plans/sprint-03-config-presets.md) | Sprint 3 — 配置系统与预置模板 |
| [sprint-plans/sprint-04-service-daemon.md](sprint-plans/sprint-04-service-daemon.md) | Sprint 4 — 服务宿主与守护进程 |
| [sprint-plans/sprint-05-cli-interface.md](sprint-plans/sprint-05-cli-interface.md) | Sprint 5 — CLI 核心命令 |
| [sprint-plans/sprint-06-cli-complete.md](sprint-plans/sprint-06-cli-complete.md) | Sprint 6 — CLI 完善 |
| [sprint-plans/sprint-07-gui-foundation.md](sprint-plans/sprint-07-gui-foundation.md) | Sprint 7 — GUI 基础 |
| [sprint-plans/sprint-08-gui-tray.md](sprint-plans/sprint-08-gui-tray.md) | Sprint 8 — 系统托盘与自动启动 |
| [sprint-plans/sprint-09-cross-platform.md](sprint-plans/sprint-09-cross-platform.md) | Sprint 9 — 跨平台构建与打包 |
| [sprint-plans/sprint-10-polish-release.md](sprint-plans/sprint-10-polish-release.md) | Sprint 10 — 打磨与发布 |
