# v0.1 剩余验证结果

日期：2026-10-11，Asia/Taipei（UTC+08:00）。本轮按用户要求人机协作验证；账号和密码由用户在本机窗口输入，代码、界面操作、结果核对和记录由测试工具执行。v0.2配置写入未启动。

## 结果与范围

| 证据编号 | 验证 | 实际结果 | 验收归属 |
| --- | --- | --- | --- |
| RDY-01 | Workstation17.5.2只读支持分类 | 真实virtualHW.version=19显示已确认版本依据；合成21显示不支持；未评审参数保持未核实，重复项保持歧义 | VMX-03 |
| RDY-02 | 官方本机API电源状态 | 真实运行、挂起、完全关机均正确；API停止后显示未知，导出来源不冒充成功 | VMX-04 |
| RDY-03 | 无开发SDK/系统.NET目标 | Win10教育版x64 Build18363普通权限运行新完整包；CLI/GUI14/14、JSON/HTML及报告导入成功，四个.NET运行时模块均来自包内 | V01-06、REL-06 |
| RDY-04 | 自动化与打包 | Release构建0警告/0错误、81/81单元、10/10文件/流程；完整许可验证包传输后哈希一致 | V01-01、REL-02 |
| RDY-05 | 新状态报告与收尾 | 四份新状态JSON的值、来源、界面一致且脱敏；测试程序/认证会话/API均结束，VM恢复运行，Tools为running | VMX-05、V01-05 |

本轮新增VMX-03、VMX-04、V01-06、REL-02、REL-06；累计29/37，其中v0.1为25/25、v0.2为0/6、发布为4/6。GitHub Actions远程运行与私密安全报告渠道仍未完成，不能称正式发布就绪。

## 官方配置与接口依据

具体来源、版本范围和尚未批准的参数见[17.5.2依据表](VMWARE-17.5.2-EVIDENCE.md)。[Broadcom硬件版本表](https://knowledge.broadcom.com/external/article/315655/virtual-machine-hardware-versions.html)与[Get Power State](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/power/get/)用于本轮只读实现；硬件版本分类不构成可写白名单。

本机Workstation/vmrun为17.5.2 build-23775571；vmrest帮助为1.3.1 build-23775571。在线API标题版本与本机二进制版本分列；本地swagger的info版本占位没有作为数值版本使用。仅发送清单和电源状态GET，先核实选定VMX对应唯一注册ID。

## 真实电源序列

用户明确确认已保存客户机工作，授权原目标软挂起、恢复、软关机和重新启动；失败即停止、不强制断电。先正常关闭客户机测试App并结束临时认证会话。

| 阶段 | 本机时间/证据 | 软件状态 | 导出stateSource |
| --- | --- | --- | --- |
| 初始运行 | 14:03界面与JSON | poweredOn | vmrest.power |
| 软挂起完成 | 命令退出0；14:14界面、14:15导出 | suspended | vmrest.power |
| 恢复 | 14:15:42–46，start退出0；列表与Tools核对 | 目标恢复运行 | 不用列表推断关机 |
| 软关机完成 | 14:16:13–20，stop soft退出0；14:17界面与导出 | poweredOff | vmrest.power |
| 重新启动 | 14:17，start退出0；最终唯一目标运行且Tools为running | 原目标恢复运行 | 独立vmrun核对 |
| 临时API停止 | 14:19停止本轮进程；14:20界面与导出 | unknown | unavailable |

状态是查询时刻的快照，不持续刷新。报告collectedAt是系统报告采集时间，不能当作后来API查询的时间；本轮查询时间以私有操作日志为准。App不会自行改变VM电源。暂停、无效响应、注册歧义、HTTP失败和调用者取消另有模拟测试，不冒称全部做过真实故障注入。

## 干净目标的已确认口径

用户选择按“无需开发SDK或系统.NET”验收现有客户机。该环境不是全新OS：已有10条VC++安装记录及其他软件，未卸载或改动它们；没有声称覆盖所有Windows版本或所有本机文件位置。

- 盘点范围：ProgramFiles/x86及用户标准.NET目录、PATH、x64/x86.NET安装登记和相关卸载元数据；未发现开发SDK、Visual Studio或共享.NET Core。
- 新包普通权限实际运行；hostfxr.dll、hostpolicy.dll、coreclr.dll、clrjit.dll均加载自应用目录。所见ucrtbase.dll、msvcp_win.dll来自Windows系统目录。
- CLI/GUI五类14项全部成功且值一致；JSON/HTML保存与实际报告导入14行通过。没有为测试安装客户机SDK、.NET、Python或额外运行库。
- 私有dependency-validation-check.json于13:56:36通过，仍诚实记录freshOs=false、vcRedistributablesAlreadyPresent=true。用户确认的验收范围不会将这两项改写为全新系统证据。

Windows10教育版18363是本次项目实测环境；上游官方支持范围另见[兼容性矩阵](COMPATIBILITY.md)，不由本次成功外推。

## 构建、测试与验证包

运行目录为当前worktree，实际SDK10.0.401、Runtime10.0.12，复用既有NuGet缓存。以下用dotnet代指实际项目内SDK，省略宿主绝对路径：

```powershell
dotnet build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename readiness-unit.trx --results-directory artifacts/test-results/20261011-readiness
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-restore --parallel none --report-xunit-trx --report-xunit-trx-filename readiness-files.trx --results-directory artifacts/test-results/20261011-readiness
dotnet publish src/VMIdentityLab.App/VMIdentityLab.App.csproj -c Release -r win-x64 --self-contained true --no-restore -m:1 --disable-build-servers -p:DebugType=None -p:DebugSymbols=false -o artifacts/publish/readiness-20261011
```

81项包含新增32项版本/状态/歧义/错误/取消/来源测试；10项文件与采集流程全部通过，零失败/零跳过。没有为文档更新重复运行这些检查。

验证包：artifacts/test-packages/VMIdentityLab-validation-20261011.zip，65368827字节、411条目；源文件逻辑字节146949453，另外包含VALIDATION-BUILD.json。SHA256：674BEA31BD97DB00C1F2D19B55AEA32A2884F47E5E191E20EBFB0C67A32AE12B；客户机复核一致。原Alpha ZIP未覆盖，新包明确标注基线015c387加本轮修改的验证快照，包含已核实的完整许可，未公开发布。

14:34:51的package-review-check.json再次核对压缩包实际内容：411条目、410个清单文件哈希一致、5份必要第三方文件，未混入VM文件、PDB、测试日志或VMware/Python二进制。该复核不扩大到其他旧包或未来Release。

## 失败记录与清理

官方vmrest账号配置窗口最初退出过快、结果未看清；保留结果的原生包装窗口后来显示退出0，实际认证查询成功。预期位置未发现认证文件，其实际存储路径尚未核实；没有读取认证文件内容，也没有把未知路径当作配置失败。

14:23:07的power-validation-check.json核对四份新状态导出与控件树，通过；范围不包含整仓库或远程CI。14:23:52的cleanup-check.json确认本轮进程剩余0、8697监听剩余0、认证会话stopped。最终MCP核对唯一原目标仍运行且Tools为running。宿主Defender1116/1117自本轮13:03:27查询无匹配事件，历史高威胁禁令仍生效。

原始报告、控件树、命令结果、包和检查文件保留于私有local-data/test-results/readiness/20261011-130327/与artifacts/，不提交真实VM数据或凭据。本轮没有克隆、VMX写入、快照、系统级依赖安装、远程推送或Release发布。
