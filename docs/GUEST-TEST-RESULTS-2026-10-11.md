# 客户机实际测试结果 — 2026-10-11

用户完成激活、打开客户机并设置非空登录密码后，执行v0.1只读客户机测试。客户机采集与宿主报告交换已通过，新增V01-03、V01-04，验收更新为24/37（v0.1 22/25、v0.2 0/6、发布2/6）。干净环境和正式发布仍未验收完成。

## 环境与产物

| 项目 | 实际结果 |
| --- | --- |
| 客户机 | Microsoft Windows 10 教育版，10.0.18363，x64；具体累积更新补丁未采集 |
| 授权状态 | SoftwareLicensingProduct的Windows Education项LicenseStatus=1，未查询产品密钥 |
| 运行权限 | 普通用户，非提升令牌，交互Session 1 |
| CPU / 内存 | Ryzen 9 8945H，4逻辑处理器，6441422848字节可见内存 |
| 虚拟化平台 | VMware Workstation/vmrun 17.5.2 build-23775571；Tools状态running |
| VMware Tools | 安装元数据11.3.5.18557794；vmtoolsd.exe文件版本11.3.5.31214，二者为不同版本字段 |
| 应用 | 基线e91e6c6的GBK修复自包含构建，Runtime10.0.12，App0.1.0-alpha.1 |
| 测试接入 | 既有7个读取MCP工具；认证复制/执行复用原生vmrun与临时本机CredUI会话 |

本次未在客户机安装SDK、.NET运行时、Python或新PowerShell，系统查询使用其既有Windows PowerShell。标准Program Files下的SDK目录不存在；该检查不等同于完整软件盘点，也不能把现有客户机视为干净目标环境。本项目允许对此Windows10环境进行实测，不据此声称微软官方支持该版本或兼容所有Windows10/11版本。

完整App目录与同版本运行时的自包含UIA开发驱动共享运行时文件。私有验证ZIP为65410889字节，SHA256：58230EB30811D8D5ED5D28334A2BC05F822037D56E3A1DF22C91B767FB6810A6；客户机复制后的哈希一致。后续只更新本轮测试目录内的驱动DLL修复交互，不修改App二进制；命令045/048保留该过程。验证包包含开发驱动，不是正式Release。

## 实际执行与验收

| 编号 | 操作 | 结果与证据 |
| --- | --- | --- |
| GUEST-01 | 本机非持久凭据窗口认证、复制/解压/哈希复核 | 通过；commands/results 001–010，认证由用户本机输入 |
| GUEST-02 | 客户机CLI采集及独立系统查询 | 通过；011采集退出0，14/14 Success，Win32 CIM/安装元数据对照一致 |
| GUEST-03 | 客户机GUI采集、JSON/HTML保存并取回 | 通过；025、029、049、052；GUI与CLI的14项值一致，保存框关闭且界面恢复 |
| GUEST-04 | 客户机语言及页面切换 | 通过；中文概览/检测/导出、英文五页实际选择；055及五份英文控件树 |
| GUEST-05 | 宿主导入真实报告A/B，查看差异，再导出 | 通过；宿主实际控件操作及14行差异表，导出的A与输入宿主报告结构完全一致 |
| GUEST-06 | 六份本轮新导出脱敏与静态HTML检查 | 通过；guest-report-check.json，私有check_guest_reports.py退出0 |
| GUEST-07 | 测试进程与临时会话收尾 | 通过；客户机App正常关闭，067进程查询无本轮App；宿主App退出，071会话stopped，认证Python进程退出，VM仍在运行 |
| GUEST-08 | 本轮Defender事件只读核对 | 宿主/客户机1116/1117均NoMatchingEventsFound；仅表示该范围未查到事件，历史禁令保持 |

独立核实字段包括OS名称/Build、CPU型号与两个CPU对象逻辑处理器总和、内存、系统厂商/型号、BIOS版本、HypervisorPresent、PnP VMware设备计数和Tools安装元数据。CPUID厂商字符串来自应用实际采集；不把它单独视为客户机身份保证。

CLI实际形式：

```powershell
.\VMIdentityLab.App.exe --collect --output .\guest-report.json --html .\guest-report.html
```

原生vmrun调用采用参数数组：runProgramInGuest、CopyFileFromHostToGuest、CopyFileFromGuestToHost；GUI使用interactive和既有UIA驱动。具体vmrun参数、路径、执行时间和退出码只保存在私有队列及结果，不保存-gu/-gp认证参数。宿主GUI过程由host-ui-actions.jsonl记录，并非用户人工点击。

## 真实对比结果

报告A为已核实宿主，报告B为本轮客户机GUI导出；不是合成样例。

| 字段 | 宿主A | 客户机B | 实际界面结果 |
| --- | --- | --- | --- |
| CPU型号 | Ryzen 9 8945H | Ryzen 9 8945H | 相同 |
| Hypervisor信号 | true | true | 相同；不能单独分类 |
| Hypervisor厂商 | Microsoft Hv | VMwareVMware | 不同 |
| 逻辑处理器 | 16 | 4 | 不同 |
| VMware设备数 | 3 | 8 | 不同 |
| 系统Build | 26100 | 18363 | 不同 |
| 内存显示 | 15.29 GiB | 6 GiB | 不同 |
| 系统厂商 | ASUSTeK | VMware, Inc. | 不同 |

GUI目前导出报告A的JSON/HTML，差异表保留在对比页；本次不声称已支持“导出差异报告”。已验证A导入后再次导出的完整JSON结构与原输入相同。客户机的CLI/GUI导出时间不同，但14项采集值一致。

## 失败尝试与处理

- 019客户机PowerShell进程编号查询退出1，原因未确认；认证进程列表成功定位本轮App，未重复原失败查询。
- 不带登录凭据的captureScreen明确要求先VixVM_LoginInGuest，没有生成截图；不重试匿名入口。本次GUI证据来自指定进程的真实UIA树/操作/导出文件，不把合成渲染当作截图。
- 026把页面条目当Invoke按钮调用，返回“found 0”；核实其ListItem类型，改为既有select，029通过。此为测试调用错误。
- Win10保存框ValuePattern.SetValue后显示文本已变，但实际采用旧默认文件名，出现System32目录权限提示；033原生命令超时，文件未生成。先核对宿主/客户机Defender事件，无匹配1116/1117；未提升权限或改变文件夹权限。
- 改用Save按钮Invoke、单独增加SetFocus均未解决旧文件名问题；失败证据保留。SetFocus改动已撤销。最终给指定原生Edit控件发送WM_CHAR，核实窗口类、所属进程及回读值，049/052实际生成指定JSON/HTML。Windows11既有value/native-command流程仍通过。此修改只涉及开发测试驱动。
- 初版会话在任何非零返回后清除认证并结束，导致普通查询失败也要重新输入；改为暂停依赖操作、允许独立取证、显式resume。完成后stop退出；Defender拦截仍必须停止，不能用resume恢复被禁操作。

## 私有证据与未完成事项

证据根：local-data/test-results/04-v01-validation/20261011-115348/，含commands/results、独立系统JSON、两轮采集报告、UI树、UIA操作日志、guest-report-check.json与最终session.json。真实机器信息、路径、VMX、截图和报告不进入Git；公开文档仅保存脱敏摘要。

六份新导出已检查用户名、机器名、绝对路径、UUID、MAC及HTML脚本/远程引用；此检查只覆盖本轮文件，不替代新正式Release包的敏感信息审查。密码由Windows本机对话框输入，不进入聊天/磁盘/队列；vmrun仍会在调用时使用原生认证参数，不能声称消除进程参数暴露或保证Python内存完全擦除。

剩余：干净环境V01-06/REL-06、App可靠电源状态VMX-04、配置官方版本依据VMX-03、完整发布集成验收、远程CI与私密安全报告渠道。旧Alpha ZIP未更新，仍不包含GBK修复。v0.2配置写入/回滚未启动。
