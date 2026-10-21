# v0.2 CI原包客户机验收（2026-10-18）

这是旧3832a6a包的历史失败记录。后续修复后的新e27543f包已通过23/23，见[CPU修复验收](CPU-FIX-ACCEPTANCE-2026-10-18.md)；下方原始结果不变。

本轮严格检查 **20/23，通过标志为false**。普通权限GUI功能通过，但两次独立CLI的CPU类超时，完整CLI采集及其相关值对照未达标。不能将历史37项验收或GUI结果当作本CI候选全部通过。

## 产物与环境

| 项目 | 实际记录 |
| --- | --- |
| 程序版本 | 0.2.0-alpha.1 |
| 编译来源 | 3832a6ad9ee6c45dba79583f60e384bbebd67010 |
| CI | [Actions 39025479358](https://github.com/Aleka579/VMIL/actions/runs/39025479358)，Release零警告/错误，127单元/100模拟集成通过 |
| SDK / 自包含运行时 | 10.0.402 / NETCore与WindowsDesktop 10.0.13 |
| 原始ZIP | 65,196,874字节；412文件；解压147,120,982字节 |
| SHA256 | `bfc7f525b2e9bd6f247f41f6c629a0ab446a5411e867743836f785c7f92b17f5` |
| 客户机 | Windows 10教育版Build18363 x64；现有系统，不是全新OS |
| 独立CIM | 4逻辑CPU，6,441,422,848字节可见内存 |
| SDK / 系统.NET探测 | 三个标准目录、PATH、x64/x86登记均未发现；未扫描所有磁盘 |
| VMware / Tools | Workstation17.5.2 build23775571；Tools运行 |

原始CI ZIP传输后SHA一致，解压412文件逐项SHA全匹配。另添加四个同Runtime13开发UI驱动入口，均有单独哈希；它们不进入Release。没有重建、改写或重压缩原应用包。

## 结果

| 检查 | 结果与证据含义 |
| --- | --- |
| 普通权限 | 两次CLI日志及全部桌面动作均elevated=false |
| 首次CLI | 14项结构完整，9成功/5超时；CPU四项及PnP一项timeout，日志collection.partial |
| 独立CLI复核 | 10成功/4超时；CPU四项timeout，没有14/14成功证据 |
| GUI检测 | 14/14成功；CPU、内存及OS Build与独立CIM一致 |
| 双语界面 | 中文/英文五页均实际导航并保存控件树 |
| 真实报告对比 | 新宿主参考报告与客户机报告生成14行；英语切换后仍保留 |
| 导出与隐私 | 实际JSON/HTML导出；六份CLI/GUI导出脱敏，已知私有路径无匹配 |
| 运行时身份 | 四个实际加载.NET模块均来自新目录；程序集完整来源3832a6a，包内Runtime13身份与文件SHA相符 |
| 拒绝覆盖 | CLI对本轮已创建报告退出2；两个目标文件前后SHA不变 |
| 正常退出 | App正常关闭，后续列表没有测试App |

未通过的三项为：CLI十四项全部成功、CLI CPU值与独立CIM一致、CLI与GUI全部值一致。三者都涉及同一CPU分类超时；没有将unknown解释成0或不存在。其他二十项通过，原始失败结果保留。

## 慢查询调查与处理

独立CPU WMI查询9,240ms、CIM查询6,770ms，均返回两个处理器对象。采用与应用相同选项的首项查询，`ReturnImmediately=false`时Get耗时5,931ms，首项5,979ms，超过应用现有5秒分类期限。

按微软文档试验`ReturnImmediately=true`后，在2,052ms发生ManagementException超时，未恢复完整采集；该实验没有进入生产代码。参数语义参考[微软ReturnImmediately说明](https://learn.microsoft.com/en-us/dotnet/api/system.management.enumerationoptions.returnimmediately)和[枚举性能说明](https://learn.microsoft.com/en-us/windows/win32/wmisdk/improving-enumeration-performance)。独立计时说明存在慢查询，不声称穷尽了所有运行时差异或证明唯一根因。

原分类5秒、底层枚举2秒限制、生产代码及CI包保持原样，没有靠放宽阈值或循环重试取得通过。后续应单独处理慢查询与CPU独立字段的采集策略，补有界等待/部分结果测试，再由对应新CI包重新验收。

## 清理、范围与Release

本轮只用新客户机目录，没有完整VM副本、真实VM配置写入、快照或助手发起的电源命令。用户手动启动并登录，结束要求保持运行；清理结果见唯一工作日志。必要报告、动作、命令、失败及计时证据只保存在私有`local-data/test-results/ci-guest-20261018-1455/`。

既有[5de配置应用/恢复验收](V02-FULL-TEST-RESULTS-2026-10-17.md)保留原源码/包范围，本轮没有用CI字节包重复真实配置写入。全新OS、Windows11客户机、其他VMware构建/拓扑仍未据此验证。

[Release准备](GITHUB-RELEASE-v0.2.0-alpha.1.md)只生成本地草稿和候选资产，**暂缓发布**。没有创建GitHub Release或tag，也没有用这次结果改写历史证据。
