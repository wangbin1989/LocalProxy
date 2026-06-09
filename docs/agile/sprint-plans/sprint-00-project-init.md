# Sprint 0 — 项目初始化与架构

**周期：** 第 1 周（1 周）  
**目标：** 搭建仓库骨架、构建系统、CI/CD 流水线，确定技术架构基线

---

## 冲刺目标

1. 完成解决方案结构（7 个项目骨架）
2. CI/CD 流水线在三大平台上跑通（构建 + 测试）
3. AOT 和 MAUI 冒烟测试通过
4. 技术架构文档定稿

---

## 交付物

- [ ] 解决方案包含 7 个项目骨架（Core, Config, Engine, Service, Ipc, Cli, App）
- [ ] 目录布局：`src/`、`tests/`、`docs/`、`tools/`
- [ ] 构建脚本：`build.sh`（macOS/Linux）、`build.ps1`（Windows）
- [ ] GitHub Actions CI：PR 触发三大平台构建 + 测试
- [ ] 架构决策记录（ADR）写入 `docs/agile/architecture.md`
- [ ] AOT 冒烟测试：验证简单控制台应用在三大平台 `PublishAot` 编译通过
- [ ] MAUI 冒烟测试：验证空白 MAUI 应用在 macOS + Windows 上构建运行
- [ ] `.gitignore` 完善（已有基线，补充 .NET 特定项）
- [ ] NuGet 包选定与审批：
  - `System.CommandLine`（AOT 安全）
  - `H.NotifyIcon`（系统托盘）
  - `CommunityToolkit.Mvvm`（MVVM 源生成器）
  - `Microsoft.Extensions.Hosting`（Generic Host）
  - `Microsoft.Extensions.Logging`（日志抽象）
  - `xUnit` + `NSubstitute`（测试）

---

## 任务分工

| 任务 | 负责人 | 工时 |
|------|--------|------|
| 架构设计、ADR 编写、解决方案结构 | Dev A | 16h |
| CI/CD 流水线、构建脚本、冒烟测试 | Dev B | 20h |
| NuGet 审查、项目脚手架、文档骨架 | Dev C | 16h |

---

## 技术调研（冲刺内）

| 调研项 | 时长 | 负责人 |
|--------|------|--------|
| Socket vs TcpClient 在 macOS/Linux 上的吞吐量对比 | 4h | Dev A |
| MAUI 在 macOS 和 Windows 上的系统托盘可行性 | 4h | Dev C |

---

## 完成标准

- [ ] `dotnet build` 全解决方案零错误零警告
- [ ] CI 在 macOS、Windows、Linux 三个 runner 上全部绿灯
- [ ] `docs/agile/architecture.md` 包含至少 6 个 ADR
- [ ] 所有选定的 NuGet 包已验证许可证兼容性
