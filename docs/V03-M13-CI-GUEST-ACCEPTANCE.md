# M13：最终CI原包客户机验收

2026-10-22，最终原CI包在声明的Win10客户机完成普通权限验收。M13当前**4/5**，剩余GitHub预发布及服务器回执；本记录不表示已经发布。

| 项目 | 实际结果 |
| --- | --- |
| 来源 | `26656db690d594bebcf2d69d273bfcd5dfc376bc`，[CI39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060)，240单元/206模拟集成通过 |
| 原产品 | artifact12354145051，65,286,300字节，413文件；SDK10.0.402、自包含运行时10.0.13 |
| ZIP SHA256 | `b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779` |
| 客户机 | Windows10教育版Build18363/x64；普通用户，工作区1289×691；4逻辑处理器，6,441,422,848可见内存字节 |
| CLI/PNG | 28/28；默认14项/可选17项、独立CIM、版本/来源/脱敏、拒绝覆盖、两语言预览和像素检查 |
| 正常App | 50/50；实际采集/导出、两语言五页、身份能力/依据、两个合成三阶段及配对历史、脱敏结论、运行时及正常退出探针 |
| 测试后完整性 | 全部413文件路径/字节SHA与原CI ZIP一致；客户机及宿主原ZIP SHA不变，产品未重压缩 |
| 证据与清理 | 最后界面动作后99份证据归档，已取回91份文件逐字节一致；仅本轮新测试目录已删除，凭据会话/进程已结束 |
| VM状态 | 用户手动启动并登录；验收后只读查询仍运行。工具没有执行电源、配置、快照或副本操作 |

CLI两组采集均返回成功，CPU型号/逻辑数、可见内存、OS/Build与独立CIM一致；标识来源正确，序列号/UUID/MAC只输出脱敏值。仅记录独立可用性布尔值，没有保存客户机原始标识。常见全局.NET路径/命令/注册项未发现；实际App加载的四个原生运行时映像路径及SHA均与包内原件一致。

正常App验证真实14/17项检测与报告导出；中文MAC与英文UUID分别导入合成的变更前/后/恢复后快照和两份配对记录，得到23检查行及3程序行。JSON/HTML版本、schema、配置/客户机结论、UI及导出脱敏均通过；VMware证据保留，网络可用性明确为未提供。合成输入不计作新的真实身份写入往返，M11/M12的旧实机结果仍保留各自来源。

## 失败与局部修正

- 首个凭据提示在隔离账户不可见；核实账户后关闭该提示，使用正常用户桌面的新原生会话，用户输入后认证成功。一次CIM进程查询被拒绝后先只读核对相关Defender记录，无匹配检测/处置事件；未重试该查询或修改安全设置。
- 固定滚到身份表100%后找MAC/UUID，在较小客户机工作区只实现了表尾。改为分段滚动实际定位，两语言行及选中依据随后通过；已完成的检测/导出不重做。
- 原生coreclr/clrjit的ProductVersion为逗号格式，原版本前缀断言不适用。改为核对实际加载路径及原CI映像SHA，原版本记录保留。
- 首个正常App已关闭，但异步PowerShell记录器未取得退出码，明确保留为未知。仅补做新的标准启动/关闭检查，`Start-Process -Wait`实际取得退出码0；不把首个未知值写成0。

这些修正涉及测试接入、定位及记录器，产品原包保持不变；失败、旧控件树、空退出记录和后续通过证据分别保留，没有将未执行步骤算作通过。

## 交付边界

原包已分别通过Win11 Build26100宿主14项CLI/PNG和43项双语GUI，以及本客户机28/50项；目标版本、系统类型和来源分别记录。这里是实际兼容验证，不是上游支持承诺或全新OS验证。其他Build、VMware版本/拓扑及联网仍未验证，现有身份写入准入不扩大。

原始报告/控件树及测试会话仅私有保存；公开材料只包含脱敏摘要。最终Release应使用该原CI ZIP、ZIP摘要及413条逐文件摘要三个资产，不能替换为本地候选或新的发布分支构建。

## English

On 2026-10-22, the unchanged original CI package from commit26656db/run39616530060 passed ordinary-user Windows10 Education build18363 x64 acceptance: 28 CLI/PNG checks and50 normal-App checks. Independent CIM observations matched, both languages and both synthetic three-stage validation flows passed, and all413 product files and the original ZIP remained unchanged. The original App's exit recorder returned no code; that value remains unknown. A separate normal launch/close check recorded exit code0. Initial credential visibility, virtualized-row lookup and recorder failures are preserved with their local corrections. No product bytes, VM configuration, power state, snapshots or VM copies were changed by the test tools. All99 final evidence files were archived before removing only the new test directory; the credential session ended and the VM remained running. M13 is4/5 until actual publication and receipt verification. Fresh-OS and broader compatibility claims remain unverified.
