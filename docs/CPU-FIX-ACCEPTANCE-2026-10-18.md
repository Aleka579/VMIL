# CPU修复后的CI包客户机验收（2026-10-18）

新CI原包严格检查 **23/23，通过标志为true**。原来未通过的三项已全部通过；[旧3832包20/23结果](V02-CI-GUEST-ACCEPTANCE-2026-10-18.md)及其原始证据保持不变。

## 来源与环境

| 项目 | 实际记录 |
| --- | --- |
| 版本与源码 | 0.2.0-alpha.1；`e27543fb5a8519b9333b57af26f95dc3873a732c` |
| CI | [Actions39109350478](https://github.com/Aleka579/VMIL/actions/runs/39109350478)，completed/success |
| 构建与测试 | Release零警告/错误；130/130单元、106/106模拟集成，零失败/跳过；实际TRX已下载核对 |
| SDK / 自包含运行时 | 10.0.402 / NETCore与WindowsDesktop10.0.13 |
| 原始CI ZIP | 65,197,403字节；412文件；解压147,122,006字节 |
| ZIP SHA256 | `77ab023491fb677eeaeff2d9860c9c771b718c762444eccd8d4e498d42de27b7` |
| 客户机 | Windows10教育版Build18363 x64；普通权限；既有系统 |
| 独立CIM | 4逻辑CPU；6,441,422,848字节可见内存；CPU型号去除末尾填充后相同 |
| SDK / 系统.NET | 三个标准目录、PATH及x64/x86登记未发现；未扫描所有磁盘 |
| VMware | Workstation17.5.2 build23775571；Tools运行 |

原CI ZIP哈希与GitHub产物元数据及客户机副本一致。测试后的412个原文件SHA逐项相符，额外仅四个已单独核对的同Runtime开发UI驱动入口；驱动不进入发布包。没有重建或重压缩CI原包。

## 原三项与完整检查

| 原未通过项 | 本轮实际结果 |
| --- | --- |
| 独立CLI十四项成功 | 首次及复核均14/14、collection.complete、elevated=false |
| CPU与独立CIM一致 | CLI和GUI均4逻辑CPU，与独立CIM一致；型号也相符 |
| CLI与GUI全部值一致 | 两次CLI和GUI的14项ID及全部观测值逐项一致 |

CPU型号改读标准CPUID品牌叶，CPU快速字段不再等待慢WMI单查询门；首次CPU类3.777ms、复核2.891ms。这是本机本轮实测值，不作为性能保证。分类5秒/枚举2秒限制不变，没有增加重试或慢WMI回落；[实现与回归](CPU-COLLECTION-FIX-2026-10-18.md)保留不支持状态和旧报告来源兼容。

其余检查同时通过：GUI14/14；中文/英文五页；实际宿主/客户机报告14行对比；实际JSON/HTML导出；六份导出默认脱敏及已知私有路径检查；四个实际加载.NET模块来自测试目录，程序集完整来源与Runtime13相符；CLI拒绝覆盖退出2且两个文件SHA不变；stderr为空；GUI正常退出，后续无测试App。

## 清理与交付边界

本轮新客户机目录已删除，并由只读存在性查询独立确认不存在；临时凭据会话和宿主辅助进程均结束。清理辅助脚本曾错误期待vmrun转发客户机stdout，移除命令实际退出0；没有重放删除，以独立查询完成确认。原VM按用户要求保持运行，Tools运行；本轮没有VM电源命令、真实配置写入、快照或完整VM副本。

原始命令、动作、控件树、报告、`guest/acceptance-check.json`与`cleanup-check.json`仅在私有`local-data/test-results/cpu-fix-20261018-1543/`保留。当前结果限于已有Win10教育版18363 x64普通权限目标；不是全新OS，也不代表Windows11或其他VMware构建验证。

[此前真实配置应用/恢复](V02-FULL-TEST-RESULTS-2026-10-17.md)保留5de本地包的有限范围。本次生产修改仅CPU采集/报告来源，配置模块未改；新CI字节包本轮没有重复真实写入。后续已按用户授权公开v0.2.0-alpha.1预发布，实际tag、三个资产及发布工作流结果见[发布记录](GITHUB-RELEASE-v0.2.0-alpha.1.md)；本验收记录不外推其他环境。
