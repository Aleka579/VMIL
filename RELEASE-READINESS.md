# 发布就绪状态

2026-10-17 GitHub接入准备：用户指定仓库名VMIL，已连接并实际核实账号Aleka579/用户ID260032387；目标查询404、可访问仓库为空，仓库访问和具体远程授权尚缺。已修正安全政策/贡献指南/包说明中的旧禁用状态，补充工作分支CI触发与[VMIL发布文案](docs/RELEASE-DRAFT-VMIL.md)，现有官方Action完整标签SHA复核一致。拟公开内容使用经审阅源码快照，内部Git历史保留；[具体接入方案](docs/GITHUB-READINESS-2026-10-17.md)记录内容、影响和剩余证据，REL-03/04仍未完成。

2026-10-17当前：最新应用来源5de4ec4，Release零警告/错误、127/127单元与100/100模拟集成通过。真实provider状态拒绝/一次启动竞争、App旧预览拒绝及实际应用/历史重开/恢复、最新包客户机十六项检查完成，CFG6/6，总计**35/37**。仅批准17.5.2.23775571/hardware19/每插槽2核、4096↔6144MiB和2↔4vCPU。副本已删除、临时进程结束，原VM按用户选择保持挂起。远程CI与私密渠道仍未完成，正式发布未就绪；[全量结果](docs/V02-FULL-TEST-RESULTS-2026-10-17.md)记录已修复失败和实测限制，下方历史记录保留原日期/范围。

2026-10-16更新：生产App官方API后端、本地跨实例租约、未决防重放与参数恢复已实现；Release零警告/错误，127/127单元、86/86模拟集成及12项普通权限宿主UI检查通过。真实provider并发与App副本闭环未验收，CFG仍0/6，完整v0.2不能发布。见[API后端结果](docs/V02-API-BACKEND-2026-10-16.md)；此前[原生API副本往返](docs/V02-CONFIG-ACCEPTANCE-2026-10-16.md)通过并已删除副本。已有9cfa28a Alpha ZIP未重建、不含本次实现；下方既有包验证保留原版本与来源。

当前开发源码为 **0.2.0-alpha.1**。2026-10-14补齐候选/恢复预览、确认窗口及私有历史，106单元/41临时文件、宿主双语交互通过；本地自包含Alpha ZIP及清单已生成、校验并运行。应用来源9cfa28a，生产写入仍拒绝，CFG-01–06保持0/6，完整正式发布未就绪。见[本地准备结果](docs/V02-LOCAL-PREPARATION-2026-10-14.md)与[开发状态](docs/V02-DEVELOPMENT.md)；旧v0.1和客户机历史证据保留原范围。

2026-10-14下午当前产品ZIP客户机验收完成：原包哈希一致，普通权限CLI14/14、GUI/独立CIM一致，声明的无SDK/系统.NET目标实际加载包内运行时；ReadOnly VMX预览、中英提交禁用、空历史/输入失效及真实报告14行对比与导出通过。测试实例/凭据已结束，原VM仍运行。见[当前包验收](docs/V02-ACCEPTANCE-RESULTS-2026-10-14.md)。真实配置、全新OS、远程CI和私密渠道仍未完成。

v0.1既有包版本：**0.1.0-alpha.2**，用于区分已合并GBK修复和本机API状态查询的新版源码。v0.1功能验收已完成，正式发布尚未就绪；旧alpha.1 ZIP未包含GBK修复，2026-10-11完整验证包保留原版本及基线，不追改历史产物。实际验收为 **29/37**：v0.1 25/25、v0.2 0/6、发布4/6。alpha.2已通过Release构建（0警告/0错误）、81单元与10文件/流程测试，新包从干净提交ab177a7生成，程序集、包内元数据及实际JSON/HTML版号一致。真实客户机和目标环境证据来自历史验证，尚未对alpha.2重新执行。

## 实际通过的验证

| 项目 | 实际结果 | 证据 |
| --- | --- | --- |
| v0.2 最新Release / 自动化 | 0警告/0错误，127/127单元与100/100模拟集成，零失败/跳过 | artifacts/test-results/20261017-vendor-media/ |
| v0.2 有限API白名单与保护 | 精确构建/拓扑/前后值；真实运行/挂起409、旧预览拒绝、一次启动竞争和未决回归；不宣称全局CAS | [FULL17](docs/V02-FULL-TEST-RESULTS-2026-10-17.md) |
| v0.2 生产App真实闭环 | 确认/应用/备份/历史重开/恢复；应用及恢复各九项核对，启动后独立2CPU/4GiB及4CPU/6GiB | FULL17；两次journal/schema2收据，未知内容与实际启动元数据保留 |
| 最新5de包客户机 | 普通权限14/14、中英/真实比较/导出/脱敏/包内.NET等十六项通过；Win10教育版18363，无标准dotnet目录/PATH/登记 | FULL17；不是全新OS |
| 测试副本及进程清理 | 官方删除成功，目录不存在，API清单仅原VM；临时API/凭据/App已结束，8697无监听 | copy-cleanup-check.json / session-cleanup-check.json；原VM按用户选择挂起 |
| 四层架构与八模块任务 | M00–M05完成，M07有限范围6/6；M06本地准备完成，远程步骤待处理 | [模块入口](docs/MODULES.md)、[进度](tasks/PROGRESS.md) |
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

## 尚未完成与阻塞

| 验收/要求 | 状态 | 完成前所需条件 |
| --- | --- | --- |
| REL-03 GitHub Actions | 已准备，远程未执行 | 授权远程仓库/推送后取得真实运行结果 |
| REL-04 安全文档完整 | 许可/中英文使用文档已完成，私密渠道待配置 | 实际确认报告渠道与首发支持范围 |
| 公开仓库、推送、Release | 未授权/未执行 | 审阅具体内容后分别授权 |
| 正式发布 | 未发布 | 最新5de验证ZIP与历史Alpha均保留；远程CI、私密渠道、发布时支持范围复核及远程授权待完成 |

## 交付入口与边界

- v0.2本地Alpha：artifacts/releases/VMIdentityLab-0.2.0-alpha.1-win-x64.zip与.sha256；65397689字节、413文件、解压147065129字节，SHA256 f1937b2af1980f51f89da449e6dffc5d1d317f10d3f30ad383553440befd666c。源码9cfa28a2d06a21b55b92644187377d3d40c1a4de；清单哈希/CRC/许可、普通权限本机及当前客户机/声明的无SDK系统.NET目标通过。真实配置、全新OS与远程CI未测，不是完整正式v0.2发布。
- 最新v0.2本地验证：artifacts/test-packages/VMIdentityLab-5de4ec4-validation-20261017.zip与.sha256，65411594字节、413文件、解压147102566字节，SHA256 d6e3cbfeaef95cdebce963ed31dc633c85216cc7da23560f59cde170bc7d4b41。应用来源5de4ec4927e5d2eaca7b92e841a4881bbd92ff98；真实App有限配置闭环和同源客户机包已通过。开发驱动另置私有测试ZIP，不进入产品包；旧Alpha未覆盖，无公开Release。
- 发布文案：[VMIL当前草稿](docs/RELEASE-DRAFT-VMIL.md)，[旧只读包草稿](docs/RELEASE-DRAFT-0.2.0-alpha.1.md)保留原来源；贡献入口：[CONTRIBUTING.md](CONTRIBUTING.md)。渠道及远程状态以[接入方案](docs/GITHUB-READINESS-2026-10-17.md)为准，不用noreply邮箱冒充安全渠道。
- 完整源码：src/，解决方案 VMIdentityLab.sln；测试：tests/。
- 模块计划：[MODULES.md](docs/MODULES.md)、tasks/modules/M00–M07。
- 使用说明：[中文](docs/USER-GUIDE.md)、[英文](README.en.md)。
- 当前本地自包含目录：artifacts/publish/0.1.0-alpha.2-win-x64/；ZIP：artifacts/releases/VMIdentityLab-0.1.0-alpha.2-win-x64.zip，65364634字节、412条目、146952995文件逻辑字节。SHA256：494536446595132D4F042613B8106CA6C4D70AD00ED391D68788744EC093A229，配套.zip.sha256。BUILD-INFO.json记录构建基线ab177a777d6be65b173128d7aef31bd0f16f004c；后续交付文档/日志与CI元数据步骤不修改应用源码或重写该基线。
- CI：[ci.yml](.github/workflows/ci.yml)，固定官方稳定Action提交摘要、windows-2025；上传应用前生成实际版本/提交/SDK/运行时的BUILD-INFO。新增步骤PowerShell语法解析及人工静态审查通过，远程未执行。
- 版本入口：[DEPENDENCIES.md](docs/DEPENDENCIES.md)。宿主OS无版本门槛，客户机/干净目标允许Win10/11 x64，VMware最低17.5.2，MCP Python最低3.14；其他工具/依赖/CI保留维护期核查。
- Python3.14.8、MCP SDK1.30.0、7读取工具接入已完成；运行列表/Tools与原生临时会话、报告交换通过，未新增MCP开放方法。App可选本机vmrest GET状态源已实际验证；临时API与本轮App/认证会话已结束。
- 历史完整验证目录：artifacts/publish/readiness-20261011/；ZIP：artifacts/test-packages/VMIdentityLab-validation-20261011.zip，包含完整许可与基线/文件哈希清单，已在声明目标实测。原alpha.1 ZIP与该验证快照均保留原内容和标识，不追改为alpha.2或正式Release。
- main已合并验证成果，私有证据/产物已保全，MCP从main运行；旧worktree已解除Git登记并清空，Windows占用的空目录按用户决定保留。[交接清单](docs/WORKTREE-HANDOFF.md)记录实际结果；功能验收数量不变。
- 包含应用与所需微软运行时；不包含 VMware、Windows 镜像、SDK、真实 VM 数据、个人报告或私有调查。

## 下一步

本地及获授权VM验收已完成，有限范围见[FULL17](docs/V02-FULL-TEST-RESULTS-2026-10-17.md)。剩余是实际远程仓库/Actions运行与私密安全渠道；取得具体信息及远程授权后处理。既有包保留原来源，最新验证包与公开Release分开，不重复创建副本或把一次竞争结果外推为全局互斥。

