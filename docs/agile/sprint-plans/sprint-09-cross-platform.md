# Sprint 9 — 跨平台构建与打包

**周期：** 第 18-19 周（2 周）  
**目标：** 完成全平台 CI/CD 构建流水线，产出各平台原生安装包

---

## 冲刺目标

实现 GitHub Actions 自动化构建流水线，为 macOS、Windows、Linux 三大平台的所有架构生成原生安装包和发布产物。

---

## 用户故事

### BLD-001: 全平台 CI 构建（8 点）

> 作为发布工程师，我希望 GitHub Actions 在打 tag 时自动构建全平台二进制。

**验收标准：**
- 推送 `v1.0.0` tag → 触发构建
- 构建矩阵覆盖：
  - `macos-latest` (arm64)
  - `windows-latest` (x64, arm64)
  - `ubuntu-latest` (x64, arm64 交叉编译)
- 全部构建成功，产物上传到 GitHub Release
- PR 触发轻量构建（Debug，无 AOT，验证编译通过）

**技术要点：**
- `dotnet publish -c Release -r <rid> /p:PublishAot=true` 用于 AOT 项目
- MAUI 项目用 `dotnet publish -c Release -f net10.0-maccatalyst` 等
- 分层构建：PR=Debug 快速反馈，Tag=Release+AOT 全量

### BLD-002: macOS 打包（5 点）

> 作为 macOS 用户，我希望下载 `.dmg` 安装到 `/Applications`。

**验收标准：**
- DMG 包含 `LocalProxy.app`（MAUI 打包的 .app bundle）
- CLI 二进制包含在 app bundle 内
- 若配置 Apple Developer 凭证，则进行代码签名和公证
- 无凭证时生成未签名 DMG（用户需手动允许）

### BLD-003: Windows 打包（5 点）

> 作为 Windows 用户，我希望下载 MSIX 或便携 ZIP。

**验收标准：**
- MSIX 安装包（Windows App SDK 包装）
- 独立 ZIP（便携版，解压即用）
- x64 和 arm64 两个架构分别产出
- CLI `localproxy.exe` 和 GUI `LocalProxy.App.exe` 都包含在内

### BLD-004: Linux 打包（5 点）

> 作为 Linux 用户，我希望有 `.deb`、`.rpm` 和 tar.gz 包。

**验收标准：**
- CI 产出 6 个 Linux 包：
  - `localproxy_1.0.0_amd64.deb`
  - `localproxy_1.0.0_amd64.rpm`
  - `localproxy_1.0.0_amd64.tar.gz`
  - `localproxy_1.0.0_arm64.deb`
  - `localproxy_1.0.0_arm64.rpm`
  - `localproxy_1.0.0_arm64.tar.gz`
- `.deb` 安装后将 CLI 放到 `/usr/bin/localproxy`
- `localproxy service install` 注册 systemd 用户服务
- 仅包含 CLI（Linux 无 GUI）

### BLD-005: 版本号注入（3 点）

> 作为用户，我希望 `--version` 显示版本号和 commit。

**验收标准：**
- `localproxy --version` 输出：`localproxy 1.0.0 (commit abc1234, 2026-06-09)`
- GUI "关于"对话框显示相同信息
- 版本号从 git tag 自动提取
- 非 tag 构建显示 `1.0.0-dev (commit abc1234)`

---

## 交付物

- [ ] GitHub Actions Release 工作流（全平台 + 全架构矩阵构建）
- [ ] macOS `.dmg`（含签名/公证，若凭证可用）
- [ ] Windows MSIX + 便携 ZIP
- [ ] Linux `.deb`、`.rpm`、`.tar.gz`（6 个变体）
- [ ] 版本号 git tag 注入

---

## 完成标准

- [ ] 发布 CI 流水线在 GitHub Actions 上全部绿灯
- [ ] macOS DMG 在 Apple Silicon Mac 上可安装运行
- [ ] Windows MSIX 在 Windows 11 x64 上可安装运行
- [ ] Windows 便携 ZIP 解压后可直接运行
- [ ] Linux .deb 在 Ubuntu 24.04 上可安装运行
- [ ] `--version` 在各平台上输出正确
