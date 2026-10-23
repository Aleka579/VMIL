# M11 精确构建的实际 API 探针结果

2026-10-20后续：[404定位](V03-M11-404-FIX.md)及[新真实复验](V03-M11-REAL-API-RETEST.md)已完成。工具修正PUT `/params`及空200正文，43项离线通过；新探针上已有静态MAC/UUID的应用/恢复通过、最终证据归档并清理，生产准入0。下列2026-10-19原实测失败及局限保持不变，不以新结果改写旧捕获。

2026-10-19，Asia/Taipei。用户已具体批准 `local-data/m11-resolution-20261019/REAL-API-PROBE-PLAN.md`，执行现已结束。仅操作唯一新建、无磁盘、断网且不启动的探针，现有 Win10 VM 未被本轮工具修改或执行电源操作。

**已验证注册和参数读取；未验证出可用身份写入通道。UUID 的 `PUT configparams` 明确返回404，MAC应用流程也以404结束。** 两次应用尝试后独立确认原值/配置未变，没有成功修改或真实逆向PUT，不能将“无需恢复”写成“真实恢复已通过”。新生产身份准入0，M11完整标准仍2/5。

## 精确目标和实际结果

| 项目 | 结果与范围 |
| --- | --- |
| 安装产品 | Workstation `17.5.2 build-23775571`，规范版本17.5.2.23775571 |
| 实际服务 | `vmrest 1.3.1 build-23775571`，普通交互用户进程，唯一 `127.0.0.1:8697`；逐次核实PID/实际映像及分发哈希 |
| 契约/程序 | 三个固定SHA256见[通道与恢复记录](V03-M11-CHANNEL-RECOVERY.md)，实际本机绑定通过；不扩大为其他17.x构建 |
| 本机凭据交互 | 两次原生窗口输入后实际认证成功，不保存密码、不发送到聊天/参数 |
| 注册及读取 | 22:16:36，官方注册及两字段参数读取成功；唯一探针可映射，关机状态及API/VMX一致检查通过 |
| MAC应用流程 | 持久化apply-intent后返回HTTP404；当时日志未区分请求资源，不能追补为已捕获某一特定PUT响应正文。22:23:20重新认证并独立确认原状态，记录reconciled-original，没有重试MAC或发送逆向写入 |
| UUID应用 | 原方案尚未发送的单独尝试明确记录 `method=PUT, resource=configparams, httpStatus=404`；随后API/VMX/状态/完整哈希独立确认请求前状态未变，记录rejected-unchanged，reverse=false |
| API阶段原始字节 | VMX与原始备份均SHA256 `F6F306C812AD3E01A1BA9B658B076F6499A1A8B4245D444A4A472C7E28B173DB`；归档10文件逐文件哈希一致 |
| 清理 | 已核实确认框指定完整探针名称、明确仅从库移除；之后库/标签探针匹配0、原Win10条目保留。解析并限定确切UUID目录后删除；本轮vmrest按厂商Ctrl+C方式退出，PID已消失，探针Python进程0 |

这组结果限定本次无磁盘探针、字段、合成值和精确分发文件。404不能单独证明整个路由不存在、所有值/VM均不支持，或厂商界面功能不存在；通用契约声明也不能代替本次未通过的逐字段实测。未尝试私有参数、其他写路由、直接替换VMX、系统安装/升级或放宽保护。

## 恢复和故障处理

此前的单键设计已解决“已有值往返必须先有删除键/多键原子API”的不必要限制；原M10自动生成/复制流程仍保留各自多字段、存在性及关联恢复要求。

本次新增7项离线回归，最终 **35/35通过**、0跳过，2.057秒：

- 仅请求收到4xx且独立证明API/VMX/完整哈希/状态仍为请求前状态，才记rejected-unchanged；恢复请求的“未变”可能仍是已应用值，必须保留reverse标记并独立判断，不等同原状态。
- 拒绝却出现副作用、读回不明或验证失败仍保留intent，禁止重放。
- 原MAC未决后只显式核实/恢复该原值，再继续原计划尚未发送的UUID；不重新注册或重试MAC。
- 未决逆向请求若仍是目标值，禁止再次发送逆向请求；若已恢复原值只记录、不再写。
- 请求错误只记录方法/固定资源类别/HTTP状态，不回显厂商正文、VM ID或凭据。

既有28项覆盖保持通过。可复现命令 `python -I tools/research/test_identity_api_probe.py -v`，实际日志 `probe-continuation-tests.log`。App/主产品源码、生产策略及既有M07收据不改；原168/139 C#结果保持其历史范围，不声称本轮重跑。

## 清理验证与证据局限

为完成库移除，在API调用结束后只打开探针配置标签，不启动VM。补充开发驱动 `context-menu`：唯一TreeItem选择回读、所有者PID核实、标准/实测ATL树控件白名单后投递WM_CONTEXTMENU。两次实际Release构建均零警告/错误，最终标准库菜单及明确移除确认框验收完成；没有键鼠扫描码、磁盘删除菜单或DELETE API。

最初子菜单无InvokePattern，按已有ExpandCollapsePattern展开；树类实际为 `ATL:SysTreeView32`，只读确认后补齐有限类名匹配。相关原失败/快照保留，不把失败记通过。对应原生消息依据见[微软说明](https://learn.microsoft.com/en-us/windows/win32/menurc/wm-contextmenu)。

**证据局限：**10份归档对应API结束、打开清理GUI之前。清理GUI阶段之后观察到VMX从462增至554字节、新增260字节vmxf及0字节vmsd元数据；删除前记录了名称/大小，但未再次归档其正文。不能宣称GUI清理阶段仍全字节一致，也不能据此分析新增元数据正文。没有创建/删除快照对象、虚拟磁盘或客户机数据；删除的是用户批准的整个新无磁盘探针目录。后续同类清理须在最终GUI动作后追加保存差异与哈希，再删除目录；本轮完整API原值证据仍保留。

私有证据统一在 `local-data/m11-resolution-20261019/`：

- `real-probe-execute.log`、`real-probe-uuid.log`及`probe-evidence-dda14e10-13a4-4c54-882e-a74453b5552d/`中的六份事件、原始字节和合成值。
- `installed-fingerprint.json`、`owned-vmrest-process.json`、`vmrest-interactive-user.log`、`archive-receipt.json`。
- `workstation-library-context.json`、`workstation-remove-result.json`、`workstation-after-remove.json`及桌面操作日志。
- `library-removal-check.json`、`probe-directory-removal.json`、`final-process-cleanup.json`，确认已清理而证据保留。

本具体授权已执行结束，不创建新探针、不重放旧会话。后续若继续身份写入，需先调查可用官方通道及这组请求被拒原因，再准备新的有限实验；实际逆向恢复、App及客户机闭环仍缺证，不自动启动M12/M13、升级、推送或发布。
