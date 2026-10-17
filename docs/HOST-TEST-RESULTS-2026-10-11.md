# 宿主机测试结果：2026-10-11

时间范围：2026-10-11 10:00–11:18，UTC+08:00（Asia/Taipei）。用户授权由Codex操作并优先复用现有工具，随后要求客户机完成激活后再验证，本轮完成宿主机范围。

## 构建与范围

- 开始基线：7bf7eff；分支：codex/v01-testing-20261011。
- 先测试原有0.1.0-alpha.1，再测试本分支的GBK修复自包含构建。修复构建保留Alpha版本号，是本地测试产物，不是新正式Release。
- 宿主实测：Windows 11 x64、Build26100；普通用户令牌，UI驱动记录elevated=false。
- VMware/vmrun：17.5.2 build-23775571。Python3.14.8、.NET SDK10.0.401、Runtime10.0.12。
- 复用已安装的MCP、vmrun、.NET及Windows UI Automation。原生文件对话框使用标准WM_COMMAND/BN_CLICKED通知；本地测试驱动不加入应用解决方案或发布包，没有新增第三方依赖。

## 实际通过

| 证据 | 场景与结果 |
| --- | --- |
| HOST-01 | 普通权限GUI检测五类14项，13项可用；OS Build、CPU、16逻辑处理器和OS可见内存与独立CIM查询一致。宿主Hypervisor信号存在，VMware客户机证据结论为inconclusive，没有单凭Hypervisor判成客户机。 |
| HOST-02 | 检测过程中取消、页面切换正常；取消后保留已有报告。取消后的JSON与原报告SHA256相同。 |
| HOST-03 | 通过实际保存对话框导出JSON/HTML，文件已落盘且可解析。已有报告出现明确覆盖确认，实际选择“否”后恢复可操作状态。 |
| HOST-04 | 合成不支持版本报告显示格式/版本错误，已有报告保留，下一次合法导入成功。取消文件选择后也恢复正常状态。 |
| HOST-05 | GUI导入同一宿主报告A/B及明确标记SYNTHETIC的比较报告，差异表14行；修改的逻辑处理器、内存和系统名称显示不同，内存4 GiB显示正确。此证据不能代替真实客户机闭环。 |
| HOST-06 | 中英文切换及英文五页均实际访问并保存控件树；恢复中文。新增不可解析VMX提示的中英文均实际验证。 |
| HOST-07 | 修复版只读解析真实GBK VMX，配置表130项；导出5个公开声明，其他配置值不进入报告，电源状态明确unknown。 |
| HOST-08 | 修复版JSON/HTML窄范围脱敏检查通过：14项观测、5个公开VMX声明；无测试用户名、真实路径、UUID/MAC，HTML无脚本或外部资源引用。不是整仓库/新Release包扫描。 |
| HOST-09 | 独立短窗口VMX读取：读前/读后字节哈希相同、CLI退出0；原始字节只保存在私有证据目录，不修改或恢复真实配置。 |
| REG-01 | GBK回归修复前49例中3失败/46成功；修复后49/49通过、无跳过。 |
| REG-02 | Release构建0警告/0错误，文件/合成流程10/10通过，自包含测试构建生成成功。 |

## 发现与修复

1. 真实VMX声明GBK，旧解析器只接受UTF-8/UTF-16/Windows-1252，返回零配置项。补充GBK/cp936/windows-936严格codepage936解码，保留原始字节；新增中文值、公开声明与非法字节回归。
2. 原UI只提示电源状态未知，无法区分零配置项。增加中英文“检查编码或内容”提示，并实际验证。
3. 驱动初版混淆相同ID的地址栏和文件名控件，未及时停止后续点击；一次新报告误写默认目录，核实后移至私有证据目录。已改为编辑框/原生按钮类型核实、输入回读、状态等待和失败即停止。原生打开框文件名ID1148，保存框1001；驱动完成不等于文件/界面验收通过。
4. 普通前台激活/键盘探测不能稳定操作本机VMware控制台和原生对话框；失败代码已从维护工具删除。现有cua_repl注册为禁用，本轮未改变它。

## 未通过与未验证

- 客户机：运行状态与Tools running查询成功，但未提供认证的进程查询超时；明确空密码认证返回“客户机操作系统不支持空密码”。用户要求留待激活完成后验证，未采集实际Build或报告。
- 应用可靠电源状态提供器仍未接入；MCP运行列表成功不代表App显示可靠状态。
- 干净Windows环境、远程CI、客户机闭环及v0.2真实配置管理均未验证。
- 原主checkout的Alpha ZIP未重新打包，仍含修复前GBK问题。修复源码和测试构建不能表述成旧ZIP已修复。
- 长会话中真实VMX哈希发生变化，最后写入时间早于修复版UI读取；具体外部来源未确认。只把独立短窗口读前/读后一致记为通过，不宣称整轮VMX恒定。

## 复现与私有证据

实际机器路径和原始日志只保存在WORK_LOG.md引用的私有目录：local-data/test-results/04-v01-validation/20261011-100016/。actions.jsonl记录驱动参数、UTC+08时间、结果和令牌；报告、GUI树、VMX字节、截图不提交。

```powershell
dotnet build VMIdentityLab.sln -c Release -m:1 --disable-build-servers
dotnet test --project tests/VMIdentityLab.UnitTests/VMIdentityLab.UnitTests.csproj -c Release --parallel none --report-xunit-trx --report-xunit-trx-filename gbk-after.trx --results-directory artifacts/test-results/20261011-host
dotnet test --project tests/VMIdentityLab.IntegrationTests/VMIdentityLab.IntegrationTests.csproj -c Release --no-build --parallel none --report-xunit-trx --report-xunit-trx-filename host-files.trx --results-directory artifacts/test-results/20261011-host
dotnet publish src/VMIdentityLab.App/VMIdentityLab.App.csproj -c Release -r win-x64 --self-contained true -m:1 --disable-build-servers -o artifacts/publish/ui-gbk-fix
```

SDK由global.json固定，实际复用主checkout的既有NuGet缓存；CLI状态写入本项目artifacts，构建/测试遥测及首次开发证书生成关闭。首次新worktreeNuGet还原因沙箱网络访问失败，未算测试执行；安全记录核对后才在允许网络上下文完成还原/测试。Defender1116/1117查询未发现本轮匹配事件，历史受限脚本继续禁止执行。

测试产物：artifacts/test-results/20261011-host/gbk-before.trx、gbk-after.trx、host-files.trx；修复测试程序：artifacts/publish/ui-gbk-fix/VMIdentityLab.App.exe，运行时保留同目录全部文件。

依据：[Microsoft编码提供程序](https://learn.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=net-10.0)、[Windows代码页](https://learn.microsoft.com/en-us/windows/win32/intl/code-page-identifiers)、[原生控件通知](https://learn.microsoft.com/en-us/windows/win32/menurc/wm-command)。
