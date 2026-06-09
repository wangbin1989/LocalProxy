# Sprint 3 — 配置系统与预置模板

**周期：** 第 6-7 周（2 周）  
**目标：** 实现 JSON 配置模型、校验、持久化和 8 套常用软件预置模板

---

## 冲刺目标

实现 `LocalProxy.Config` 项目，提供声明式 JSON 配置管理，包含格式校验、文件监控热加载、配置版本迁移，以及 SQL Server、Redis 等 8 套开箱即用的预置代理模板。

---

## 用户故事

### CONF-001: JSON 配置文件（5 点）

> 作为用户，我希望通过 JSON 文件定义代理配置。

**验收标准：**
- 创建包含 3 个代理条目的 `config.json`，成功加载
- 配置解析使用 `System.Text.Json` 源生成器（AOT 安全）
- 配置模型包含：Name, Protocol, LocalPort, RemoteHost, RemotePort, MaxConnections, Timeout

**技术要点：**
- 文件路径：`~/.localproxy/config.json`（macOS/Linux）`%APPDATA%\LocalProxy\config.json`（Windows）
- 使用 `JsonSerializerContext` 源生成器

### CONF-002: 配置校验（3 点）

> 作为用户，我希望配置校验拒绝无效条目并给出明确错误。

**验收标准：**
- 无效端口（负值、>65535、冲突）→ 明确错误信息
- 必填字段缺失 → 指出具体缺失字段
- 重复名称 → 指出重复项
- 不支持的协议类型 → 列出支持的协议

### CONF-003: 预置代理模板（5 点）

> 作为新用户，我希望有常用数据库和服务的预置代理模板。

**验收标准：**
- 8 种预置模板可通过 API 查询：SQL Server (1433)、Redis (6379)、MySQL (3306)、PostgreSQL (5432)、MongoDB (27017)、RabbitMQ (5672)、Elasticsearch (9200)、Kafka (9092)
- 模板以嵌入式资源编译，无需外部文件
- `PresetProfiles.List()` 返回全部模板，`PresetProfiles.Get(name)` 返回指定模板

### CONF-004: 自定义代理配置（3 点）

> 作为用户，我希望自定义代理配置支持完整参数控制。

**验收标准：**
- 可配置全部参数：本地端口、远程主机、远程端口、协议、最大连接数、超时时间
- 所有参数均有校验
- 提供默认值：最大连接数=100，超时=30s

### CONF-005: 配置热加载（5 点）

> 作为用户，我希望配置文件被修改后隧道自动重载。

**验收标准：**
- 服务运行时修改 `config.json`，5 秒内隧道自动更新
- 新增的隧道自动启动，移除的隧道自动停止
- 已有连接不受影响（不中断活跃连接）
- 使用 `FileSystemWatcher` + 防抖（去重 500ms 内的多次触发）

---

## 交付物

- [ ] `LocalProxy.Config` 项目含完整配置模型
- [ ] JSON Schema 校验逻辑
- [ ] `System.Text.Json` 源生成器（AOT 安全序列化）
- [ ] 8 套预置代理配置模板（嵌入式资源）
- [ ] `FileSystemWatcher` 防抖热加载
- [ ] 配置迁移机制（`version` 字段 + 升级路径）
- [ ] 配置校验单元测试（正常 + 边界）
- [ ] 预置模板完整性测试

---

## 完成标准

- [ ] 5 个用户故事验收标准全部通过
- [ ] 配置模型单元测试覆盖率 >= 90%
- [ ] 热加载在 macOS + Windows 上手动验证通过
- [ ] 8 套模板参数已验证为各服务的标准默认端口
