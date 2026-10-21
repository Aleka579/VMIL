# M11：404修复后的真实参数往返复验

后续2026-10-20夜[实际App/客户机验收](V03-M11-APP-ACCEPTANCE.md)已通过，并登记精确有限范围。下文只记录上午无磁盘研究探针的原始结果及当时边界，不把夜间结果追补为该探针已执行的测试。

2026-10-20，Asia/Taipei，当前worktree。用户具体批准新无磁盘探针方案后，**已有静态MAC和已有BIOS UUID的配置修改/原值恢复已实际通过**。探针始终不启动，现有虚拟机未进行配置、电源、磁盘或快照操作。本轮对象已移出库并删除，自有vmrest已退出。

## 精确范围与结果

| 项目 | 实际验证 |
| --- | --- |
| 分发与服务 | Workstation `17.5.2 build-23775571`，vmrest `1.3.1 build-23775571`；完整版本字符串、固定契约/程序哈希、127.0.0.1监听PID/映像绑定通过 |
| 目标 | 新建无磁盘、无ISO/共享目录、不启动的hardware19合成探针；已有静态MAC、hostonly且startConnected=FALSE。没有复制或修改现有VM |
| 注册 | 一次注册，唯一映射/完全关机/两字段API与VMX一致检查通过；首次401发生在注册前，没有重复注册 |
| MAC | 10:38:43应用后配置达到测试值；客户端因空成功正文解析失败停止。10:45:16继续时独立核实API/完整VMX目标态，再恢复原值并复核，没有重发MAC应用 |
| UUID | 10:45:17单次修改、独立API/VMX复核、单次原值恢复及复核通过 |
| 实际成功响应 | MAC恢复、UUID应用、UUID恢复的三份收据均 `httpStatus=200, bodyBytes=0, bodyKind=empty`。最初MAC应用的正文未捕获，不追补为已有响应元数据 |
| 写入/恢复保护 | 最终检查在持久化intent之后；每个请求只发一次。已观察到的两次应用、两次逆向恢复共四次参数PUT，无注册重放或MAC应用重放 |
| 最终配置 | 所有键值恢复原语义；原字节不相等，见下文，未直接覆盖VMX |

本机使用的写路由是 **`PUT /api/vms/{id}/params`**，name/value字符串及原媒体类型。随附Swagger误写`configparams`，静态定位及首轮HTTP回归见[404修复](V03-M11-404-FIX.md)。

## 本轮发现并修复的响应兼容问题

首次登录返回401，工具在GET清单阶段停止，事件0且原字节未变。用户明确要求重新打开原生窗口；第二次认证/注册成功，MAC已变，但工具对HTTP成功响应无条件解析JSON，引发`JSONDecodeError`，未决intent保留。

对相同固定vmrest映像只读检查，参数更新成功分支调用 `WriteResponseJSON(200, nil)`，nil分支不写JSON正文。研究工具现在只允许 **参数PUT的空200响应**，随后仍以独立API和全部VMX键值复核判定结果；GET、注册、其他PUT及非空无效JSON不因此放行。成功收据仅增状态/字节数/正文类型，不保留原文或凭据。

响应丢失/解析失败后的处理沿用原批准方案：先保存MAC目标态证据，新的原生窗口输入凭据后执行有限`continue-uuid`，核实/恢复MAC，再执行尚未发出的UUID；没有重做MAC应用。`uuid-completed`不回填缺失的初次MAC applied事件，MAC目标态的独立核实及其恢复收据分别保留。

## 配置语义与字节差异

API结束时配置由462变为476字节，全部键值仍与基线一致；14行LF变为CRLF，`uuid.bios`行移到文件末尾。因此完整SHA256与基线不同，不能宣称原字节恢复。没有为了得到相同哈希而回落VMX替换。

后续只打开配置标签以移出库，GUI将VMX变为554字节，另建260字节vmxf及0字节vmsd。没有创建快照对象或启动虚拟机。**这些GUI后的最终正文已重新归档**，与较早API阶段分别保存，不再重复上一轮未保存最终元数据的遗漏。

## 自动化与归档检查

已有Python3.14.8，在当前worktree运行 `python -I tools/research/test_identity_api_probe.py -v`：

- 空响应/租约修复前 **43项中40通过、3错误**，保留原失败。
- 修复后 **43/43通过，0失败/跳过，2.959秒**。严格HTTP替身使用真实空200正文，验证两字段四次合成往返、收据元数据、GET/注册空正文和非空坏JSON拒绝，原故障/防重放/恢复冲突用例保持通过。
- 归档最初误用另一文件句柄读取自己锁住的`lease.lock`，出现PermissionError后停止。按项目规则只读核对本轮Defender日志，没有1116/1117匹配；沙箱Get-MpThreatDetection访问被拒绝，没有重试或修改防护。原生离线用例复现字节锁行为，lease改为提供原句柄，归档通过该句柄读取，保持锁有效。
- 第一次部分归档5文件保留；修复后的未决态归档7文件，API完成归档14文件，最后GUI之后归档 **16文件**，逐文件SHA256核实后才删除唯一探针目录。

本轮未改生产C#、依赖/版本、M07有限策略或M10不可执行计划；原168/139 C#结果保持历史范围，没有重跑或冒充本轮新结果。未运行远程CI、App/客户机或新发布验收。

## 清理与证据入口

API结束后先用厂商Ctrl+C方式停止本轮自有vmrest，再完成GUI库移除，避免服务与GUI同时管理库。唯一新对象的确认框明确“不会从磁盘中删除”；移除后UI匹配0、持久`inventory.vmls`匹配0，现有Win10条目保留。

最后GUI动作之后归档全部新增/变化文件；解析并限定目标仍在批准父目录、拒绝重解析点/目录变化、逐文件核对源与最终副本哈希并确认VMX未占用，随后只删除批准的新探针目录。本轮没有删除锁文件、快照对象或其他虚拟机。

发现上一轮已删除探针的旧库残留条目仍可见；本次只操作新授权对象，旧条目未修改。这不影响本轮新对象匹配0/目录已删的结论，旧残留仍待另行处理；不将本轮结果表述为所有历史条目都已清空。

私有证据统一在 `local-data/m11-404-20261020/`：

- `real-retest.log`、`real-retest-credentials02.log`、`real-retest-reconcile-uuid.log`，保留401、解析失败和成功继续。
- `response-handler.json`、`response-json.json`及原静态路由证据；`empty-response-red-tests.log`、`empty-response-final-tests.log`。
- `pending02-archive-receipt.json`、`api-archive-receipt.json`、`final-archive-receipt.json`和对应原件；原不完整pending归档也保留。
- `workstation-remove-confirm.json`、`workstation-after-remove.json`、`library-removal-check.json`、`probe-directory-removal.json`及桌面操作日志。

该具体授权已执行结束，不重放本轮execute/continue-uuid/归档/清理脚本或重建旧UUID。

## 对M11的意义

精确构建、已有静态MAC/已有BIOS UUID的**研究通道与有限原值恢复**已有真实证据。它没有验证开机后的客户机效果、有效网络身份、自动生成/复制生命周期、缺失键恢复或生产App闭环。M11整体验收仍 **2/5**，新生产身份准入 **0**；不自动扩展产品后端、版本范围或M12/M13。
