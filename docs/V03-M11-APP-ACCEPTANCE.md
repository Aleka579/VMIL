# M11 真实 App 验收与有限准入结果

2026-10-20，Asia/Taipei。起点d15c427，用户明确批准本轮完整副本、电源、App身份往返和清理方案。**M11在下述有限范围完成5/5验收：两字段各完成真实App应用、退出重开历史、客户机有效值核对、App恢复及客户机原值核对。** 五阶段客户机75/75、四份事务收据36/36通过，成功身份PUT恰好4次，无未决操作。原VM已恢复最初可靠挂起状态，唯一测试副本已删除。

正常App策略现仅登记Workstation `17.5.2.23775571`、hardware19、已有VMware格式静态MAC和BIOS UUID、单个static/hostonly且启动断开的网卡；一次修改一个已有字段。其他版本、自动地址、多网卡、缺失键、厂商生成/复制生命周期仍未准入。M12/M13未开始，未推送或发布，程序集版本仍0.2.0-alpha.1。

## 已完成的准备

- `tools/acceptance/VMIdentityLab.Acceptance`为独立开发测试工具，不属于解决方案/正式包；引用实际App MainWindow/MainViewModel、确认/凭据窗口及既有事务/历史工作流，内部注入一个确切VMX目标和本轮私有历史的候选策略。
- 验收准备时正常入口保持拒绝；真实证据齐全后才在ReleasedVmxApiWritePolicy登记上述范围，并完成正常入口的合成检查。无命令行、环境或导入策略放行入口；专用宿主的确切目标限制及所有版本/输入/状态/备份/未决保护保留。
- 语言字典改用明确的引用程序集pack URI，支持正常入口与测试宿主；验收宿主使用自己的Application类型和实际主题/语言资源，不把App.xaml的另一x:Class强行载入派生类型。首次两个启动失败已捕获并保留日志，无未处理异常弹窗；修正后自检及实际窗口通过。
- 在未注册的新冷副本构造静态/仅主机/启动断开网卡与唯一UUID基线，此环境准备与App字段写入/恢复分别记录；不允许把直接构造测试配置当作App官方API验收或失败回落。
- 新状态助手只支持确切源VM/副本的状态读取、两标识读取、副本注册与显式正常电源队列，不支持配置PUT或任意API路径。原始凭据只在本机窗口输入；401停止，不自动登录重试。

## 本轮已实际执行

| 检查 | 结果 |
| --- | --- |
| 解决方案及验收工具Release | 零警告/错误 |
| C#回归 | 199/199单元、192/192模拟集成，无失败/跳过；1.181秒/4.354秒 |
| 验收宿主自检 | 15/15，默认拒绝、指定目标允许、其他目标/不完整版本拒绝、中英文资源、无配置旁文件及未创建历史 |
| 完整复制工具 | 8/8，合成小磁盘的本地引用/父盘拒绝、过期电源拒绝、逐字节复制验证、原件不变和已有目标拒绝 |
| 副本基线准备 | 9/9，原始备份、无关内容和磁盘保留、隔离网卡、旧自动键移除、厂商格式MAC/UUID及重复操作拒绝 |
| 状态/电源助手 | 13/13，模拟API/凭据/原生命令；目标绑定、注册一次、失败暂停、电源拒绝、无配置PUT、不持久化凭据及401无重试 |
| 实际主机合成窗口 | 14次驱动调用全部成功；指定目标的审阅复选框启用、勾选前/后提交禁用/启用、默认隐藏、英语资源和退出；未点击提交、未连接API或请求凭据 |
| 原VM资源预检 | 只读通过，单磁盘且无父盘/外部引用/重解析点；当前挂起文件存在，不能据此冒充可靠电源状态 |
| 原VM实际状态及冷关闭 | API确认初始挂起；正常恢复/软关机/完全关机成功；结束正常启动后soft suspend成功，独立API确认suspended |
| 新完整副本 | 345文件/61,287,416,063字节逐文件SHA256一致，单磁盘无父盘及外部资源引用 |
| 副本初始化及启动 | 原始复制VMX已保留，按批准差异初始化；官方注册成功，首次启动选择“已复制”后正常运行、Tools运行；UUID由VMware正常重生成，最终基线按实际观测建立 |
| 客户机测试包 | 源码044e650，.NET10.0.12自包含App/驱动；传入后包SHA及5组件SHA一致 |
| 实际客户机基线 | 15/15核对通过；Win10教育版Build18363、普通权限、4逻辑处理器、6441422848字节内存；17条采集全部success、无diagnostics，JSON/HTML默认脱敏且原值不在导出中 |
| 客户机实际GUI | 普通用户启动/开始检测/完成/检测详情/正常退出；默认未请求标识的14条观测可见 |
| 运行中/过期预览拒绝 | 实际App返回停止；身份未变，无备份/意图/历史，仅已释放的空App租约。固定注释后的过期尝试保持整文件哈希不变，不发配置PUT |
| MAC实际应用 | 同一App确认/备份/intent/官方API/schema3历史；独立收据9/9、API/完整VMX仅MAC改变，无pending；条目顺序规范化使原字节不同，完整配置语义通过 |
| MAC历史重开 | 正常退出并重开App后，原应用记录可见；历史行仅显示时间、文件名及操作 |
| MAC客户机有效值 | 15/15，独立CIM MAC与应用后API相符；UUID/CPU/内存保持基线，普通权限17条采集成功、脱敏且仍有VMware证据 |
| MAC实际恢复 | 重开App从历史生成单键逆向预览、重新确认/备份/官方API；恢复收据9/9、独立API和VMX恢复原MAC，保留后续无关配置，无pending |
| MAC恢复后客户机 | 15/15，独立CIM确认原MAC/UUID，4CPU/内存不变，17条成功、普通权限/脱敏/VMware证据保持 |
| UUID实际应用 | 同一App确认/备份/intent/官方API/schema3历史；独立收据9/9、API/完整VMX仅uuid.bios改变，MAC保持，无pending |
| UUID历史重开 | 正常退出并重开App后，UUID应用及此前MAC的应用/恢复记录全部可见 |
| UUID客户机有效值 | 15/15，独立CIM经SMBIOS字节顺序转换后与API相符；MAC/CPU/内存及隐私/VMware证据保持 |
| UUID实际恢复 | 重开App从历史生成逆向差异并重新确认/备份/API写入；收据9/9、独立API及VMX原值恢复，保留后续无关配置，无pending |
| UUID恢复后客户机 | 15/15，两身份均回到实际基线，4CPU/内存及普通权限/脱敏/VMware证据保持；五阶段合计75/75 |
| 最终恢复和清理 | 副本soft stop及API poweredOff；原VM可靠suspended，11项关键配置与冷复制前相同；自有App/客户机/API控制/vmrest全部结束 |
| 最后GUI后归档 | 357文件/2,416,381字节逐文件核实，保留4份原始备份/提议/完成意图与外部history；不重复备份57.1GiB磁盘 |
| 移库与删除 | UI及持久inventory目标引用0；仅删除获准副本目录61,287,292,669字节，原VM及旧探针条目保留 |

客户机工具失败均保留：`cmd mkdir`返回1且目录不存在，历史已有同入口失败，本轮不应再次使用；同期宿主/客户机Defender日志明确无匹配1116/1117/1121，不据此宣称历史误报。恢复队列先于前置目录任务落盘，导致一次UUID证据输出失败；核实时序后先完成前置再恢复，最终采集通过。两项属于测试准备/队列问题，不记为产品事务成功或隐藏原失败。客户机Restricted策略未改，准备的ps1未执行；短交互cmdlet及实际EXE完成采集，`vmrun`退出码和取回产物分别核实。

MAC恢复后首次客户机采集被VMware以“用户必须交互登录”拒绝，程序未启动；独立核实副本运行/Tools正常、原VM仍关闭，隔离旧未执行任务后做不写文件的交互就绪检查成功。确认新证据目录仍不存在后，以新编号执行并通过，不覆盖原失败，不重填或持久保存密码。后续每次启动均先核实交互就绪再采集。

原VM恢复启动时出现已有sata0:1光驱提示，由用户手动处理后才正常挂起；本轮代码未选择外设行为。原VM完整字节有正常电源元数据变化，关键身份、网络类型、CPU/内存与磁盘引用相同；仅恢复电源状态，不宣称恢复冷关机前的RAM会话。

清理首次仅移除GUI内存中的条目，持久库残留需正常重开厂商GUI后移除；最终等待延迟写入并核实引用0，没有直接修改清单。首次沙箱只读清单遭PermissionError，先核对本轮Defender事件无匹配，真实用户只读访问正常，再在用户上下文归档；无关闭防护或添加排除。全部最终元数据在最后GUI动作之后归档，原失败保留。

## 正常入口准入验证

真实测试沿用源码044e650的事务实现。随后只修改正常策略的两个字段登记和双语提示，未再发送真实配置PUT：

- 单并发Release零警告/错误，199/199单元、192/192集成全部通过，无失败/跳过；持续1.228秒/4.464秒。两字段合成应用→历史重开→恢复现直接使用默认发布策略，旧文件后端仍拒绝。
- 验收宿主自检15/15，正常策略允许已验证配置，限定宿主仍拒绝其他目标，两者拒绝不完整版本；不创建配置旁文件或历史。
- 正常App入口普通权限双语检查8/8：MAC中文、UUID英文均默认隐藏，未勾确认时提交禁用、勾选后启用；返回预览后退出，合成文件未改且无旁文件。未点击提交、打开凭据窗口或连接API。
- UI工具首次未展开语言列表、误用后台“取消”而非模态“返回预览”，后续控件不可用；核实实际控件后完成，原失败动作保留，不将每次驱动调用都计为成功。

本轮没有新远程CI、全新OS或最终Release包验收。客户机ZIP仍为044e650的实际测试原包（65,503,814字节，SHA256 `169896542bec0595e1701e43c16d50fc087c5f6093ec29433c26784483196400`），不把策略登记后的本地构建冒充该原包。

命令：

```powershell
dotnet build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers --tl:off
dotnet build tools/acceptance/VMIdentityLab.Acceptance/VMIdentityLab.Acceptance.csproj -c Release --no-restore -m:1 --disable-build-servers --tl:off
VMIdentityLab.Acceptance.exe --self-test <新的私有目录>
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m11-acceptance-unit.trx --results-directory artifacts/test-results/m11-app-20261020
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m11-acceptance-integration.trx --results-directory artifacts/test-results/m11-app-20261020
```

SDK10.0.401、.NET10.0.12、原有Python3.14.8/mcp-venv及pywin32；没有系统安装、依赖升级、版本号/CI修改或远程操作。

## 证据、支持依据与边界

本轮方案、绑定清单、预生成基线差异及准备结果在`local-data/m11-app-20261020-2100/`。`REAL-VM-PLAN.md`列出新完整副本、源VM恢复/软关机、仅副本的MAC/UUID应用→客户机→App重开/恢复、原VM状态恢复和最后归档/删除副本；`baseline-plan.json`与`acceptance-manifest.json`包含具体本机数据，不提交。完整复制、基线、控制助手的测试均使用独立合成夹具，没有触碰真实VM。

私有实际结果包括五个`guest-*/verification.json`、四个`*-receipt-check.json`、`control-results/`、`history/`、`source-final-check.json`、`final-archive-receipt.json`及`copy-directory-removal.json`。准入回归为`release-admission-*.log`、`released-normal-ui-check.json`和`artifacts/test-results/m11-released-20261020/`的两个新TRX。

[17系列手册](V03-M11-MAC-UUID-EVIDENCE.md)提供产品能力和格式约束；[精确构建研究](V03-M11-REAL-API-RETEST.md)与本轮App/客户机证据提供目标构建实测。系列手册并非冻结17.5.2的逐字段API保证，本项目也未枚举全部随机值；格式范围与实际测试实例分别记录。规则见[配置政策](VMWARE-CONFIG-POLICY.md)。

网卡保持断开，只验证有效MAC/CIM和配置恢复，未验证联网、DHCP或外部网络策略；随机值不承诺跨VM唯一。MAC/UUID变化后VMware/Hypervisor等证据仍在，不构成去虚拟化。此具体方案及对象已经结束，不能重放四次写入或用其批准新VM；M12/M13按后续请求处理。
