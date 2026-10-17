# v0.2 当前 Alpha 包验收结果

2026-10-14，Asia/Taipei。用户要求启动剩余验收。本轮对现有客户机执行当前产品包测试，完成可执行的只读闭环；真实配置应用/恢复仍未验收，完整 v0.2 正式发布尚未就绪。

## 实际测试对象

- 应用：0.2.0-alpha.1，来源 `9cfa28a2d06a21b55b92644187377d3d40c1a4de`；本轮起始仓库 `037782d`，没有修改应用源码或重打产品包。
- 原始产品 ZIP：`artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip`，65397689 字节。宿主与客户机实际 SHA-256 均为 `f1937b2af1980f51f89da449e6dffc5d1d317f10d3f30ad383553440befd666c`，测试结束宿主哈希仍一致。
- 目标：Windows 10 教育版 Build 18363 / x64，VMware Workstation 17.5.2、Tools 11.3.5.18557794。宿主版本不作为准入要求。
- 按用户已确认的“无需开发 SDK 或系统 .NET”目标验收。标准三处 .NET 位置、PATH、x64/x86 登记未见安装；实际新 App 加载自身目录中的 hostfxr、hostpolicy、coreclr、clrjit。普通权限查询和实际 UI 操作成功。不是全新 OS，既有软件未卸载，也不是全盘安装位置普查。
- 新客户机目录与单独开发驱动目录；未覆盖旧包、旧证据或用户已有文件。开发驱动不加入产品 ZIP。

## 本轮通过项

| 编号 | 实际结果 | 私有证据 |
| --- | --- | --- |
| V02-A01 当前产品包与声明目标 | 原始 ZIP 哈希一致、解压/CLI 成功；新 App ProductVersion 对应上述来源，包内运行时实际加载 | 003、006、025–032；runtime-loaded.json |
| V02-A02 采集与独立对照 | CLI 14/14 success；GUI 14 行。两份报告的系统、检测值/状态/来源/敏感级别一致；OS、Build、CPU、逻辑处理器与可见内存和独立 CIM 一致 | guest-cli.json、guest-gui.json、independent-system.json、gui-collected.json |
| V02-A03 只读 VMX 预览 | 无磁盘合成文件设为 ReadOnly；6144→4096 MiB、4→2 vCPU 两行差异及影响可查看 | synthetic-readonly.vmx、preview-zh.json；039–050 |
| V02-A04 中英文审阅 | 两种语言均显示目标/差异/影响、可返回；ConfirmConfigCommit 实际禁用，没有配置提交 | review-zh.json、review-en.json；051–059 |
| V02-A05 历史与输入失效 | 空历史 0 行，恢复/重试禁用；读取未创建历史目录。非法 4097 清除预览 0 行、禁用审阅并显示正确错误 | history-empty-en.json、preview-invalid-en.json、fixture-final.json |
| V02-A06 真实报告闭环 | 导入当前版本宿主 A / 客户机 B，14 行对比；16→4 逻辑处理器及不同 Build 等显示正确。导出 A 与实际输入 JSON 结构完全一致 | report-comparison.json、imported-host-export.json；084–098 |
| V02-A07 报告与文件保全 | 新 JSON / HTML 导出成功，默认 redacted；三份 JSON 和两份 HTML 未含 Windows 路径，JSON 未含本机账号值；检测项均为 public。合成 VMX 最终 ReadOnly、原字节 SHA-256 相同 | acceptance-check.json、guest-cli.json/html、guest-gui.json/html、fixture-final.json |
| V02-A08 收尾 | App 3128 正常关闭，查询无测试 App；临时凭据会话 stopped、父/子进程退出；原 VM 仍运行 | 104/105、credentials/session.json、WORK_LOG.md |

独立事实断言 `verify-current-evidence.py` 实际退出 0，结果保存在 `acceptance-check.json`。测试命令、参数、耗时、时间与退出码均保存在本轮 `credentials/commands/`、`credentials/results/`；所有原始报告/控件树仅在私有 `local-data/test-results/v02-acceptance-20261014-162000/`。

应用来源未变，既有 106/106 单元与 41/41 临时文件证据继续有效，本轮未重复执行。没有将历史 `9dfe84f` 客户机结果重标为当前包。

## 失败与处理

- 本机 show-window 时认证已完成，窗口已关闭；session 的 authenticated=true 与时间可核实。无需再次弹窗。窗口枚举辅助命令另有引号 SyntaxError，未执行且未重试。
- 010 环境脚本文件启动返回 1，011 输出不存在。012 未执行命令归档，防止恢复时重放。取证发现客户机执行策略 Restricted；原脚本保持失败，没有重跑或修改策略。转为独立短查询取得实际环境证据。
- 021 数组表达式错误拼接三个路径，退出 0 也判为无效；025 用明确 Join-Path 生成新证据，原错误文件保留。
- 080 测试驱动误用英文导航名 Compare reports，实际控件为 Comparison。新控件树核实后 084 成功；错误结果保留，不把它写成产品故障。
- 宿主只读安全查询与客户机导出的 Defender Operational 日志，在16:19至诊断导出（约16:28）时段未见 1116/1117/1121。该结果不解除项目历史高威胁禁令，不代表整机安全结论。

## 真实配置验收与远程项

CFG-01–06 仍为 **0/6**。发布白名单为空、实际提交禁用；两次状态/哈希检查没有证明与 VMware 启动或外部重命名互斥。未创建完整副本、未操作真实 VM 配置/电源/快照，合成文件属性与预览不属于真实配置验收。

安装目录随附的 Workstation Swagger JSON 只读审查确认有 `PUT /api/vms/{id}`、memory/processors 字段和 409 状态冲突响应；其 info 仍含版本占位符，字段没有允许范围或互斥保证。在线[官方接口说明](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/put/)也列出更新与冲突响应。它们提供后续验证方向，不能据此批准当前文件写入方式。摘要/哈希保存在 installed-api-schema-review.json，未运行 Swagger JavaScript、启动 API 或发送修改请求。

完整副本只读预检完成，具体目标/空间保存在私有 REAL-VM-ACCEPTANCE-PLAN.md；[后续步骤](V02-REAL-CONFIG-ACCEPTANCE.md)要求先落实保护与目标版本依据，再取得具体副本/电源/配置授权。远程仓库未配置，GitHub Actions、实际私密报告渠道与公开发布仍未完成。
