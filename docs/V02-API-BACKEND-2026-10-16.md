# v0.2 官方API后端与本地并发测试结果

日期：2026-10-16，Asia/Taipei。起始提交6748019，分支`codex/v02-config-management-20261013`。本轮完成无需用户协助的生产代码接入、合成服务/临时文件测试和普通权限宿主界面验证。没有连接真实API、索取凭据、重建副本、操作VM电源或配置，也未生成新发布包。

## 已完成的代码

| 范围 | 实现 |
| --- | --- |
| 官方API传输 | `WorkstationRestClient`共享清单、状态、设置GET及`PUT /api/vms/{id}`；只发送本次改变的memory/processors，校验目标ID和严格字段，不自动重试 |
| 生产App后端 | `VmxApiConfigManager`接入原确认/历史工作流；实际安装版本、候选、关机、目标映射、API/VMX一致性、备份与提交前后核对，不回落直接替换VMX |
| 同目标互斥 | `.vmidentitylab-api.lease`的`FileShare.None`句柄贯穿事务；不同管理器和实际Windows子进程验证，相同目标拒绝并发，不同目标可独立进行 |
| 未决操作 | 发请求前刷盘并重读intent；超时、断线、异常响应、部分变更保留pending和备份，阻止新实例/重启后重放；无自动回滚或核销 |
| 取消与拒绝 | 请求前取消不发PUT；请求后取消继续核实。明确4xx拒绝还须证明ID、API设置及字节未变，才归档rejected；不自动重试 |
| 恢复与历史 | schema2记录后端和前后参数，兼容schema1；API恢复只反向修改收据中的内存/CPU，保留其他启动后内容，拒绝候选/拓扑后续变更和备份篡改 |
| 界面 | 中英未决/占用/停止提示，保留恢复位置，打开时即显示pending；切换目标清除旧提示和路径，生产确认仍禁用 |

实现说明见[提交与恢复设计](CONFIGURATION-WRITE-DESIGN.md)及[配置政策](VMWARE-CONFIG-POLICY.md)。没有增加第三方依赖、框架或审批开关，八模块仍位于原四个源码项目内。

## 实际验证

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| Release解决方案构建 | 0警告、0错误 | WORK_LOG.md；SDK10.0.401，Runtime10.0.12，单进程/既有缓存 |
| 单元测试 | 127/127，0失败、0跳过；新增21例 | `artifacts/test-results/20261016-api-backend/api-unit-final.trx` |
| 模拟API/临时文件集成 | 86/86，0失败、0跳过；新增44例 | `artifacts/test-results/20261016-api-backend/api-files-final.trx` |
| 最后界面修正后的App构建 | 0警告、0错误 | WORK_LOG.md；仅调整打开/切换配置时的提示与路径，后端测试代码未变 |
| 普通权限宿主UI | 12项检查通过，两个App实例均正常关闭 | 私有`local-data/test-results/api-backend-20261016/ui-check.json`、控件树和`ui-actions.jsonl` |

集成测试使用独立临时合成VMX和内存中的`HttpMessageHandler`，不连接本机8697或其他服务，不包含真实磁盘/VM引用。实际子进程只尝试打开测试租约，证明Windows句柄约束；不构成真实VMware并发验证。

新增测试覆盖：

- 同目标两管理器、Windows子进程占用、不同目标并发和完成后释放；旧预览与未决记录拒绝。
- 运行/挂起/暂停/未知状态，以及准备中改变目标ID、版本、设置、内容或电源，不发PUT。
- 备份、暂存和intent写入失败/内容损坏，不发PUT且保留目标；用户请求前取消。
- 超时、断线、错误/格式异常响应、服务端错误、部分409、外部编辑和启动后未知结果；新管理器不能重放。
- 客户端超时后服务端延迟成功：当前文件暂时未变也不能清除pending；请求发出后取消仍核实并保存历史。
- 成功应用、启动后其他字段变化、参数恢复和历史读取；历史保存失败只重试历史，不重发更新。
- 旧历史兼容、后端伪装拒绝、备份篡改、后续候选编辑和禁止API收据回落文件恢复。
- 本机根地址、字段/ID严格解析、路径段编码、错误正文不回显、响应上限及响应体读取超时。

普通权限UI分别检查中英文未决提示、两项差异和禁用确认；最后一次App构建另验证打开pending文件时无需预览即可提示、展开可见恢复路径、切换到另一合成目标清除旧提示/路径。没有创建提交租约，原合成pending证据仍保留。

## 命令与失败修复

运行目录为项目根；使用已有项目内SDK及NuGet缓存，关闭.NET/测试遥测，没有安装依赖。实际标准入口：

```powershell
& artifacts/tooling/dotnet/dotnet.exe build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers
& artifacts/tooling/dotnet/dotnet.exe test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename api-unit-final.trx --results-directory artifacts/test-results/20261016-api-backend
& artifacts/tooling/dotnet/dotnet.exe test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename api-files-final.trx --results-directory artifacts/test-results/20261016-api-backend
```

首次测试编译误用了内部转换方法，改为独立模拟服务实现；首轮单元117/125，8项只读降级遗漏`InvalidDataException`，补齐后通过。补充intent核对时的Span跨await编译错误已修正为先读取再比较。失败TRX保留，没有关闭分析器或降低标准。最终213项通过后，仅修正两个UI状态行为并重新构建/实际交互，没有重复扩大后端测试。

## 放行边界与下一步

此前[原生API独立副本往返](V02-CONFIG-ACCEPTANCE-2026-10-16.md)已证明Workstation17.5.2、hardware19、每插槽2核的4096/6144MiB和2/4vCPU候选兼容。该结果不是本轮App路径的真实验收，副本已按用户要求删除。

本地租约只能协调遵守它的App实例，外部写入/启动检测也不能提供全局锁。官方[更新接口](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/put/)列出409资源冲突，没有声明CAS/幂等请求契约；不能据此推断所有并发场景安全。因此`ReleasedVmxApiWritePolicy`继续拒绝生产写入，CFG-01–06仍0/6，总计29/37。

剩余真实验收须按具体目标准备：先验证provider运行/挂起拒绝、并发启动及外部修改冲突/保全，证据充分后评审有限策略，再验证App确认、应用、重启读取历史与参数恢复，闭环未通过不交付可写版本。不得通过测试策略、环境变量或删除pending放行。后续实际VM操作/副本另依授权规则执行，本轮不向用户请求凭据或操作。

当前源码仍0.2.0-alpha.1；旧9cfa28a Alpha ZIP未重建、不包含本次实现。远程CI、私密安全渠道、公开仓库/推送/Release也未执行；完整v0.2仍未就绪。
