# Sprint 2 — UDP + HTTP 代理

**周期：** 第 4-5 周（2 周）  
**目标：** 扩展代理引擎，支持 UDP 数据报转发和 HTTP 代理，统一协议处理器接口

---

## 冲刺目标

在 TCP 代理基础上，增加 UDP 和 HTTP 协议支持，将三种处理器统一到 `ITunnelHandler` 接口下，并暴露结构化统计信息。

---

## 用户故事

### ENG-005: UDP 数据报转发（8 点）

> 作为开发者，我希望将 UDP 数据报从本地端口转发到远程 host:port。

**验收标准：**
- 向本地 UDP 端口发送数据报，验证远程目标正确收到，响应返回发送方
- 支持多个客户端同时发送数据报（UDP 无连接，但维护会话映射）
- 可配置超时时间，超时后清理会话

**技术要点：**
- 使用 `UdpClient` 实现双向转发
- 维护客户端会话映射表（用于回传响应）
- 注意 UDP 是无连接协议，与 TCP 生命周期不同

### ENG-006: HTTP 正向代理（8 点）

> 作为开发者，我希望运行 HTTP 正向代理。

**验收标准：**
- 配置浏览器使用 `localhost:8080` 作为 HTTP 代理，正常浏览网页
- 支持 HTTP/1.1，正确处理 `CONNECT` 方法（用于 HTTPS 隧道）
- 支持 `Host` 头解析和转发

**技术要点：**
- 使用 `HttpListener`（AOT 安全，无 ASP.NET 依赖）
- 解析 HTTP 请求，重建并转发到目标服务器
- `CONNECT` 方法降级为原始 TCP 隧道（TLS 透传）

### ENG-007: 统一处理器接口（3 点）

> 作为开发者，我希望引擎对所有协议处理器暴露统一接口。

**验收标准：**
- `ITunnelHandler` 被 TCP、UDP、HTTP 三种处理器实现
- 公共生命周期方法：`StartAsync`、`StopAsync`、`GetStats`
- 新协议处理器只需实现该接口即可接入引擎

### ENG-008: 结构化隧道统计（2 点）

> 作为运维，我希望通过编程方式获取结构化隧道统计信息。

**验收标准：**
- `TunnelStats` 记录包含：`BytesIn`、`BytesOut`、`ActiveConnections`、`Uptime`、`Status`
- 可通过 `ITunnelHandler.GetStats()` 获取
- 统计实时更新（每次查询返回最新快照）

---

## 交付物

- [ ] `UdpForwardHandler` 实现（`LocalProxy.Engine`）
- [ ] `HttpForwardHandler` 实现（`LocalProxy.Engine`，基于 HttpListener）
- [ ] `ITunnelHandler` 接口稳定版
- [ ] `TunnelStats` 记录类型
- [ ] UDP 和 HTTP 处理器的集成测试
- [ ] 三种协议的吞吐量基准测试文档

---

## 完成标准

- [ ] 4 个用户故事验收标准全部通过
- [ ] 三种协议（TCP/UDP/HTTP）在 macOS + Windows 上手动测试通过
- [ ] 集成测试覆盖 TCP、UDP、HTTP 的核心转发路径
- [ ] `ITunnelHandler` 接口文档完整
