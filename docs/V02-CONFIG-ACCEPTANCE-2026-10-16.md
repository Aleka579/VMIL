# v0.2 配置验收：2026-10-16

本轮完成源码回归和官方API的独立副本候选往返验证，测试副本按用户要求已删除，原VM恢复运行。已有 `0.2.0-alpha.1` ZIP（来源 `9cfa28a`）不包含本轮源码修复。生产App仍禁止写入，完整CFG验收保持0/6，不能发布完整v0.2。

## 已实际执行

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| 最后哈希检查后的外部替换回归 | 修复前实际42项中41通过、1失败；事务错误地返回成功 | `artifacts/test-results/20261016-v02-config/direct-before.trx` |
| 保留实际被替换内容、报告冲突 | 修复后42/42文件与历史测试通过，0跳过 | 同目录 `files-fixed.trx` |
| 核心逻辑回归 | 106/106单元测试通过，0跳过 | 同目录 `unit.trx` |
| Release构建 | 0警告、0错误 | `WORK_LOG.md`，SDK10.0.401 |
| 源VM资源预检 | 一块无父盘的monolithicSparse磁盘，未发现外部写入文件引用；运行中采样不算关机或副本验收 | 私有 `source-resource-preflight.json` |
| 本机API重新配置 | 两次认证401后，经用户明确允许，用原生配置命令完成，退出0 | 私有认证状态和配置退出码；不记录密码 |
| 完整副本与引用 | 345文件、61,286,622,019字节；逐项源流/目标重读哈希一致，单基盘无父链/快照，磁盘和其他写入资源均在副本内 | 私有 `copy-manifest.json`、`copy-resource-postflight.json` |
| 官方API候选应用 | Workstation17.5.2/hardware19；6144→4096MiB、4→2vCPU，每插槽2核；HTTP200、原字节备份、精确只改两项 | `009-copy-apply-candidates.json`、`native-apply-check.json` |
| 候选实际启动 | 正常启动退出0，API poweredOn；Windows CIM为2逻辑处理器、4,293,939,200字节可见物理内存 | `candidate-hardware.json` |
| 官方API恢复 | 正常软关机/API poweredOff后恢复6144MiB/4vCPU；HTTP200，保留启动后其他最新字节 | `012-copy-restore-candidates.json`、`native-restore-check.json` |
| 恢复实际启动 | 正常启动退出0；Windows CIM为4逻辑处理器、6,441,422,848字节可见物理内存 | `restored-hardware.json`、`guest-hardware-roundtrip-check.json` |
| 清理 | 副本正常软关机，原生deleteVM退出0，余下两份备份归档核对后删除；副本目录不存在，源VM仍运行；临时进程/8697监听均0 | `copy-cleanup-check.json`、`session-cleanup-check.json` |

早期混用构建开关的外层测试命令返回退出5/零测试，不算通过。直接执行已构建测试程序集取得上述红/绿结果；随后使用先构建、再无构建运行的标准入口实际42/42通过，`cli-entry-fixed.trx`：

```powershell
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename cli-entry-fixed.trx --results-directory artifacts/test-results/20261016-v02-config
```

首次修复后的 `files-after.trx` 有2项失败：旧历史测试假定只保留一个备份。修正断言为“只重试保存历史时备份集合不变”，验证不会再次应用配置；失败记录保留。

## 本轮修复的范围

文件替换现在同时保留实际被替换文件的字节，并核对它是否符合确认时的哈希。发生最后窗口外部替换时，事务报告冲突，保留原始备份、实际被替换内容和可用的临时证据；界面显示对应恢复位置，不自动回滚。

这解决静默成功及外部内容丢失，**不能保证完全阻止外部替换，也未解决与VMware启动的生命周期互斥**。发布白名单仍拒绝写入，不将该修复单独当作CFG-03/05通过。

## 真实验收

用户已明确允许本轮私有方案中的完整副本、正常软关机/启动、内存6144↔4096MiB和vCPU4↔2候选及恢复；保持每插槽2核心。该授权不包含快照、强制断电、锁文件删除、系统安装或远程发布。

已执行上述授权：原VM20:33正常软关机，取得API poweredOff；复制时持有关键源文件的读保护句柄，拒绝写入/删除，保留源锁条目并跳过其复制。复制/引用核实后，20:37原VM正常恢复运行。副本经原生注册、参数应用和恢复，两次实际Windows查询完成闭环。副本正常生成了不同的BIOS UUID和MAC；没有手工编辑身份字段。

用户随后明确要求完成后删除副本。20:50原生删除及限定目录清理完成，约57.1GiB测试副本已释放；保留目录外的日志、哈希和几KB的VMX备份证据，不保留虚拟磁盘。原VM仍为6144MiB/4vCPU/hardware19，未作为配置或磁盘试验目标。本轮没有快照、强制断电或锁文件删除。

源及副本的正常运行会由VMware/Windows自行写入运行数据，不能将“未用于配置试验”解释为磁盘从未产生正常运行写入。复制前的字节一致性证据和运行后的参数核对分别保留。

客户机非持久会话12552/13384及API会话16404/16500均stopped并退出，本轮临时API5316已停止，8697无监听。原生账号配置窗口已关闭，新专用账号配置按用户授权保留；密码不进入代码、聊天、日志或队列。一次恢复启动早期IP查询返回Tools未运行，稍后的直接客户机CIM/文件取回已实际成功，不把早期失败重标通过。

目标版本随附Swagger和[官方更新接口](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/put/)定义内存/处理器更新；本轮仅证实17.5.2/hardware19上4096、6144MiB及2、4vCPU的这条路径，不能外推其他数值、拓扑或版本。409响应定义不证明全部并发保护。

## 尚未通过的产品验收

本轮原生API测试工具不随App分发，没有使用FixturePolicy批准真实VM，也没有开放发布白名单。完整CFG以生产App的实际应用/恢复路径为对象。

| 编号 | 现有证据 | 尚缺内容 |
| --- | --- | --- |
| CFG-01 | 官方API字段、当前目标版本及有限候选往返 | 与生产更新路径绑定的已验证白名单，不能直接批准文件替换 |
| CFG-02 | 中英审阅/预览ID及失效机制已测；本轮真实差异获具体授权 | App提交仍禁用，生产实际应用确认未验证 |
| CFG-03 | 合成四态/占用/过期证据及本轮真实API on/off | 与启动、外部替换的可靠保护；仅两次状态查询仍不足 |
| CFG-04 | 临时文件事务和本轮原生工具原字节备份通过 | 生产App真实应用/恢复及历史入口 |
| CFG-05 | 故障文件测试通过；新增最后窗口替换保全 | 当前方案仍会发生命名空间替换，不能保证不覆盖后续修改 |
| CFG-06 | 完整独立副本、官方API修改/恢复和两次客户机实值通过 | 生产App路径的同一闭环，原生工具成功不替代 |

下一步是产品更新后端与保护实现，不再是缺少本轮副本授权。优先评估接入已证实的官方API方法，补充实际状态冲突/并发/外部修改验证，再决定有限白名单和新包。测试副本已经删除，不自动重新复制；远程CI、私密安全渠道及发布仍未完成。

敏感VM路径、资源清单、认证状态、操作方案和后续原始结果仅保存在 `local-data/test-results/v02-config-acceptance-20261016/`。
