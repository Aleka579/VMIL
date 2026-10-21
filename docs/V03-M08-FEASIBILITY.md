# M08：底层身份信息修改可行性调研

历史范围说明：本报告保留 M08 当时的源码/采集状态。之后 [M09](V03-M09-RESULTS.md) 已实现三个标识的显式只读采集和身份分析，并通过本机功能验收；下文“采集器未输出”等属于 M08 快照。修改支持结论和新身份写入批准为 0 的边界不变。

后续补证（2026-10-19）：[M11取得官方17系列完整手册](V03-M11-MAC-UUID-EVIDENCE.md)，已核实MAC界面/静态管理、UUID生命周期及手工指定，不再仅依赖搜索摘要或旧KB。下文保留M08当时的资料获取限制；精确17.5.2自动通道、缺失键恢复、多键语义和实际闭环仍未获证，生产准入仍0。

- 调研日期：2026-10-18，Asia/Taipei（UTC+08:00）。
- 方法：官方资料、已安装文件元数据、随安装提供的静态 API 契约、当前源码及既有测试记录审查。
- 本轮未调用 VMware API、未运行客户机命令、未修改真实 VM、未创建 VM 副本、未操作电源或快照。
- 结论适用范围：VM Identity Lab 当前源码与本机可核实安装；不将其他 VMware 产品或更高版本能力自动套用。

## 1. 结论

**M08 静态可行性调研完成；新增获准身份写入字段为 0。** 后续可以先做 M09 只读身份分析。不能把“去虚拟化，让所有软件都判断为物理机”作为可承诺的产品验收目标。

能确认已有实际修改证据的是 v0.2 的内存和 vCPU，且仅限既有精确策略；它们不消除其他虚拟化信号。MAC 地址手动分配、复制/迁移时的 UUID 保留或生成新值，存在官方能力资料，但目标安装版本的完整支持依据、App 后端与往返恢复均不足，当前只能列为后续研究候选。CPUID、SMBIOS 厂商等其他标识没有取得满足本项目准入要求的修改证据，当前不支持修改。

本机安装包还声明了通用配置参数接口，必须保留这一发现。它不能证明任意身份字段可以写入，也不能据此断言官方 API 永远不能配置身份信息。后续需要分别核实接口运行时、字段支持、适用版本、状态保护和恢复语义。

## 2. 证据等级和用词

| 标记 | 含义 | 能否批准新生产写入 |
| --- | --- | --- |
| H：历史实测 | 原记录在明确版本、值和拓扑中完成真实验证 | 仅沿用该范围，不扩展到新字段 |
| D：官方资料 | 官方说明存在相关能力，但仍需核对产品/版本/通道 | 否 |
| C：本机契约 | 随当前安装提供的接口/字段声明，尚未调用验证 | 否 |
| U：依据不足 | 没有得到符合项目要求的支持证据 | 否，保持只读或不支持 |

“当前不支持修改”是本项目能力边界；“未确认”不是“绝对无法修改”。Windows 查询属性只读，也不能推导出虚拟化平台绝对不能配置对应信息。字段在响应中出现、在目录中定义或在 VMX 中出现，均不等于该字段有受支持的写入通道。

## 3. 本机版本与静态契约核实

本轮从已安装文件的版本资源读取到 Workstation 与 vmrun 均为 **17.5.2 build-23775571**。vmrest 的文件版本资源为空，本轮不执行它的帮助命令；历史记录中 `1.3.1 build-23775571` 来自以前实际执行的帮助输出，不将在线 API 文档标题 `1.2.1` 当作本机二进制版本。

| 静态对象 | 本轮结果 |
| --- | --- |
| 安装附带 `swagger.zip` | 1,425,876 字节 |
| ZIP SHA-256 | `c417eedca1f81b0d105739e2245f03e87cd7fc68c2b9b97209a422ced5d3a940` |
| 审查条目 | `json/swagger_WS.json`，96,358 字节；未使用 Fusion/Player 契约 |
| 条目 SHA-256 | `71c8094a4ab765151111ac4fd68a2bf8ace1061d470ef4ac2c5dc6f86aebb3f7` |
| 格式和范围 | Swagger 2.0，`/api`，19 个路径、28 个操作；info 标题/版本含模板占位符 |
| 读取方式 | 将固定 JSON 条目作为数据解析；未执行 ZIP 内的网页或脚本 |

本机核查摘要保存在本 worktree 忽略目录 `local-data/m08-20261018/contract-review.json`；固定数据读取工具为同目录 `inspect-contract.py`。二者仅作本轮私有审查依据，未把厂商完整契约或真实 VM 数据纳入源码。

### 3.1 有限字段与通用接口必须分开

| 本机契约声明（省略 `/api` 前缀） | 输入/输出要点 | 本轮判断 |
| --- | --- | --- |
| `PUT /vms/{id}` | `VMParameter` 输入只有 `memory`、`processors` | 与当前 App 有限更新实现吻合；不是任意身份更新接口 |
| `PUT /vms/{id}/nic/{index}` | `NICDeviceParameter` 输入为 `type`、`vmnet` | 返回对象含 `macAddress`，不等于请求支持设置 MAC |
| `GET /vms/{id}/nic` | NIC 返回对象含 `macAddress` | 有读取声明；本轮未调用，不代表客户机实际读取已实现 |
| `GET /vms/{id}/params/{name}` | 操作 `getVMParams`，返回名称/值结构 | 通用读取声明；运行时和具体字段适用性未验证 |
| `PUT /vms/{id}/configparams` | 操作 `configVMParams`；`ConfigVMParamsParameter` 输入为字符串 `name`、`value` | 通用写入声明；没有逐字段支持、允许值或事务保证，不能直接进入白名单 |
| `PUT /vmnet/{vmnet}/mactoip/{mac}` | 请求设置绑定的 IP | 是宿主网络 DHCP 绑定管理，不是修改 VM 网卡 MAC |
| `POST /vms` | 克隆输入 `name`、`parentId` | 不能据此推导有任意 UUID 设置参数；本轮未克隆 |

官方在线 [操作索引](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/operation-index/)（页面标题 1.2.1）未列出上述 `params/configparams`，本机 Workstation 静态契约则列出。差异已确认在资料层存在，服务端是否实现以及支持哪些字段仍未验证；不猜测差异原因，不尝试未获准的 PUT。

## 4. 可修改性矩阵

| 标识/信息 | 读取或观测来源 | 修改支持证据 | 当前 App / 本轮决定 |
| --- | --- | --- | --- |
| 内存、vCPU | VMX/API 声明；客户机系统观测 | H：v0.2 官方有限 API 已在独立副本实测 | 已实现：仅 17.5.2.23775571、HW19、每插槽 2 核，4096↔6144 MiB、2↔4 vCPU；本轮未重测，不属于身份隐藏 |
| VM 网卡 MAC | C：NIC 返回对象；客户机实际采集待实现 | D：[KB324973](https://knowledge.broadcom.com/external/article/324973)记录 hosted VM 手动分配 MAC；环境列表为旧 Workstation 等版本，未明确 17.5.2；17.0 手册搜索摘要亦提到高级网卡设置，但正文未能读取 | 官方能力存在资料依据；当前 App 不支持修改。目标版本、地址约束、官方通道与恢复待核实，不开放任意值或物理机身份复制 |
| VM UUID / SMBIOS Type 1 UUID | Windows `Win32_ComputerSystemProduct.UUID` 为只读观测；当前采集器未输出 | D：[KB320246](https://knowledge.broadcom.com/external/article?legacyId=1541)讨论 Workstation 移动/复制时保留或新建 UUID，但 Environment 又列 ESXi 版本，不能据此确认 17.5.2 | 保留生命周期候选，当前 App 不支持修改；不承诺任意 UUID 写入，不把克隆生成新 UUID 等同于通用 setter |
| SMBIOS 系统厂商、型号 | 当前 `Win32_ComputerSystem` 只读采集 | U：未取得目标版本逐字段官方配置和恢复依据；通用参数声明不补足这一缺口 | 当前不支持修改，保留实际来源及虚拟化证据解释 |
| SMBIOS 序列号、BIOS 信息 | BIOS 版本已采集；序列号只有敏感字段定义 | U：未确认厂商允许的逐字段编辑和一致性要求 | 当前不支持修改，不从序列号字段存在推导能力 |
| CPU 型号/品牌、Hypervisor 位和厂商叶 | 当前标准 CPUID 只读观测；逻辑处理器数单独采集 | U：未取得身份改写所需的目标版本官方支持证据 | 当前不支持修改；不加入 CPUID 隐藏开关或私有参数，不因单个信号消失判定物理机 |
| ACPI 表及固件表内容 | Windows 提供只读固件表查询；当前产品未实现此项采集 | U：未取得受支持的内容改写与恢复通道 | 当前不支持修改；不写固件、替换平台二进制或重置 NVRAM |
| PCI/PnP 厂商、设备 ID、虚拟磁盘型号/序列号 | 当前仅有 VMware PnP 设备数；逐设备身份/磁盘身份采集未实现 | U：没有符合准入要求的逐标识修改依据 | 当前不支持修改；选择设备类型不等于能够任意设置厂商/设备身份，不修改磁盘数据 |
| VMware Tools 痕迹 | 当前注册表只读安装/版本观测 | 软件安装状态，不是底层硬件标识 | 不提供隐藏/移除流程；不把卸载 Tools 当作本功能的解决方案 |
| 固件类型、客户机类型、嵌套虚拟化选项 | 可解析的配置声明；具体兼容性需各自依据 | 不同于底层身份内容编辑；本轮未完成这些选项的写入准入 | 不新增可写项，不把配置选择描述为去虚拟化能力 |

其中 U 类不是对所有 VMware 版本技术能力的否定；它表示本轮无法给出可批准、可维护、可恢复的实现方案。不得用网络流传的未验证参数补全矩阵。

### 4.1 副作用与恢复缺口

| 候选/类别 | 主要影响 | 获准实验前必须补足 |
| --- | --- | --- |
| MAC | 网络身份、地址冲突、DHCP 租约及依赖原地址的网络配置可能变化 | 厂商允许值、适用版本、VM/客户机对应关系；原值恢复和连通性独立验证；不能仅以 VMX 备份承诺全部网络状态恢复 |
| UUID | 克隆/迁移的身份关系，以及依赖身份的软件配置可能受影响 | UUID 类型/派生关系、实际变更资源、明确支持的流程；保留/生成/恢复的边界，禁止以授权规避为测试目标 |
| 固件/设备身份 | 可能涉及多处配置和启动兼容性；当前无法给出完整恢复保证 | 有逐字段官方支持和备份范围之前不做写入；不得假设 VMX 字节备份覆盖固件或磁盘 |
| 通用参数接口 | 接口接受请求不等于字段受支持；状态竞争、部分应用或响应丢失的语义不明 | 目标版本资料、每个获准字段、值约束、状态拒绝、结果复核及未决恢复；不制作任意名称/值编辑器 |

## 5. 源码与历史证据审查

- [WorkstationRestClient.cs](../src/VMIdentityLab.VMware/WorkstationRestClient.cs)：生产更新只映射 `memsize → memory`、`numvcpus → processors`，其他键拒绝；没有 MAC/UUID/通用参数写入实现。
- [VmxApiOperation.cs](../src/VMIdentityLab.VMware/Configuration/VmxApiOperation.cs)：发布策略登记精确版本、HW19、每插槽 2 核和上述有限数值；不能从“VMware >=17.5.2”推导其他版本获准。
- [NativeWindowsDataSource.cs](../src/VMIdentityLab.Windows/NativeWindowsDataSource.cs) 与 [FindingCatalog.cs](../src/VMIdentityLab.Core/Reports/FindingCatalog.cs)：当前正常采集 14 项。目录另定义 UUID、MAC、序列号三个 Identifier 项，但原生采集器未输出这三项；目录不是已完成采集的证明。M09 需处理真实来源、故障、隐私和旧报告兼容。
- [v0.2 全量实测](V02-FULL-TEST-RESULTS-2026-10-17.md)：既有独立副本的有限配置应用、拒绝、恢复证据；不是本轮身份候选的验证。
- [CPU 修复和 CI 包验收](CPU-FIX-ACCEPTANCE-2026-10-18.md)：130 单元、106 模拟集成及最终原包客户机严格 23/23 是已有结果。本轮未修改产品或重跑这些测试。
- [2026-10-11 只读依据](VMWARE-17.5.2-EVIDENCE.md)中的“白名单为空”为当时历史状态；当前以 [配置政策](VMWARE-CONFIG-POLICY.md) 和发布策略源码为准。

## 6. 官方资料索引与限制

所有在线来源于 2026-10-18 核查；不使用第三方参数合集或 ESXi 专用文档替代 Workstation 支持依据。

| 来源 | 本轮获得的事实 | 适用限制/读取结果 |
| --- | --- | --- |
| [Workstation API 操作索引](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/operation-index/) | 在线声明的操作集合 | 正文已读取；标题 1.2.1；与本机静态通用参数声明有差异 |
| [Update VM](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/put/) | 有限更新请求为 memory/processors | 正文已读取；不推导其他接口不存在 |
| [Update NIC](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/nic/index/put/) / [Get NICs](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/nic/get/) | 网卡更新请求与返回 MAC 属性分开 | 正文已读取；与本机对应结构一致；未实际调用 |
| [Broadcom KB324973](https://knowledge.broadcom.com/external/article/324973) | hosted VM MAC 手动分配的官方说明 | 正文已读取；Environment 未列目标 17.5.2，不能扩大支持范围 |
| [Workstation Pro 17.0 手册 PDF](https://techdocs2-prod.adobecqms.net/content/dam/broadcom/techdocs/us/en/pdf/vmware/desktop-hypervisors/workstation/vmware-workstation-pro-17-0.pdf) | 搜索摘要提及网卡高级设置中的 MAC 分配 | 只见摘要；正文访问 403，未取得完整允许值/版本依据，未绕过访问限制 |
| [Broadcom KB320246，legacyId=1541](https://knowledge.broadcom.com/external/article?legacyId=1541) | 移动/复制场景的 UUID 保留或生成工作流 | 正文已读取；Workstation 描述与 ESXi 环境列表混合，不证明 17.5.2 任意 UUID setter |
| [Win32_ComputerSystem](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystem) | Manufacturer、Model、HypervisorPresent 为读取属性 | 正文已读取；Windows 观测接口说明，不是 VMware 配置能力说明 |
| [Win32_ComputerSystemProduct](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystemproduct) | UUID 来自 SMBIOS Type 1，属性只读 | 正文已读取；不代表本项目已采集 UUID |
| [GetSystemFirmwareTable](https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemfirmwaretable) | 获取原始固件表数据的 Windows 接口 | 正文已读取；读取能力不等于固件编辑能力 |
| 本机附带 Workstation Swagger | 上述 19 路径、28 操作及数据结构 | 已静态解析并计算哈希；未执行其中脚本，未测试服务端 |

## 7. 未验证事项与后续实验设计

本轮不执行下列实验，也不把此清单作为授权：

1. **目标版本补证**：取得能完整审阅、适用于目标 Workstation 版本的 MAC/UUID 官方依据。不能取得时继续保持候选/只读。
2. **传输能力补证**：分别确认通用参数接口是否在目标运行时可用、每个拟用字段是否获厂商支持。先明确字段再评审通道；不穷举私有键、不发送任意身份写入探测。
3. **独立副本往返**：以后经具体授权才创建完整 VM 副本、核实磁盘/快照链和恢复范围，记录基线。MAC 仅研究合法唯一性管理；UUID 仅研究复制/迁移的身份生命周期，不克隆物理机身份。
4. **分开核实结果**：配置读取、客户机独立观测、已授权测试程序行为、恢复后结果分别记录；确认其他字段保留、网络/启动可用。某个检测器结论改变不构成唯一验收。
5. **进入生产前**：还需 App 闭环、版本化有限白名单、预览失效、并发/状态拒绝、持久化未决与恢复冲突等证据。沿用既有保护，不开放 VMX 直接替换回落。

## 8. M08 完成判定

八条研究验收见 [M08 任务](../tasks/modules/M08-feasibility.md)：范围、基线、契约、官方来源、能力矩阵、源码核实、风险/恢复及后续门槛。它们完成的是可行性审查，未验证的运行时问题已明确列出，不将“候选实验未做”改写为通过。

**继续建议：**后续开启 M09 只读分析；M10–M13 按 [v0.3 计划](V03-PLAN.md) 的门槛推进。M11 新身份写入目前不满足准入条件，不能承诺本版必然交付修改能力。
