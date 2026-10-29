# 发布就绪状态

## 当前v1.0.0正式版

用户已要求发布v1.0，源码统一1.0.0，自包含运行时与采集依赖10.0.14，现有功能/报告schema/有限配置政策保持。实际范围及完整回执入口为[V1发布记录](docs/V1-RELEASE.md)。

| 门槛 | 实际状态 |
| --- | --- |
| 本地Release / 单元 / 模拟集成 | 零警告/错误，240/240及206/206，失败/跳过0 |
| 打包 / 凭据模拟 / 官方许可核对 | 15/15、8/8、10.0.14许可/两包MIT及共享声明核对通过 |
| 主线CI / 最终原包 / 客户机 | main/8c30801的CI40894459119 success；413文件/来源/许可/全SHA通过，Win11普通权限部署6、CLI/PNG28、正常双语App50全通过，退出0及清理完成 |
| 正式tag / 三资产 / Release/latest | 2026-10-29已公开Release428034665，发布Actions40900265023 success；tag固定8c30801、三资产名称/大小/SHA/正文/Latest均独立核对通过，旧发布保留 |

## 当前v0.3.0-alpha.1预发布

2026-10-23，已按用户授权公开[v0.3.0-alpha.1预发布](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1)，M13完整5/5。26656db的[真实CI与宿主验收](docs/V03-M13-CI-RESULTS.md)、[最终原包客户机验收](docs/V03-M13-CI-GUEST-ACCEPTANCE.md)及tag/三资产服务器大小与SHA核对均通过，见[实际发布回执](docs/GITHUB-RELEASE-v0.3.0-alpha.1.md)。旧本地候选保留各自来源；[发布方案](docs/V03-RELEASE-PLAN.md)及[双语发布说明](.github/release-notes/v0.3.0-alpha.1.md)保留具体范围，不把历史v0.2计数当本版通过。

| 本版门槛 | 当前状态 |
| --- | --- |
| 源码版本/功能范围/未准入项 | 已固定0.3.0-alpha.1，M09–M12有限范围，与配置政策一致 |
| 双语文档、MIT/Daniel Waltz、安全渠道 | 已同步；隐私/实际依赖许可及包内容审查见M13实际结果 |
| Release/本地单元/模拟集成 | 本轮修复通过240/240及206/206，0失败/跳过；注释变化红测3失败及修复后通过均保留 |
| 干净来源、自包含ZIP、文件清单/SHA、普通权限运行 | bdec910包413文件/CRC/所有SHA/许可/内容通过；CLI/PNG14、正常双语GUI43、资源/夹具77通过，非最终CI包 |
| 新实际GitHub CI | 26656db/Actions39616530060 completed/success；原TRX240/206、Release零警告/错误，来源/版本核对通过 |
| 最终CI原包目标验收 | Win11 Build26100普通权限宿主14/43；Win10教育版Build18363/x64客户机28/50；413文件及原ZIP测试后完整性通过，最终证据已归档/测试目录已清理 |
| 全新OS、其他Build/拓扑/联网 | 未验证，不扩大兼容声明 |
| 本版推送/tag/Release及服务器资产回执 | 已公开预发布421405067，发布Actions39892345315成功；tag固定26656db，三资产大小/SHA与审阅一致，main/旧v0.2保留 |

已发布原CI包`VMIdentityLab-0.3.0-alpha.1-win-x64.zip`为65286300字节、413文件/解压147354242字节，SHA256 `b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779`；是原CI字节副本，未重压缩。SDK10.0.402/两运行时10.0.13及许可/来源/全部SHA核对通过，产品没有开发驱动。M13完整**5/5**；客户机测试记录器的原失败/首个App未知退出码保留，后续独立正常退出探针实际取得0，不伪造原值。实际发布及服务器核对已完成；全新OS等范围不继承。

此前3c28a74本地候选65516669字节/SHA942fb17cbf1318e5d3f8ebb0f8858523379ad3ed9c26d4da755a385ad9784af3及bdec910修复候选保留原范围，不覆盖、重标或作为当前CI资产。附加worktree已释放，所有必要原件已迁回主目录且逐文件SHA一致。

## v0.2及更早历史记录

2026-10-21 M12[有限范围实机验收](docs/V03-M12-REAL-ACCEPTANCE.md)完成5/5。237/200、真实快照150/150、四收据36/36、宿主和Win10客户机双语/脱敏各34/34通过；成功身份PUT恰四次且两标识独立恢复。原VM恢复挂起、关键配置核实保留；637份最后GUI后元数据已归档，唯一副本已移库并删除，临时凭据/API进程已结束。显式接口GUID仅私有保存，默认诊断1.0仍脱敏，M11准入未扩大，联网未验证。实测包来自e0f4e10，随后双语连接说明修正另经Release构建核实；不把旧包当作最终v0.3包。M13、新CI/最终包/全新OS验收及v0.3发布未开始，下方旧阶段和v0.2资产保持原来源。

2026-10-20夜M11[有限范围完成5/5](docs/V03-M11-APP-ACCEPTANCE.md)：真实App/客户机75/75、四收据36/36，默认策略仅登记精确版本和隔离单网卡的两个已有字段；最终199/192、正常入口确认8/8通过。副本及自有认证/API进程已清理，原VM恢复挂起。当前仍是未发布v0.3开发成果，程序集暂沿用0.2.0-alpha.1；M12/M13、新远程CI、最终包/全新OS验收及v0.3 Release未完成。下方按日期保留旧阶段状态和已公开v0.2资产，不能用旧CI替代新开发构建。

2026-10-19 M11：[当前可实施的事务开发/模拟已完成](docs/V03-M11-SIMULATION-RESULTS.md)，整体2/5仅为现有M07保护，不能宣称身份写入或v0.3已就绪。Release零警告/错误、168/168单元、139/139模拟集成，无跳过；原两项时序复现失败及修复证据分别保留。未执行真实API、客户机、独立副本、GUI、远程CI、自包含新包或Release验收；新身份写入准入0，版本仍0.2.0-alpha.1。下方阶段记录保留其当时状态；M12/M13未开始。

2026-10-19 M10组件完成本机合成验收5/5：Release零警告/错误、164/164单元、115/115模拟集成，无跳过；普通权限双语GUI20项通过，见[独立记录](docs/V03-M10-RESULTS.md)。计划固定未执行且不可执行，没有新身份写入批准。本轮没有远程CI、客户机、自包含新包或Release验收；M11–M13未开始，开发版本仍0.2.0-alpha.1。下方M09及v0.2记录保留各自来源与范围。

2026-10-19 M09验收阶段记录：组件已完成声明环境的补充验收，676d0ec实际CI140/113、原包宿主9/9、现有Win10客户机28/28及清理通过，见[独立记录](docs/V03-M09-CI-GUEST-ACCEPTANCE.md)。首轮PNG失败、局部修复和新CI原包分别保存；全新OS仍未验证。当时开发版本0.2.0-alpha.1，M09尚未形成Release、M10–M13未开始；M10最新状态见上方，完整v0.3尚未固定最终范围/版本。下方v0.2既有Release资产保持其原来源和范围。

2026-10-18当前版本**已按授权上传并公开预发布**：[v0.2.0-alpha.1](https://github.com/Aleka579/VMIL/releases/tag/v0.2.0-alpha.1)，tag精确指向e27543f，三份资产大小/SHA与审阅稿一致。CPU修复后的原CI包客户机严格23/23，原三项全部通过；两次普通权限CLI与GUI均14/14、独立CIM及全部观测值一致。产品CI130/106通过，发布任务39113269967成功。旧3832失败保留，REL-02/06通过，发布准备6/6、总计37/37，均限已声明环境；见[修复验收](docs/CPU-FIX-ACCEPTANCE-2026-10-18.md)及[发布记录](docs/GITHUB-RELEASE-v0.2.0-alpha.1.md)。

2026-10-17 GitHub验收完成：源码已按用户授权公开至[Aleka579/VMIL](https://github.com/Aleka579/VMIL)。[Actions 39024064418](https://github.com/Aleka579/VMIL/actions/runs/39024064418)实际completed/success，Release零警告/错误、127单元/100模拟集成均零失败/未执行，自包含publish与CI产物上传成功；实际下载TRX的SHA/CRC/计数核对通过。私密报告API enabled=true、入口按钮和链接可见，维护者Daniel Waltz已确认，安全政策与中英文说明已同步。REL-03/04通过，总计**37/37**；具体来源和限制见[GitHub验收](docs/GITHUB-READINESS-2026-10-17.md)。

公开源码从caa7f1f审阅包导入：149文件/333973字节，ZIP回读/CRC/敏感格式与来源核对通过；远程初始完整提交0c471bfd的149个blob均与批准包完全一致。仅公开AGENTS/日志脱敏，原本地记录及内部历史保留；App/测试相对已验收5de基线无差异，本轮不重复VM测试。后续文档提交由对应CI继续验证；CI包有独立来源，尚未进行客户机验收。GitHub Release/tag和面向发布的二进制资产尚未授权或发布。

2026-10-17 12:58本地/VM阶段历史：应用来源5de4ec4，Release零警告/错误、127/127单元与100/100模拟集成通过。真实provider状态拒绝/一次启动竞争、App旧预览拒绝及实际应用/历史重开/恢复、最新包客户机十六项检查完成，当时CFG6/6、总计35/37，远程CI和私密渠道待处理。仅批准17.5.2.23775571/hardware19/每插槽2核、4096↔6144MiB和2↔4vCPU。副本已删除、临时进程结束，原VM按用户选择保持挂起；[全量结果](docs/V02-FULL-TEST-RESULTS-2026-10-17.md)保留当时范围，不外推其他环境。下方更早记录保留原日期/范围。

2026-10-16更新：生产App官方API后端、本地跨实例租约、未决防重放与参数恢复已实现；Release零警告/错误，127/127单元、86/86模拟集成及12项普通权限宿主UI检查通过。真实provider并发与App副本闭环未验收，CFG仍0/6，完整v0.2不能发布。见[API后端结果](docs/V02-API-BACKEND-2026-10-16.md)；此前[原生API副本往返](docs/V02-CONFIG-ACCEPTANCE-2026-10-16.md)通过并已删除副本。已有9cfa28a Alpha ZIP未重建、不含本次实现；下方既有包验证保留原版本与来源。

当前开发源码为 **0.2.0-alpha.1**。2026-10-14补齐候选/恢复预览、确认窗口及私有历史，106单元/41临时文件、宿主双语交互通过；本地自包含Alpha ZIP及清单已生成、校验并运行。应用来源9cfa28a，生产写入仍拒绝，CFG-01–06保持0/6，完整正式发布未就绪。见[本地准备结果](docs/V02-LOCAL-PREPARATION-2026-10-14.md)与[开发状态](docs/V02-DEVELOPMENT.md)；旧v0.1和客户机历史证据保留原范围。

2026-10-14下午当前产品ZIP客户机验收完成：原包哈希一致，普通权限CLI14/14、GUI/独立CIM一致，声明的无SDK/系统.NET目标实际加载包内运行时；ReadOnly VMX预览、中英提交禁用、空历史/输入失效及真实报告14行对比与导出通过。测试实例/凭据已结束，原VM仍运行。见[当前包验收](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md)。真实配置、全新OS、远程CI和私密渠道仍未完成。

v0.1既有包版本：**0.1.0-alpha.2**，用于区分已合并GBK修复和本机API状态查询的新版源码。v0.1功能验收已完成，正式发布尚未就绪；旧alpha.1 ZIP未包含GBK修复，2026-10-11完整验证包保留原版本及基线，不追改历史产物。实际验收为 **29/37**：v0.1 25/25、v0.2 0/6、发布4/6。alpha.2已通过Release构建（0警告/0错误）、81单元与10文件/流程测试，新包从干净提交ab177a7生成，程序集、包内元数据及实际JSON/HTML版号一致。真实客户机和目标环境证据来自历史验证，尚未对alpha.2重新执行。

## 实际通过的验证

| 项目 | 实际结果 | 证据 |
| --- | --- | --- |
| v0.2 当前CPU修复 / 自动化 | 本地及e27543f CI均0警告/0错误，130/130单元与106/106模拟集成，零失败/跳过 | [CPU修复验收](docs/CPU-FIX-ACCEPTANCE-2026-10-18.md)，本地TRX artifacts/test-results/20261018-cpu-fix/ |
| v0.2 当前CI原包客户机 | 严格23/23；两次CLI及GUI14/14、独立事实与全值一致、双语/对比/脱敏/运行时/原文件SHA/拒绝覆盖均通过 | e27543f原包；现有Win10教育版18363 x64，不是全新OS |
| v0.2 历史5de构建 / 自动化 | 0警告/0错误，127/127单元与100/100模拟集成，零失败/跳过 | artifacts/test-results/20261017-vendor-media/ |
| v0.2 有限API白名单与保护 | 精确构建/拓扑/前后值；真实运行/挂起409、旧预览拒绝、一次启动竞争和未决回归；不宣称全局CAS | [FULL17](docs/V02-FULL-TEST-RESULTS-2026-10-17.md) |
| v0.2 生产App真实闭环 | 确认/应用/备份/历史重开/恢复；应用及恢复各九项核对，启动后独立2CPU/4GiB及4CPU/6GiB | FULL17；两次journal/schema2收据，未知内容与实际启动元数据保留 |
| 最新5de包客户机 | 普通权限14/14、中英/真实比较/导出/脱敏/包内.NET等十六项通过；Win10教育版18363，无标准dotnet目录/PATH/登记 | FULL17；不是全新OS |
| 测试副本及进程清理 | 官方删除成功，目录不存在，API清单仅原VM；临时API/凭据/App已结束，8697无监听 | copy-cleanup-check.json / session-cleanup-check.json；原VM按用户选择挂起 |
| 四层架构与八模块任务 | M00–M05完成，M07有限范围6/6，M06共享发布准备6/6；总计37/37 | [模块入口](docs/MODULES.md)、[进度](tasks/PROGRESS.md)、[GitHub验收](docs/GITHUB-READINESS-2026-10-17.md) |
| Release 构建 | 0 警告、0 错误 | WORK_LOG.md；项目内 SDK 10.0.401，单进程构建 |
| 单元测试 | alpha.2本轮81/81，0失败/0跳过；GBK修复前3例失败保留历史证据 | artifacts/test-results/20261013-version/alpha2-unit.trx |
| 临时文件/合成采集流程 | alpha.2本轮10/10，0失败/0跳过 | artifacts/test-results/20261013-version/alpha2-files.trx |
| 普通权限宿主采集 | 五类 14 项，13 可用；Tools 未安装，版本不可用 | 私有 local-data/test-results/m05/、m06/；stdout elevated=false |
| 信息一致性 | OS Build、CPU、逻辑处理器、可见内存与独立查询一致 | 注册表/CIM 对照；[兼容性](docs/COMPATIBILITY.md) |
| 默认 JSON/HTML 与错误脱敏 | 真实报告人工查看、窄范围特征检索与恶意合成测试通过 | 私有报告/日志、ReportTests 和 WORK_LOG.md |
| VMX 保留式只读 | UTF-8/UTF-16/Windows-1252/GBK、非法字节与原字节测试；真实GBK 130项/五个公开声明，独立短窗口哈希一致 | [HOST-07/09](docs/HOST-TEST-RESULTS-2026-10-11.md)，VmxTests |
| VMX-03 版本支持分类 | 17.5.2硬件版本官方依据、真实19已确认/合成21不支持，其他未评审项与歧义明确展示 | [依据表](docs/VMWARE-17.5.2-EVIDENCE.md)，RDY-01 |
| VMX-04 真实电源状态 | 运行/挂起/完全关机/API不可用四态界面与新导出正确，原VM恢复运行 | [本轮结果](docs/READINESS-TEST-RESULTS-2026-10-11.md)，RDY-02/05 |
| 五页 WPF 与双语资源 | 中文五页、独立 EXE 英文五页渲染完成，实际视觉核查 | artifacts/ui-preview/；全部合成预览 |
| 普通权限真实宿主交互 | 检测/取消保留/格式错误及恢复/选择VMX/导入/对比/拒绝覆盖/导出/语言切换通过 | [宿主结果](docs/HOST-TEST-RESULTS-2026-10-11.md)，私有actions.jsonl与控件树 |
| 普通权限真实客户机 | Win10教育版18363 x64，14/14 CLI/GUI采集、独立系统对照、JSON/HTML、英文五页通过 | [客户机结果](docs/GUEST-TEST-RESULTS-2026-10-11.md)，GUEST-02/03/04 |
| 真实宿主/客户机报告闭环 | 导入实际A/B、14行比较、导出A与输入结构一致；六份新导出脱敏通过 | GUEST-05/06，私有guest-report-check.json及UIA记录 |
| 本地自包含运行 | alpha.2 / win-x64 / Runtime10.0.12，普通权限EXE采集退出0，JSON/HTML版号正确；既有双语渲染证据保留 | artifacts/publish/0.1.0-alpha.2-win-x64/；私有local-data/test-results/version-20261013-alpha2/ |
| alpha.2包版本与来源 | EXE/四DLL均为alpha.2+ab177a7；ZIP内五个应用文件哈希与新目录一致，5份第三方文件/1份公共声明正文；旧包哈希未变 | 新包BUILD-INFO.json、ZIP与.sha256；WORK_LOG.md |
| V01-06/REL-06 声明的干净目标 | 用户确认无需开发SDK/系统.NET口径；Win10教育版18363普通权限运行，新完整包内四个.NET模块实际加载 | RDY-03；已有VC++/其他软件，非全新OS |
| REL-02 v0.1适用集成 | 自动化、真实宿主/客户机、报告闭环与本轮声明目标通过；范围和未执行项列明 | [宿主](docs/HOST-TEST-RESULTS-2026-10-11.md)、[客户机](docs/GUEST-TEST-RESULTS-2026-10-11.md)、[本轮](docs/READINESS-TEST-RESULTS-2026-10-11.md) |
| 正式原创许可 | MIT，Copyright (c) 2026 Daniel Waltz，用户已确认 | [LICENSE](LICENSE)、[确认记录](docs/LICENSE-DECISION.md) |
| 第三方许可 | 实际官方运行时和依赖许可/声明已核实并加入包目录 | [说明](THIRD-PARTY-NOTICES.md)、包内 third-party/ |
| 源码/当前包敏感审查 | 已检索密钥/用户路径，当前包无 VM 文件、PDB、测试日志或测试/遥测组件 | WORK_LOG.md；发布目录内容复核 |

## 发布状态与未验证范围

| 验收/要求 | 状态 | 完成前所需条件 |
| --- | --- | --- |
| REL-03 GitHub Actions | 通过 | e27543f实际Actions39109350478成功，130/106实际TRX、job日志及新产物核对通过；旧运行保留原范围 |
| REL-04 安全文档完整 | 通过 | MIT/Daniel Waltz、中英说明/第三方声明、正式私密入口及维护者确认 |
| 公开源码 | 已授权并上传 | 初始149个blob与审阅包一致，内部历史未上传；文档更新沿用公开历史快进 |
| GitHub Release / tag | 已授权并公开预发布 | v0.2.0-alpha.1 / e27543f；发布任务39113269967成功，三资产大小/SHA及tag回读通过 |
| 当前e27543f CI包客户机 | 23/23通过 | 原三项全部通过，声明的无SDK/系统.NET目标普通权限运行，见CPU18 |
| 历史3832 CI包客户机 | 20/23，失败保留 | 旧CPU超时结果及原包未改，不重标通过，见CI18 |
| 全新OS及其他构建 | 未执行 | 不继承本地5de ZIP字节身份，不扩大已验证兼容范围 |

## 交付入口与边界

- 当前预发布材料：artifacts/releases/github-v0.2.0-alpha.1-e27543f/；原CI ZIP字节副本65,197,403字节、412文件，SHA256 `77ab023491fb677eeaeff2d9860c9c771b718c762444eccd8d4e498d42de27b7`。完整目标e27543fb5a8519b9333b57af26f95dc3873a732c，原Runtime13；附ZIP哈希、逐文件清单、中英文说明与本地请求样稿。发布前候选状态保留历史；新增PUBLISHED-RECEIPT.json记录实际公开Release及服务器资产核对。不覆盖旧Alpha或3832暂缓候选，此次只读测试不重做5de真实写入。
- v0.2本地Alpha：artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip与.sha256；65397689字节、413文件、解压147065129字节，SHA256 f1937b2af1980f51f89da449e6dffc5d1d317f10d3f30ad383553440befd666c。源码9cfa28a2d06a21b55b92644187377d3d40c1a4de；清单哈希/CRC/许可、普通权限本机及当前客户机/声明的无SDK系统.NET目标通过。真实配置、全新OS与远程CI未测，不是完整正式v0.2发布。
- 最新v0.2本地验证：artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip与.sha256，65411594字节、413文件、解压147102566字节，SHA256 d6e3cbfeaef95cdebce963ed31dc633c85216cc7da23560f59cde170bc7d4b41。应用来源5de4ec4927e5d2eaca7b92e841a4881bbd92ff98；真实App有限配置闭环和同源客户机包已通过。开发驱动另置私有测试ZIP，不进入产品包；旧Alpha未覆盖，无公开Release。
- 发布文案：[VMIL当前草稿](docs/RELEASE-DRAFT-VMIL.md)，[旧只读包草稿](docs/RELEASE-DRAFT-0.2.0-alpha.1.md)保留原来源；贡献入口：[CONTRIBUTING.md](CONTRIBUTING.md)。渠道及远程状态以[接入方案](docs/GITHUB-READINESS-2026-10-17.md)为准，不用noreply邮箱冒充安全渠道。
- 完整源码：src/，解决方案 VMIdentityLab.sln；测试：tests/。
- 模块计划：[MODULES.md](docs/MODULES.md)、tasks/modules/M00–M07。
- 使用说明：[中文](docs/USER-GUIDE.md)、[英文](README.en.md)。
- 当前本地自包含目录：artifacts/publish/0.1.0-alpha.2-win-x64/；ZIP：artifacts/releases/VMIdentityLab-0.1.0-alpha.2-win-x64.zip，65364634字节、412条目、146952995文件逻辑字节。SHA256：494536446595132D4F042613B8106CA6C4D70AD00ED391D68788744EC093A229，配套.zip.sha256。BUILD-INFO.json记录构建基线ab177a777d6be65b173128d7aef31bd0f16f004c；后续交付文档/日志与CI元数据步骤不修改应用源码或重写该基线。
- CI：[ci.yml](.github/workflows/ci.yml)，固定官方稳定Action提交摘要、windows-2025；当前公开e27543f实际Actions39109350478完成130/106及publish，BUILD-INFO记录SDK10.0.402/Runtime10.0.13；对应原包客户机23/23已另行验证。旧3832的CI成功与客户机失败保留历史。
- 版本入口：[DEPENDENCIES.md](docs/DEPENDENCIES.md)。宿主OS无版本门槛，客户机/干净目标允许Win10/11 x64，VMware最低17.5.2，MCP Python最低3.14；其他工具/依赖/CI保留维护期核查。
- Python3.14.8、MCP SDK1.30.0、7读取工具接入已完成；运行列表/Tools与原生临时会话、报告交换通过，未新增MCP开放方法。App可选本机vmrest GET状态源已实际验证；临时API与本轮App/认证会话已结束。
- 历史完整验证目录：artifacts/publish/readiness-20261011/；ZIP：artifacts/test-packages/VMIdentityLab-validation-20261011.zip，包含完整许可与基线/文件哈希清单，已在声明目标实测。原alpha.1 ZIP与该验证快照均保留原内容和标识，不追改为alpha.2或正式Release。
- main已合并验证成果，私有证据/产物已保全，MCP从main运行；旧worktree已解除Git登记并清空，Windows占用的空目录按用户决定保留。[交接清单](docs/WORKTREE-HANDOFF.md)记录实际结果；功能验收数量不变。
- 包含应用与所需微软运行时；不包含 VMware、Windows 镜像、SDK、真实 VM 数据、个人报告或私有调查。

## 下一步

本轮CPU修复、CI、三项验收及授权后的预发布上传均已完成，版本仍为alpha.1。后续按维护任务处理；保留旧失败、发布脚本两次失败与成功记录及各包来源，不自动移动tag、重跑通过的测试或重建真实配置副本。

