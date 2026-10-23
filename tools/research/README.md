# M11 官方参数通道探针

这是独立研究工具，不随 App 发布，不是 M10 计划执行器。只验证工具自己生成的无磁盘、断网、不启动 VM 的两个**已存在**字段：静态 `ethernet0.address`、`uuid.bios`。不接受真实 VM 路径、任意字段或用户标识输入。

2026-10-20已[修复404](../../docs/V03-M11-404-FIX.md)并完成[真实复验](../../docs/V03-M11-REAL-API-RETEST.md)：精确vmrest实际绑定PUT `/api/vms/{id}/params`，成功响应为200空正文，与随附Swagger不一致。工具固定使用已核实路由并兼容该空响应，仍须独立API/VMX复核，不自动切换/重试。43项离线通过，已有静态MAC/UUID有限真实配置应用/原值恢复通过，本轮新探针及服务已清理；生产准入不变，不重放本轮会话。

上一轮具体授权已执行结束，结果见[实际记录](../../docs/V03-M11-REAL-API-RESULTS.md)：注册/读取成功，UUID参数PUT404、MAC流程404，原值确认未变且探针已清理。当时35项离线测试通过；不重放旧对象/会话。

需要 Windows、已有 Python 3.14+，只用标准库和 Windows 原生 API；不安装依赖、驱动或服务。实际操作限已核查的 Workstation `17.5.2 build-23775571` 分发文件及契约哈希，升级或二进制不同会停止，不回退 VMX 写入。具体依据及边界见 [M11 缺口处理](../../docs/V03-M11-CHANNEL-RECOVERY.md)。

## 离线执行

在项目根目录运行；这两条命令不会调用 VMware：

```powershell
python -I tools/research/test_identity_api_probe.py -v
python -I tools/research/identity_api_probe.py prepare
```

第一条使用假 API、临时合成文件和仅绑定`127.0.0.1`临时端口的HTTP替身，验证实际请求字节与严格路由；另检查原生文件独占读取、App文件租约及短暂测试监听进程识别。所有监听器在测试后关闭，不输入真实凭据、不连接VMware。

第二条只创建 `local-data/m11-api-probes/<随机UUID>/`：原始配置、原始字节备份、固定字段的前后值、租约文件。无磁盘、ISO、快照、共享目录或开机连接网卡；不注册、不启动。不将这些私有文件提交或放入报告。

## 需要具体授权的实际探针

先审阅并批准包含确切目录、字段差异、原值恢复和清理的方案。`--confirm` 只防止选错对象，不代替用户授权。

确认本机已有 vmrest 以 `127.0.0.1:8697` 运行后：

```powershell
python -I tools/research/identity_api_probe.py execute --fixture <本工具生成的完整目录> --confirm <该目录UUID> --installation <Workstation安装目录>
```

工具验证完整版本字符串、契约/程序哈希、监听 PID 和实际进程路径，然后打印awaiting_local_credentials并打开原生凭据窗口，提示内容包含“VM Identity Lab M11 API 探针”。凭据只在窗口填写，不放入命令行、文件或聊天；不承诺 Python 字符串可完全从内存擦除。原生提示可由系统承载，Python的MainWindowTitle为空不能据此判断没有窗口；先核实状态/事件，不重开并发认证。工具不自动启动 vmrest 或重设账号。

一次 `execute`：

1. 经官方 `POST /api/vms/registration` 注册唯一探针；不复制/修改已有 VM。
2. 确认唯一映射、完全关机、字段/API/VMX一致、无锁，写入并落盘 intent 后再次核实，单次 PUT 修改静态 MAC。
3. 独立读取 API 和 VMX，确认所有其他键未变；经相同保护单次 PUT 恢复原 MAC。
4. 以同样步骤修改并恢复已有 BIOS UUID，最多共四次配置 PUT，无电源操作。
5. 保存配置语义恢复结果及原始字节是否相等；本构建成功PUT可无正文，收据只记状态/长度/类型，仍须独立读回。API可规范化换行/行顺序，语义恢复不保证字节相等；不把这些配置结果当作客户机/网络验收。

记录使用独占创建并 `fsync` 的 `event-NNN.json`。完成后 `execute`/`run` 不能重放；探针失败也不会在 `finally` 中写回、盲目重试、清除 intent、删除锁或切换入口。

若注册响应丢失，可在重新核实唯一注册后单独 `run`，不会再次注册。若配置响应丢失，必须先调查本机结果；只有原方案覆盖且明确决定继续恢复时才运行：

```powershell
python -I tools/research/identity_api_probe.py restore --fixture <原探针目录> --confirm <原UUID> --installation <原Workstation目录>
```

`restore` 只读取并区分原状态、已应用状态或冲突。原状态不再发送逆向写入；仅已确认的单字段变更可生成新的恢复 intent、重新检查后写回原值。若上一逆向请求未决且仍为目标值，不再发第二次逆向请求，停止调查。值/映射/状态/版本不明、关联字段或后来修改冲突时停止；保留证据和对象供审查，不自动覆盖或删除。该工具不是 VMware 全局锁，也不证明外部开机与最终检查之间不存在竞争。

`continue-uuid`只用于已授权两字段方案在MAC尝试中断后的有限继续：先对同一MAC未决执行上述显式核实/恢复，或重新验证已证明未变的拒绝状态，然后只尝试尚未发送的UUID；不再注册或尝试MAC。UUID成功仅记uuid-completed，不表示MAC通过。该模式不能用于新的任意字段或复用已经结束的授权。

传输拒绝输出固定资源类别/方法/状态、受限错误分类及整数错误码，不输出VM ID、厂商message或正文。错误正文限长读取，阶段/字段/正向或恢复分别标明；只有PUT的4xx且独立核实请求前全部API/VMX/哈希/状态未变后记rejected-unchanged，检查/读回GET失败不误记。reverse=true时该“未变”可能仍是已应用状态，不是恢复通过。任何不一致仍保留intent，禁止重放。

成功或明确拒绝后，按获准方案移出 VMware 库并清理探针，先保留私有证据。清理GUI可能增写vmxf/vmsd/VMX元数据，须在最终GUI操作之后、删除目录之前追加归档差异和哈希；早期API归档不能冒充最终GUI状态。代码没有 DELETE 或清理入口，不能猜测 API DELETE 的数据删除范围。

`lease`上下文提供自身打开的文件句柄，调用者通常只需`with lease(...)`。归档租约文件时须使用该句柄读取；Windows字节锁会拒绝其他句柄读取锁定区域，不释放锁或删除租约文件来复制。只清理本次具体批准对象，库条目及持久清单分别核实。

原生接口依据：[文件版本查询](https://learn.microsoft.com/en-us/windows/win32/api/winver/nf-winver-verqueryvaluew)、[监听进程结构](https://learn.microsoft.com/en-us/windows/win32/api/tcpmib/ns-tcpmib-mib_tcprow_owner_pid)、[不持久化凭据标志](https://learn.microsoft.com/en-us/windows/win32/api/wincred/nf-wincred-creduipromptforcredentialsw)。本轮原生窗口及实际API认证已通过；这不表示写入通过。
