# VMware 17.5.2：只读配置与状态依据

历史范围说明（2026-10-18）：本文保留 2026-10-11 的只读阶段结论，包括当时“白名单为空”等描述。之后有限内存/vCPU 已按[现行配置政策](VMWARE-CONFIG-POLICY.md)完成精确范围实测；新的身份能力调查见 [M08 报告](V03-M08-FEASIBILITY.md)。不得把本页历史状态当作当前策略，也不得把后续接口声明当作新字段写入批准。

核查日期：2026-10-11。本机Workstation/vmrun为17.5.2 build-23775571；注册表ProductVersion17.5.2.23775571。本文区分官方功能、VMX参数映射、实际验证和写入批准。

## 配置分类

| 项目 | 依据与结论 | 当前软件处理 |
| --- | --- | --- |
| virtualHW.version | [Broadcom KB315655](https://knowledge.broadcom.com/external/article/315655/virtual-machine-hardware-versions.html)列17.x的硬件版本20，另列17.6的21，并说明较高硬件级别不能在较低支持级别产品中启动；较旧级别可能损失功能 | 对实际识别的17.5.2显示已确认版本范围或不支持；19/20与21/22分类已模拟验证，真实19和合成21的GUI已验证 |
| memsize | [Workstation API的VMMemory](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/data-structures/VMMemory/)明确内存字段及单位；部分旧内存KB已停止维护 | 只读取VMX声明；没有把旧KB、当前营销上限或API字段直接当作17.5.2 VMX写入批准 |
| numvcpus | [VMProcessors](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/data-structures/VMProcessors/)定义处理器核心数量；API的VMCPU包含processors字段 | VMX映射与关联拓扑约束尚待目标版本依据及副本测试，保持未核实 |
| firmware / guestOS | 本轮尚未完成17.5.2具体参数/取值依据核查 | 保留可解析的公开声明，支持状态仍未核实 |
| 重复键、未知项与其他产品版本 | 不从单个值或更高版本号推导支持 | 重复项显示歧义；未评审项和其他目标版本显示未核实 |

“版本依据已确认”仅描述只读兼容范围，不保证所有设备/客户机功能，也不允许修改硬件版本。当前可写白名单仍为空，内存/vCPU仅为后续候选。

## 电源状态来源

[官方Get Power State](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/power/get/)使用GET /api/vms/{id}/power，枚举poweredOn、poweredOff、paused、suspended；[Get All VMs](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/get/)提供VM ID与路径对应关系。

本机vmrest帮助实际报告1.3.1 build-23775571；随安装swagger.zip的Workstation契约包含上述路径、字段和枚举。在线API页面标题1.2.1与本机二进制版本为不同层次；本地info版本是模板占位，不编造其数值。

实现只向显式连接的本机接口发送GET：先将用户选定VMX匹配到唯一注册ID，再读取状态。缺失注册、重复路径/ID、失败HTTP响应、无效JSON、超时、暂停或新枚举均保持Unknown；调用者取消不伪装成成功。无凭据或未连接时仍默认Unknown。

App连接窗口使用本机vmrest专用账号，密码只用于本次请求，不保存到项目或程序设置；官方vmrest自身的账号配置需要用户另行授权创建。软件不会自动运行vmrest、修改VM电源或写入VMX。SDK/System.Net.Http为既有平台依赖，无新增第三方包。

当前通过：81/81单元、10/10文件、真实VMX只读支持分类，以及获授权的真实运行、挂起、完全关机和API不可用降级。前三种导出来源为vmrest.power，失败为unknown/unavailable；目标已恢复运行，临时API已停止。具体时间、界面与报告证据见[本轮结果](READINESS-TEST-RESULTS-2026-10-11.md)。进入v0.2写入之前还须满足[配置政策](VMWARE-CONFIG-POLICY.md)，不能把一次状态快照当作后续写入许可。

## 测试与产物

私有证据：local-data/test-results/readiness/20261011-130327/，真实VMX/报告/控件树不提交。新完整验证包411条目、146949453文件逻辑字节，ZIP65368827字节，SHA256 674BEA31BD97DB00C1F2D19B55AEA32A2884F47E5E191E20EBFB0C67A32AE12B。它是基线015c387加本轮待提交修改的验证快照，包内清单保存各文件哈希；不是正式Release，原Alpha ZIP未覆盖。
