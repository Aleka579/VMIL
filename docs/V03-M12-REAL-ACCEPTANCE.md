# M12 真实验收

2026-10-21，Asia/Taipei，当前worktree `codex/v03-m10-20261019`。本轮获用户具体确认后执行，有限范围验收**5/5及清理完成**，不代表v0.3发布完成。原离线交付见[V03-M12-RESULTS](V03-M12-RESULTS.md)，实际任务状态见[M12](../tasks/modules/M12-identity-validation.md)。

## 范围与证据

具体原VM、新完整副本、最多四次身份PUT、正常电源/恢复和最终归档/清理授权保存在忽略的`local-data/m12-real-20261021-1445/REAL-VM-PLAN.md`。实际源码、包、原始失败及每步结果同目录保留，真实标识不进入本文件或Git。原VM最初由独立API确认挂起，已正常恢复/软关机以取得冷副本，结束需恢复挂起；不修改原VM配置、快照或安全设置。

## 实际发现与调整

首个bc40ccd包校验及原生私有CLI成功。副本存在Ethernet0和蓝牙PAN两个.NET网络接口；独立参考只取PCI适配器，因此首轮对照23/24，MAC单值相等检查失败。原文件保留，不能通过删除蓝牙接口或隐去额外MAC让结果通过。

保留默认读取全部接口及多地址不可比较规则，新增**显式网络接口范围**：

- CLI在原私有命令末尾可加`--adapter INTERFACE_GUID`；GUI私有采集区提供可选的“网络接口GUID”。留空仍采全部接口，不能自动猜测要比较的网卡。
- 接口GUID可在待测Windows通过`[System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() | Select-Object Name,Id`只读查看，由操作者明确选择。选择不存在、无有效地址或被排除类型的接口时保留不可用，绝不回退到另一张网卡。
- `networkAdapterId`仅为私有快照可选的采集范围元数据，旧快照缺省仍兼容。三阶段混合空值或不同GUID时身份对比不可比较；即使声明同一GUID，多地址仍不可比较。
- 原始MAC/UUID和接口GUID只在私有输入保留，不进入公开结果、普通报告或日志；默认诊断/标识集合不变。此选择不修改Windows网卡或VM配置，不改变M11写入准入。

本轮在修改身份前先完成上述选择、边界及真实来源对照，再使用新源码包重新采集完整六份三阶段输入；不能混用旧包首份快照或将新增单接口范围冒充默认多接口结果。

## 新包与实际验证

显式范围修正保存为本地`e0f4e10`，Release构建零警告/错误，237/237单元、200/200集成、CLI边界6/6通过，0失败/跳过。普通App私有表单7/7；未采集宿主机私有标识。首个英文检查器期望单词顺序错误的失败保留，只纠正检查器，使用原UI证据。实际日志为本轮`interface-fix-build.log`、`interface-fix-unit.log`、`interface-fix-integration.log`及`cli-check.json`；TRX在`artifacts/test-results/m12-real-20261021/`。

从该提交自包含发布的新验证包为`m12-guest-e0f4e10.zip`，65534102字节，SHA256 `2d46d0585141b403f6166114e3530c320dd9b4b276d54be9b55b57ec0da6a316`；SDK10.0.401/Runtime10.0.12，内置源码及五组件哈希，与客户机取回值一致。使用新的客户机目录、两个新会话和六份新快照，不覆盖或混用首轮bc40ccd结果。这是本轮验证包，不是CI包或正式Release。

| 实际项目 | 结果 | 私有证据入口 |
| --- | --- | --- |
| 冷副本资源独立性及逐文件SHA256 | 625文件/61288061658字节，单磁盘、无父盘/外部引用 | copy-state.json、copy-manifest.json、copy-resources.json |
| 正常客户机采集及独立CIM/API参照 | MAC与UUID各before/after/restored，六份各25/25，总150/150；普通权限、14项成功、4 vCPU及内存保留 | guest-fixed-*/verification.json、control-results/ |
| 默认多接口与指定缺失接口 | 9/9；默认保留两个MAC，缺失GUID不回退，保留NotSupported/Unknown | native-scope-check.json |
| 正常App确认与官方API往返 | 成功PUT恰四次：MAC应用/恢复、UUID应用/恢复；四份预览各8/8、四份收据各9/9，独立API两标识均回基线，无pending | *-confirmation-check.json、*-receipt-check.json、history/ |
| 退出后重开历史 | MAC恢复前重开读取一条、UUID恢复前重开读取三条，使用原apply记录逆向恢复 | app-mac-reopened-history.json、app-uuid-reopened-history.json |
| 宿主普通App真实三阶段界面/脱敏导出 | MAC及UUID各17/17，总34/34；中英文23项检查/3条程序观测、JSON及HTML | real-*-public-check.json、ui-real-*-complete-*.json |
| Windows 10客户机普通App真实界面 | 教育版Build18363/X64，MAC及UUID各17/17，总34/34；已有文件不覆盖及App退出通过，原命名失败保留 | guest-real-ui-fixed/、guest-real-ui/、guest-session-02/results/ |
| 原VM结束状态、最终归档及副本清理 | 独立API挂起、关键配置保留；637份/3214602字节最终元数据哈希归档，唯一副本已删除、释放61288090890字节，临时会话/自有API进程结束 | library-final-check.json、final-archive-receipt.json、source-final-check.json、copy-directory-removal.json、owned-process-cleanup.json |

客户机真实捕获结果始终保留VMware一致证据与Hypervisor信号。两标识的配置/Windows变更和恢复通过，不使环境成为物理机，也没有测试第三方程序。网卡按已准入策略保持断开，独立CIM状态7；联网检查明确`notProvided/notTested`，不将Tools正常或能启动等同网络连通。

## 失败保留与边界

首次启动等待厂商“我已复制”提示超时，独立核实真实启动后继续，未重复start；一次客户机认证失败换用新的原生窗口，密码没有保存。首轮蓝牙PAN范围23/24失败、英文检查器期望错误均保留。正常API会重排VMX字节，各次备份/实际哈希匹配、提议语义相等，不能把恢复原值说成恢复整个VMX字节或整机内存会话。

客户机GUI首轮由验收助手把历史改名为`mac-apply.json`等，App正确拒绝不匹配记录ID的文件名、保持导出禁用；没有改产品规则。保留该UI及失败结果，审查并隔离10条从未执行的旧命令后显式恢复；新目录保留原GUID文件名，重新验收，不重放任何身份PUT。

本轮没有扩大M11准入，未推送、合并、发布或开始M13。其他VMware版本/硬件、全新OS、联网、新远程CI及最终v0.3包未验证；原始开发程序集仍为0.2.0-alpha.1。

## 实测后文案修正

实测发现API连接窗口旧说明限定“内存/CPU”，与已逐项审阅的身份操作不符。仅修改两个语言资源，改为“本次已确认配置 / confirmed configuration”，后端、策略和凭据流程不变。随后单并发Release构建零警告/错误，9.07秒，`connection-wording-build.log`及两份资源已实际核对；没有为文案变更重放身份PUT，六份客户机快照和实际包证据仍明确属于e0f4e10。最终v0.3包/CI验收属于未开始的M13。

## 最终状态与清理

用户手动处理原VM的sata0:1提示后，Tools正常，原VM经正常soft suspend，046及GUI同步后的049独立API均确认`suspended`；归档另核对11项关键配置与冷副本前原件相等。实际只有`checkpoint.vmstate`和`softpoweroff`状态字段变化，光驱提示未由代理代选；不声称完整VMX字节或原RAM会话恢复。

首次移库后界面树已无副本，但持久清单/API仍有引用。正常关闭重开GUI后，针对重新出现的本轮条目核实“不会从磁盘删除”的确认再移除；等待其写盘后，API/库树/持久清单匹配均0，原VM仍保留，旧探针不操作。停止客户机/控制凭据会话及本轮vmrest，仅保留用户的VMware界面。

最后GUI动作后归档637份元数据、3214602字节，逐文件SHA256及源原件二次读取一致；唯一大磁盘不作为冗余证据复制。随后原生PowerShell验证确切绝对目录、无重解析点/运行锁、文件集及全部元数据哈希不变、VMX和磁盘可独占读取，删除唯一新副本，实际释放61288090890字节（约57.1GiB）。外部history/六快照/原失败/最终归档仍私有保留；不删除其他用户数据或旧探针。本轮具体授权已执行结束，不能重放旧队列/四次PUT、归档删除脚本或重建该旧副本。
