# M12：三阶段身份验证开发与模拟

2026-10-21，Asia/Taipei。当前worktree `codex/v03-m10-20261019`，起点`68314f3`。本轮按用户要求先完成不需协助的开发、模拟和普通用户合成界面验收，**不修改、启动或连接真实虚拟机**。M11历史实测保留原范围，不当作M12新实机结果；M13未开始，程序集仍0.2.0-alpha.1，未推送或发布。

上午开发/模拟交付时M12整体**2/5**；该记录保留当时范围。下午[真实验收](V03-M12-REAL-ACCEPTANCE.md)已完成有限范围**5/5**，原VM恢复挂起、副本已删除，最新状态见[任务](../tasks/modules/M12-identity-validation.md)。

2026-10-21下午已按新授权进入[真实验收](V03-M12-REAL-ACCEPTANCE.md)，现场发现蓝牙PAN与PCI网卡的观测范围不同，补充显式接口选择；下方保留上午初始交付范围和测试记录，不能代替新包实机结果。

## 实现

| 部分 | 行为 |
| --- | --- |
| 私有快照 | 独立格式`vmil.private-identity-snapshot/1`，保存会话、阶段、采集权限、脱敏诊断及两项原始Windows标识；不保存BIOS序列号 |
| 默认隐私 | 原诊断报告1.0、默认14项与可选脱敏17项不变。普通采集和导入不会开启私有采集，私有快照不是公开诊断报告 |
| 配置层 | 只读配对既有schema3身份应用/恢复记录，核实精确版本、目标、单字段逆向、相关上下文、时间和值；不调用API或执行恢复，不把导入记录当作新鲜实机证明 |
| 客户机层 | 对照三个阶段的选定标识变化/恢复、另一标识保留、普通权限，以及14项公开观测的值、类型/单位、状态和来源；缺失/脱敏/失败/多网卡不伪装为相同 |
| 程序层 | 当前VMIL证据分类器分别处理三份观测；分类不变也是实际结果，不以物理机分数或第三方安全程序作为目标 |
| 格式兼容 | 可导入旧1.0诊断报告；公开字段仍可比较，缺少私有标识的项明确不可比较，无配置记录时为未验证 |
| 界面 | 原“报告对比”新增“三阶段身份验证”页签；私有输入和配置记录分别选择，原两份报告入口保留。中英文、后台文件操作及取消机制复用既有实现 |
| 失效 | 输入无效、阶段错配或配置记录不配对会清除旧结果并禁用导出；其他成功导入不能清除尚未修复的输入错误 |
| 公开导出 | `vmil.identity-validation/1` JSON和静态HTML，仅含状态、有限版本、阶段时间及程序分类；无原始标识、身份哈希、会话GUID或输入路径，不覆盖已有文件 |

三层分别呈现，没有把配置写入、Windows有效值或程序分类合并成一个“去虚拟化成功”结论。导入文件并不构成来源认证；会话编号用于分组，配对记录和值用于对照，不是密码学证明。联网和外部网络始终列为未验证，当前功能不操作网络。

## 使用

1. 在待测Windows中选择“三阶段身份验证”，三个阶段使用同一会话编号。展开“采集本机私有快照”，选择阶段和新文件；明确确认后才读取、保存本机MAC/UUID。它不自动连接所选VMX。
2. 客户机也可运行现有App的显式私有CLI模式：

   ```text
   VMIdentityLab.App.exe --capture-private-validation --session <同一个GUID> --phase before --output PRIVATE-before.json
   VMIdentityLab.App.exe --capture-private-validation --session <同一个GUID> --phase after --output PRIVATE-after.json
   VMIdentityLab.App.exe --capture-private-validation --session <同一个GUID> --phase restored --output PRIVATE-restored.json
   ```

3. 私有快照含标识原值，应保存到受控位置，不公开上传。项目默认私有文件名已加入忽略规则，任意命名的私有文件仍应放在`local-data/`或`private/`。导入前/后/恢复后三份文件，选择对应MAC或UUID；可选读取原App保存的应用和恢复记录，以核对配置预期值。UUID按SMBIOS字节顺序转换后与Windows标准GUID比较；客户机与宿主时钟需可对照，时序无法确认时不会自动放宽。
4. 查看分层状态，导出脱敏JSON/HTML；现有文件不会被覆盖。普通报告只用于公开观测对比，不能通过相同掩码补出标识是否相同。

本轮没有实际运行私有本机硬件采集，原生查询由模拟来源覆盖；新增分支复用已存在的Windows接口和限流，真实客户机采集仍待具体授权验收。

## 架构与维护

- Core：`Comparison/IdentityValidation.cs`、`ValidationSnapshotJson.cs`负责三阶段语义、私有输入边界及公开结果；复用`ReportComparer`、`ReportJson`、`ReportPrivacy`和原子新文件保存。
- Windows：`WindowsValidationCollector`把正常报告与最小两项读取分开；`NativeWindowsDataSource`复用原查询/门控，只在显式私有分支跳过序列号。
- VMware：`VmxIdentityValidation`仅解释已有私有收据，`LocalVmxBackupHistory.ReadEntryAsync`复用同一读取校验，原目录读取/旧schema兼容保持。
- App：`MainViewModel.Validation.cs`及原比较页面组合这些功能，不新增生产项目、依赖、驱动、服务、通用参数编辑器或远程控制；M11写入策略未改。

## 已执行验证

共享SDK10.0.401、Runtime10.0.12，单并发Release构建，无系统安装或依赖更新。

- 首次构建因HTML编码包装的nullable返回CS8603失败，处理空值后恢复；没有降低警告等级，原日志保留。
- 最终解决方案构建零警告/错误，20.17秒；现有验收工具也构建成功，2.49秒，SDK/三个运行时目录已实际核实10.0.401/10.0.12。
- 最终**233/233单元、200/200集成**通过，0失败/跳过；持续2.312秒/5.876秒。比M11新增34项单元、8项集成，覆盖变化未生效、错误恢复、其他字段变化、状态/来源/版本/会话/时序不一致、缺失/脱敏、多网卡、私有读取异常、超时/取消、schema兼容、收据配对、原始值与SMBIOS字节序及不覆盖文件。
- 两类合成夹具/真实语言资源自检**65/65**通过；三阶段配置及观测、分类不变、初始导出禁用和全部状态/原因资源均已检查，没有Native采集或VMware调用。
- 普通App合成UI **28/28**：中英MAC/UUID三阶段、23项分层检查/3条程序观测、六份脱敏结果、错误来源、错阶段与坏记录拒绝、错误不被其他成功导入抹掉、清除失效、旧报告标识不可比较/14项公开观测可比较、原两份报告14行回归及已有输出哈希不变。192次驱动调用为191成功/1错误导航名称；原失败保留，按实际“报告对比”后完成，无配置动作。App28576已正常关闭并实查退出、stderr为空。
- CLI边界**4/4**：帮助说明显式私有模式、无效会话拒绝/不生成文件、已有输出拒绝/原哈希保留、无未处理stderr。只执行help和无效请求，没有实际读取硬件。原M11合成宿主兼容自检15/15通过，不是新的真实VM验收。

```powershell
dotnet build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers --tl:off
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m12-unit-delivery.trx --results-directory artifacts/test-results/m12-20261021
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename m12-integration-delivery.trx --results-directory artifacts/test-results/m12-20261021
dotnet build tools/acceptance/VMIdentityLab.Acceptance/VMIdentityLab.Acceptance.csproj -c Release --no-restore -m:1 --disable-build-servers --tl:off
VMIdentityLab.Acceptance.exe --validation-fixture <新的私有合成目录>
```

原首轮224/200通过后，按“其他字段保留”要求覆盖全部14项，补充5项后229/200通过；核查补充“三份均缺版本不得算匹配”后230/200。最终再细分未知值、脱敏值、错误标识类型的原因，三项回归补齐，233/200通过。已有有效输入的UI证据沿用，相关新边界由最终自动化验证；未降低阈值或隐藏失败。工具/输出均不进入产品包。

私有证据为`local-data/m12-20261021/`：`delivery-build.log`、`unit-delivery.log`、`integration-delivery.log`，`synthetic-fixture/fixture-check.json`、`ui-actions.jsonl`、UIA快照、`ui-check.json`、`cli-check.json`、六份公开结果及原失败日志；TRX在上述测试目录。验证快照/记录全部使用合成夹具，不包含本轮真实硬件标识。

## 剩余实机验收

新的真实验收须先准备具体独立副本、目标/差异、正常电源、私有输入位置和恢复/清理方案并获得授权。本轮不请求或复用M11已结束的授权。

1. 从明确源码生成新验收包，核实原始包和客户机组件哈希；在普通权限客户机内真实执行新增私有采集，而非仅使用模拟或旧M11报告。
2. 对每个已准入字段分别保存变更前、App应用后、App重开恢复后三份同会话快照及两份配置收据。逐次执行独立Windows对照和当前VMIL分类，保留失败及其他观测变化。
3. 在真实输入上完成App导入/对照/脱敏导出及双语流程；原值仅在私有快照/恢复记录中核对。网卡保持已准入断开范围，不扩大联网功能或修改策略。
4. 记录正常启动/工具就绪、原VM约定结束状态、实际副本独立性和最终归档/删除；不能把本轮未创建副本写成新实机清理已通过。

当前开发/模拟不等于M12完整实机闭环通过，也不等于v0.3发布就绪。M13、远程CI、新Release及全新OS验收均未开始。
