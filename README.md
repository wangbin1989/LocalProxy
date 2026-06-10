# LocalProxy

一款本地代理工具，用于将本地端口代理到远程服务器，支持 TCP/UDP/HTTP 代理。提供 CLI 和 GUI 两种管理方式，适用于 macOS、Windows 和 Linux 平台。

## 技术栈

- 核心框架：.NET 10 AOT（原生提前编译）
- GUI 框架：.NET MAUI
- CLI 框架：手动解析器（AOT 安全）
- IPC 协议：JSON-RPC 2.0 over Named Pipes

## 运行环境

| 平台 | 架构 | 界面 |
|------|------|------|
| macOS | Apple Silicon | CLI + GUI |
| Windows | x64, arm64 | CLI + GUI |
| Linux | x64, arm64 | CLI only |

## 功能特性

- 支持 TCP/UDP/HTTP 代理转发
- 多平台支持（macOS、Windows、Linux）
- CLI 命令行管理界面
- GUI 桌面管理界面（.NET MAUI）
- 8 套预置代理模板（SQL Server / Redis / MySQL / PG / MongoDB / RabbitMQ / ES / Kafka）
- 自定义代理配置（JSON 格式，支持热加载）
- 后台服务运行，隧道故障自动重启（指数退避）
- 系统托盘最小化、开机自启

## 快速开始

### 构建

```bash
# 要求: .NET 10 SDK
dotnet --version  # >= 10.0.300

# 构建所有项目（除 MAUI GUI）
dotnet build

# 运行测试
dotnet test

# AOT 发布 CLI（单文件）
dotnet publish src/LocalProxy.Cli/LocalProxy.Cli.csproj \
  -c Release -r osx-arm64 --self-contained \
  -o artifacts/cli/osx-arm64

# 构建脚本
./build.sh Release osx-arm64    # macOS/Linux
./build.ps1 Release win-x64     # Windows
```

### 安装 MAUI 工作负载（GUI 构建）

```bash
sudo dotnet workload install maui
dotnet build src/LocalProxy.App/LocalProxy.App.csproj -f net10.0-maccatalyst
```

### CLI 使用

```bash
# 查看帮助
localproxy --help

# 添加代理配置
localproxy config add --name redis --proto tcp \
  --local-port 6379 --remote-host redis.example.com --remote-port 6379

# 列出配置
localproxy config list

# 启动/停止隧道
localproxy start redis
localproxy stop redis

# 查看状态
localproxy status --json

# 安装后台服务 (macOS)
localproxy service install
```

### 配置文件

位置：`~/.localproxy/config.json`（macOS/Linux）或 `%APPDATA%\LocalProxy\config.json`（Windows）

```json
[
  {
    "name": "my-redis",
    "protocol": "tcp",
    "localPort": 6379,
    "remoteHost": "redis.example.com",
    "remotePort": 6379,
    "maxConnections": 100,
    "timeoutSeconds": 30,
    "enabled": true
  }
]
```

## 项目结构

```
src/
  LocalProxy.Core/      # 领域模型、接口
  LocalProxy.Config/    # 配置管理、预置模板
  LocalProxy.Engine/    # TCP/UDP/HTTP 转发引擎
  LocalProxy.Service/   # 后台服务宿主
  LocalProxy.Ipc/       # JSON-RPC 命名管道协议
  LocalProxy.Cli/       # CLI 命令行工具
  LocalProxy.App/       # .NET MAUI GUI 应用
tests/
  LocalProxy.Engine.Tests/   # 引擎集成测试 (8)
  LocalProxy.Config.Tests/   # 配置单元测试 (20)
docs/
  agile/                # 敏捷迭代规划文档
```

## 开发状态

| 里程碑 | 状态 |
|--------|------|
| M1: 功能性代理服务 | 已完成 |
| M2: CLI v0.9 | 已完成 |
| M3: GUI v0.9 | 代码就绪（需 MAUI workload） |
| M4: v1.0 发布 | 进行中 |

## 许可证

MIT
