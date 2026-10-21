# 使用指南 — v0.3.0-alpha.1 本地候选

当前源码版本0.3.0-alpha.1尚未上传或发布。已公开的[v0.2.0-alpha.1](https://github.com/Aleka579/VMIL/releases/tag/v0.2.0-alpha.1)保持原内容。包身份见BUILD-INFO.json，[发布就绪表](../RELEASE-READINESS.md)区分本地、CI和目标环境验证。[English guide](USER-GUIDE.en.md)。

## 启动和检测

完整解压Windows x64自包含包并运行VMIdentityLab.App.exe，保留运行时文件；普通权限，无需开发SDK或系统.NET。侧栏切换概览、检测、配置、对比和报告五页，左下角切换中英文。

点击“开始检测”；可取消，失败/拒绝访问/超时分别记录。默认14项不读取原始标识；显式选择UUID、MAC、BIOS序列号后为17项，诊断JSON仍脱敏。身份分析分列Windows观测、VMX声明及证据解释；未核实旧来源不重标为新接口。

```powershell
.\VMIdentityLab.App.exe --collect --output .\report.json --html .\report.html
.\VMIdentityLab.App.exe --collect --include-identifiers --output .\identity.json
```

CLI拒绝已有输出，GUI普通诊断导出可确认覆盖。导入两份诊断JSON进行比较，脱敏标记不解释为原值相同；VMX与本机系统不会自动关联。格式见[报告约定](REPORT-SCHEMA.md)。

## 配置查看、应用和恢复

选择VMX只读取声明、保留未知项，不执行其中内容。可连接预先配置且仅回环监听的官方vmrest查询状态；API专用账号不同于Windows/客户机账号，凭据仅本次请求使用。App不创建账号、启动API或控制电源；未知/运行/挂起不能写入，状态观测不代表持续监控。

数值写入仅批准Workstation17.5.2.23775571、hardware19、每插槽2核的4096↔6144MiB及vCPU2↔4。身份写入批准同一精确构建/hardware19、已有有效MAC和BIOS UUID及唯一静态hostonly网卡且启动断开；一次一个已有字段，格式及恢复范围见[配置政策](VMWARE-CONFIG-POLICY.md)。最低版本声明不开放其他构建。

选择字段和拟用值，生成预览并核对目标、差异、影响，再勾选确认。点击应用，在本机“连接本机VMware API”窗口输入专用账号；后端重新核实关机、版本、路径、占用及原内容。备份原字节、验证预期内容、持久化intent后，经官方API更新和复核，不直接替换VMX。编辑输入、外部内容/目标变化使旧审阅失效。

“备份与恢复历史”刷新后选择实际记录，可在退出重开后生成恢复预览。恢复同样确认和检查，只反向修改本次参数、保留其他内容；字段/拓扑不兼容后续变化会拒绝恢复。备份不能恢复磁盘、快照、NVRAM、客户机或整机状态。结果不确定时保留pending/备份/预期文件，先核实服务端结果；不能删除记录重试，当前不自动核销未决操作。App租约不是VMware全局锁。

其他构建、多网卡/联网、自动地址、缺失键、生成/复制身份及多字段恢复保持禁用。实验计划折叠区仅预览意图、风险和缺口，vmil.identity-plan/1固定未执行；计划审阅不能替代应用确认，也不生成新标识。

## 三阶段身份验证

1. 在“对比 → 身份验证”选择MAC或BIOS UUID和会话UUID。在目标Windows系统分别采集变更前、变更后、恢复后三份私有快照，不在宿主机替客户机采集。
2. 确认私有数据提示并选择新JSON路径。需要固定网卡时输入同一接口GUID；缺失接口报告失败，不回退其他网卡，默认多地址仍不可比较。
3. 导入三份同会话/同范围快照及配对的应用/恢复历史记录，分别查看配置、Windows观测、VMIL分类与恢复结果。旧脱敏报告、缺失记录、范围不同或失败观测不能证明原值相等。
4. 选择新路径导出脱敏JSON/HTML；原始快照/记录/路径不嵌入。重新导入错误文件会清除旧结论，不能保留此前成功。

私有CLI等效入口：

```powershell
.\VMIdentityLab.App.exe --capture-private-validation --session SESSION-GUID --phase before --output .\VMIL-PRIVATE-before.json
.\VMIdentityLab.App.exe --capture-private-validation --session SESSION-GUID --phase after --output .\VMIL-PRIVATE-after.json --adapter INTERFACE-GUID
```

用有效UUID替换占位符；阶段为before/after/restored，三阶段保持同一接口范围。私有快照确实保存原始MAC/UUID，历史记录含私有路径；放私有目录，不提交Git/公开Issue或公开附件。默认诊断不创建这类快照，不承诺托管内存安全擦除。

## 支持与安全

项目允许Windows10/11 x64，按具体版本/Build验证，宿主OS无项目版本门槛。官方支持与项目实测分列。[M11](V03-M11-APP-ACCEPTANCE.md)/[M12](V03-M12-REAL-ACCEPTANCE.md)原模块包曾在Win10教育版18363独立副本完成有限闭环；本候选、最终CI包、全新OS和联网分别验收，不能继承原包结果。[兼容性矩阵](COMPATIBILITY.md)列明边界。

无遥测、不自动上传。默认诊断与验证导出脱敏；私有快照/恢复记录由用户私下保管。MAC/UUID改变后其他虚拟化证据仍在，不保证第三方软件识别为物理机；不得用于绕过反作弊、反欺诈、设备封禁、授权或安全保护。安全问题使用[私密渠道](https://github.com/Aleka579/VMIL/security/advisories/new)。Defender拦截时停止操作并保留告警，不添加排除或降低防护。
