# M13：v0.3.0-alpha.1 本地发布准备

2026-10-23后续：[复审修复](V03-M13-CLOSURE.md)、[真实CI/宿主](V03-M13-CI-RESULTS.md)、[最终原包客户机](V03-M13-CI-GUEST-ACCEPTANCE.md)及[公开预发布/服务器回执](GITHUB-RELEASE-v0.3.0-alpha.1.md)均已通过，M13完整5/5。本页3c28a74候选保留此前本地历史，不作为当前CI资产；附加worktree已保全后释放，原证据从主目录同相对路径读取。本轮工具无VM配置/电源操作。

## 固定交付范围

- M09：默认14项/显式17项、Windows观测/VMX声明/身份解释，公开报告保持1.0和脱敏。
- M10：MAC唯一性/UUID复制生命周期的未执行计划，独立vmil.identity-plan/1；不生成标识、不作为写入批准。
- M11：复用官方API事务、原字节备份、持久intent、未决防重放、历史重开与单键恢复。身份有限准入只覆盖Workstation17.5.2.23775571、vmrest1.3.1.23775571、hardware19、两个有效已有标识、唯一静态hostonly网卡且启动断开，一次一个已有MAC或BIOS UUID；[政策](VMWARE-CONFIG-POLICY.md)为权威细则。
- M12：明确选择的私有MAC/UUID快照、可选接口GUID、前/后/恢复后三阶段记录配对；配置、Windows和VMIL分类分层，结论JSON/HTML脱敏；接口/会话不一致和旧脱敏报告不可比较。
- M07数值策略不变：原精确构建/hardware19/每插槽2核的4096↔6144MiB、2↔4vCPU。其他构建/拓扑/联网/自动地址/缺失键/生成复制/多字段恢复不准入；不移除其他VMware证据，不保证物理机分类。

版本0.3.0-alpha.1由Directory.Build.props统一定义，为有限范围预发布候选。M11/M12旧模块包实机结果保持原提交和字节，不能赋予新补丁包或最终CI产物；[原M12验收](V03-M12-REAL-ACCEPTANCE.md)保留失败及清理证据。

## 本轮变更与维护

四源码项目/.NET10/WPF/MVVM保持不变。System.Management及传递System.CodeDom更新10.0.13，自包含发布固定NETCore/WindowsDesktop10.0.13；没有系统安装或工具链复制。本地既有SDK10.0.401只用于本地候选验证，未来CI按global.json同特性带latestPatch选稳定维护补丁，记录实际版本；不将旧SDK说成最新。

双语README/使用指南/包说明、安全范围、报告schema、兼容性、第三方声明、变更和就绪表同步。MIT/Daniel Waltz与已确认的私密渠道保持；不重复第三方声明正文。保留原生dotnet/既有Python打包流程，新增ZIP验证/合成测试，CI增加源版本/文件清单检查；没有新增发布触发器或修改旧v0.2发布脚本。

2026-10-21只读复核官方维护依据：

| 工具/组件 | 依据与决定 |
| --- | --- |
| .NET10 LTS | [官方政策](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)：维护至2028-11-14，当前补丁10.0.13；沿用长期分支并更新候选包补丁 |
| PowerShell7.6 LTS | [官方政策](https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle)：至2028-11-14，当前7.6.6；复用已准备版本，不新采用7.4/7.5 |
| Python3.14 | [官方状态](https://devguide.python.org/versions/)：bugfix，计划2030-10结束；继续既有3.14.8，不因3.15存在而更换测试工具链 |
| xUnit4.0.1/MTP2.4 | [官方发布](https://xunit.net/releases/v3/4.0.1)：2026-09-12维护发布，无统一EOS承诺；保持固定测试依赖 |
| 官方Actions | [checkout7.0.1](https://github.com/actions/checkout/releases/tag/v7.0.1)、[setup-dotnet6.0.0](https://github.com/actions/setup-dotnet/releases/tag/v6.0.0)、[upload-artifact7.0.2](https://github.com/actions/upload-artifact/releases/tag/v7.0.2)官方Release标签与既有SHA前缀相符；沿用SHA固定和windows-2025，不把静态核对写成新CI通过 |
| OS/VMware | 宿主无项目版本门槛、客户机Win10/11 x64、VMware最低17.5.2为既有用户例外；与上游支持分列，不扩展到其他SDK |

## 本地验证结果

| 验证 | 实际结果与证据 |
| --- | --- |
| Release构建与维护补丁 | e75e5ef：零警告/错误，13.92秒；官方NuGet还原与漏洞审计成功，六项目无已报告漏洞，不保证没有未知漏洞 |
| 单元/模拟文件集成 | 237/237、200/200，0失败/跳过，TRX位于`artifacts/test-results/m13-20261021/`；后续源码只改两资源文字，未改被测逻辑 |
| 打包契约 | 14/14离线测试，覆盖篡改、清单/许可、重复/越界项、来源/版本/运行时及已有输出保护 |
| App合成工具自检 | 正常有限策略15项、两字段三阶段夹具及双语资源65项通过；均无VMware连接 |
| 最终本地候选构建 | 从干净`3c28a74bd30bb7072abe4b5b8df446766fed74bf`原生publish；实际SDK10.0.401、两运行时10.0.13；EXE/四DLL的版本及完整提交一致 |
| 原ZIP结构/完整性 | CRC、413文件、PACKAGE-FILES及外部SHA256逐项通过；解压147352240字节。实际Management13，CodeDom13与官方WindowsDesktop Runtime13 DLL字节一致 |
| 原包普通用户CLI与PNG | Win11家庭中文版Build26100/X64，默认14/显式17项、1.0/0.3版号、脱敏、HTML、拒绝覆盖、双语身份PNG及像素校验14/14；实际查看两语言PNG无离屏问题，侧栏及页脚均为v0.3 |
| 原包正常GUI | 中英文五页、空输入、MAC/UUID各三阶段合成快照及配对记录、23行检查/3行证据、JSON/HTML脱敏、版本和正常退出35/35；普通权限、App退出0、自有测试App剩余0 |
| 实际依赖许可 | 7项正文/MIT元数据一致；官方CRLF与仓库LF使5项原字节不同，仅规范化换行后正文相同，不重复共享声明 |
| 隐私与内容 | 包字节/UTF16及源码审阅范围内的个人路径/令牌标记无命中；无VM数据/PDB/测试驱动/测试平台/遥测组件；GUI不显示合成原标识，四份验证导出不含原值/会话/私有路径 |
| CI准备 | 3个literal PowerShell步骤Parser检查通过，原官方Actions SHA保持；没有完整YAML运行引擎或新远程CI结果，不能标为云端通过 |

最终本地三资产位于`artifacts/releases/local-v03-3c28a74/`：

- `VMIdentityLab-0.3.0-alpha.1-win-x64.zip`：**65516669字节**，SHA256 **`942fb17cbf1318e5d3f8ebb0f8858523379ad3ed9c26d4da755a385ad9784af3`**。
- 同名`.zip.sha256`和`VMIdentityLab-0.3.0-alpha.1-win-x64.files.sha256`；413条逐文件摘要。此ZIP是local-candidate，不是CI原包或GitHub Release资产。
- 包内BUILD-INFO标记干净来源3c28a74、Release/win-x64/self-contained、SDK10.0.401与Runtime10.0.13。后续就绪说明/日志提交不改变应用源码或重写该ZIP。

私有证据`local-data/m13-20261021/`：`final-package-check.json`、`license-check.json`、`nuget-vulnerability-audit.json`、`host-smoke-final/`、`final-gui-ready/gui-check.json`及操作记录。源码审阅材料另置`artifacts/source-review/v03-final/`，含逐文件SHA256、敏感标记范围及最终来源回执；不包含内部AGENTS/WORK_LOG/tasks、历史或私有数据，不构成上传授权。这里只准备公开历史的文件覆盖层，未对未来远程HEAD假定快进。

## 发现并处理的问题

- 第一个e75e5ef包的实际PNG显示侧栏仍为v0.2 alpha，页脚正确。只修正两语言State.ReadOnly资源为v0.3，不改变历史V02ExactRange依据；原包/图保留，新3c28a74包完成上述复验。
- 可选Prepare-Alpha.ps1文件入口被独立PowerShell7.6.6执行策略拒绝，正文未执行；只读核对同期Defender事件0，Security模块亦发生AuthorizationManager加载错误。未降低策略、解除封锁或换入口执行该文件；撤回非必需包装脚本，继续已存在的独立原生CLI/元数据/打包流程。
- 私有检查首轮错误要求CodeDom单列deps库，实际由WindowsDesktop运行时统一提供；以实际DLL版本/官方字节确认，旧断言失败保留，不改产品依赖或压低版本。
- GUI驱动首轮早于窗口创建，未进入验收；保留`final-gui/`失败及自有App清理结果。新驱动等待首个输入空闲后在`final-gui-ready/`完整35项通过，不把失败首轮改成通过。

首次沙箱NuGet还原NU1301/NU1900，网络套接字权限拒绝；依项目规则先只读核对同期Defender1116/1117为0，未重跑受限脚本。经正常网络审批后官方NuGet还原完成，未关闭审计/防护或安装系统组件。该环境失败与后续实际验证分开记录。

## 未执行及后续顺序

1. 根据[待上传审阅方案](V03-RELEASE-PLAN.md)复核最新远程基线、文件范围及隐私；获得具体推送授权后，使用公开历史的新快进提交上传审阅源码并运行真实CI，不推内部worktree历史。
2. 下载并固定最终CI原始ZIP、run/commit/artifact与服务器摘要，校验版本/文件及许可；在声明目标以普通权限验收这个确切包。现有客户机/全新OS/其他版本分别列明，真实VM操作另行确认。
3. 填入最终原包哈希和准确验收范围，完成具体Release授权，再发布v0.3.0-alpha.1预发布并回读tag/资产。原v0.2tag/资产不移动或覆盖。

## English

Local M13 preparation is complete; full delivery remains2/5. The clean3c28a74 candidate is0.3.0-alpha.1 with SDK10.0.401 and self-contained runtime10.0.13. Its ZIP is65516669bytes,413files, SHA256942fb17cbf1318e5d3f8ebb0f8858523379ad3ed9c26d4da755a385ad9784af3. Release/237unit/200simulated integration/14package contract tests passed, as did15policy/65fixture-resource checks. The exact extracted candidate passed14ordinary-user CLI/PNG and35bilingual normal-App synthetic validation checks on Windows11 build26100. License content, hashes and scoped privacy review passed. The stale v0.2 sidebar was fixed and its first package retained; a startup race in the GUI test driver was resolved, with the initial failure preserved. The optional PowerShell wrapper was withdrawn after execution-policy rejection; protection/policy was not weakened. No VMs, remote writes, tags or releases were operated. Earlier real-guest module evidence remains separate; new CI, final-CI-byte target acceptance and publication still require their own authorization. See the fixed scope above and the English user guide for precise restrictions.
