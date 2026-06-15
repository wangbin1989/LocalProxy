# LocalProxy CLI 命令设计文档

## 概述

LocalProxy 是一款本地端口代理工具，将本地端口转发到远程服务器，支持 TCP/UDP/HTTP 协议。

### 技术基础

| 组件 | 用途 |
|---|---|
| `System.CommandLine` v2.0.9 | 命令行参数解析与命令路由 |
| `Spectre.Console` v0.57.0 | 富终端输出（表格、面板、彩色状态） |

---

## 命令树

```
localproxy [--config <path>] [--version]
│
├── run                         启动代理
│
├── add <name>                  添加代理配置
├── update <name>               更新代理配置
├── remove <name> [--force]     删除代理配置
├── enable <name>               启用代理
├── disable <name>              停用代理
└── list                        列出所有代理配置
```

---

## 全局选项

| 选项 | 别名 | 类型 | 说明 |
|---|---|---|---|
| `--config` | — | `string` (路径) | 配置文件路径，默认 `~/.localproxy.json` |
| `--version` | — | `bool` | 输出版本号并退出 |

---

## 退出码

| 码 | 含义 |
|---|---|
| 0 | 成功 |
| 1 | 一般错误 / 参数无效 / 校验失败 |
| 2 | 配置文件未找到或不可读 |
| 3 | 用户取消确认 |

---

## 命令详细规格

### `run` — 启动代理

```
localproxy run [--config <file>]
```

| 选项 | 必填 | 类型 | 说明 |
|---|---|---|---|
| `--config` | 否 | `string` (路径) | 配置文件路径（JSON 数组），默认 `~/.localproxy.json` |

读取配置文件中的所有代理，并行启动端口转发。

### `add` — 添加代理配置

```
localproxy add <name> --local-port <port> --remote-host <host> --remote-port <port> --protocol <tcp|udp|http> [--config <file>]
```

### `update` — 更新代理配置

```
localproxy update <name> [--local-port <port>] [--remote-host <host>] [--remote-port <port>] [--protocol <tcp|udp|http>] [--config <file>]
```

### `remove` — 删除代理配置

```
localproxy remove <name> [--force] [--config <file>]
```

### `enable` / `disable` — 启用/停用代理

```
localproxy enable <name> [--config <file>]
localproxy disable <name> [--config <file>]
```

### `list` — 列出所有代理配置

```
localproxy list [--config <file>]
```

---

## 代码组织

```
src/LocalProxy/
├── Program.cs
├── Commands/
│   ├── RunCommand.cs
│   └── ConfigCommands.cs
├── Handlers/
│   ├── RunHandler.cs
│   └── ConfigHandlers.cs
├── Services/
│   ├── ProxyService.cs
│   ├── ConfigService.cs
│   └── ProxyConfig.cs
├── Models/
│   └── ProxyProtocol.cs
└── Infrastructure/
    ├── ConsoleOutput.cs
    └── JsonContext.cs
```
