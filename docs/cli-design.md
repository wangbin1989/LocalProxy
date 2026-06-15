# LocalProxy CLI 命令设计文档

## 概述

LocalProxy 是一款本地端口代理工具，将本地端口转发到远程服务器，支持 TCP/UDP/HTTP 协议。CLI 作为管理入口，负责代理的启停、隧道的增删改查、预置模板的快速应用以及全局配置的管理。

### 技术基础

| 组件 | 用途 |
|---|---|
| `System.CommandLine` v2.0.9 | 命令行参数解析与命令路由 |
| `Spectre.Console` v0.57.0 | 富终端输出（表格、面板、彩色状态） |
| `System.IO.Pipes` | 跨平台 IPC 通信（后续迭代） |

---

## 命令树

```
localproxy [--config <path>] [-v|--verbose] [--version]
│
├── run                        快速启动单个代理（v0.0.1）
│
├── tunnel                     管理代理隧道
│   ├── list [--protocol] [--enabled|--disabled]
│   ├── add <name> --local-port --remote-host --remote-port --protocol [--description]
│   ├── remove <name> [--force]
│   ├── enable <name>
│   └── disable <name>
│
├── preset                     预置代理配置
│   ├── list [--protocol]
│   ├── add <preset-name> [--local-port] [--name]
│   └── info <preset-name>
│
├── service                    管理代理后台服务
│   ├── start
│   ├── stop [--force]
│   ├── restart [--force]
│   └── status [--json]
│
└── config                     全局配置管理
    ├── show [--json]
    ├── set <key> <value>
    └── path
```

---

## 全局选项

| 选项 | 别名 | 类型 | 说明 |
|---|---|---|---|
| `--config` | — | `string` (路径) | 自定义配置文件路径 |
| `--verbose` | `-v` | `bool` | 详细输出，可叠加（`-v` = info, `-vv` = debug） |
| `--version` | — | `bool` | 输出版本号并退出 |

---

## 退出码

| 码 | 含义 |
|---|---|
| 0 | 成功 |
| 1 | 一般错误 / 参数无效 / 校验失败 |
| 2 | daemon 未运行 |
| 3 | 配置文件未找到或不可读 |
| 4 | IPC 通信失败 |
| 5 | 用户取消确认 |

---

## 命令详细规格

### 0. `run` — 快速启动代理（v0.0.1）

快速启动单个端口代理，无需创建命名隧道或配置文件。

```
localproxy run -l <port> -H <host> -p <port> -P <tcp|udp|http>
```

| 选项 | 别名 | 必填 | 类型 | 说明 |
|---|---|---|---|---|
| `--local-port` | `-l` | 是 | `int` (0–65535) | 本地监听端口 |
| `--remote-host` | `-H` | 是 | `string` | 远程目标主机地址 |
| `--remote-port` | `-p` | 是 | `int` (0–65535) | 远程目标端口 |
| `--protocol` | `-P` | 是 | `tcp\|udp\|http` | 代理协议 |

**行为：**
- 解析参数，校验端口范围和协议值
- 通过 Spectre.Console 面板输出代理配置摘要
- 校验失败时退出码 1，含明确错误信息
- v0.0.1 不启动实际端口转发

### 1. `service` — 后台服务生命周期

CLI 自身不执行代理转发，所有操作通过 IPC 与 daemon 进程通信。

| 子命令 | 说明 |
|---|---|
| `start` | 以后台进程方式启动 daemon；若已运行则提示 PID |
| `stop [--force]` | 通过 IPC 发送关闭请求；`--force` 跳过确认并强制终止 |
| `restart [--force]` | 依次执行 stop + start |
| `status [--json]` | 显示 daemon 运行时状态（PID、运行时间、活跃隧道数、转发字节数） |

### 2. `tunnel` — 隧道管理

隧道定义以配置文件为数据源。daemon 读取该文件并据此启动/停止转发。

| 子命令 | 说明 |
|---|---|
| `list [--protocol] [--enabled\|--disabled]` | Spectre.Console 表格展示所有隧道 |
| `add <name>` | 新建隧道，必填 `--local-port` `--remote-host` `--remote-port` `--protocol`，可选 `--description` |
| `remove <name> [--force]` | 删除隧道，非 force 模式需确认 |
| `enable <name>` | 启用已禁用的隧道 |
| `disable <name>` | 停用隧道但保留配置 |

### 3. `preset` — 预置代理模板

硬编码的常用服务代理模板。预置列表：`sqlserver`(tcp:1433)、`redis`(tcp:6379)、`mysql`(tcp:3306)、`postgres`(tcp:5432)、`mongodb`(tcp:27017)、`rabbitmq`(tcp:5672)、`grpc`(http:5000)、`https`(tcp:8443)

| 子命令 | 说明 |
|---|---|
| `list [--protocol]` | 表格展示所有预置模板 |
| `add <preset-name>` | 从模板创建隧道，`--local-port` 覆盖端口，`--name` 自定义隧道名 |
| `info <preset-name>` | 展示单个预置的详细信息和使用示例 |

### 4. `config` — 全局配置管理

管理的配置项：`log-level`、`ipc-endpoint`、`auto-start`、`connection-timeout`

| 子命令 | 说明 |
|---|---|
| `show [--json]` | 表格展示所有配置 |
| `set <key> <value>` | 修改配置项，含类型校验 |
| `path` | 输出配置文件绝对路径 |

---

## 迭代规划

| 版本 | 内容 |
|---|---|
| v0.0.1 | `run` 命令 — 快速启动单个代理 |
| v0.0.2 | `tunnel` 命令组 — 隧道配置 CRUD，JSON 文件持久化 |
| v0.0.3 | `preset` + `config` 命令组 |
| v0.1.0 | `service` 命令组 + IPC 通信 |
| v0.2.0 | 实际代理转发（TCP → UDP → HTTP） |

---

## 代码组织（规划）

```
src/LocalProxy/
├── Program.cs
├── Commands/
│   ├── RunCommand.cs
│   ├── TunnelCommands.cs
│   ├── PresetCommands.cs
│   ├── ServiceCommands.cs
│   └── ConfigCommands.cs
├── Handlers/
│   ├── RunHandler.cs
│   ├── TunnelHandlers.cs
│   ├── PresetHandlers.cs
│   ├── ServiceHandlers.cs
│   └── ConfigHandlers.cs
├── Services/
│   ├── TunnelService.cs
│   ├── PresetService.cs
│   ├── ConfigService.cs
│   ├── DaemonManager.cs
│   └── IpcClient.cs
├── Models/
│   ├── ProxyProtocol.cs
│   ├── TunnelConfig.cs
│   └── AppConfig.cs
└── Infrastructure/
    ├── ConsoleOutput.cs
    ├── ConfigPaths.cs
    └── JsonContext.cs
```
