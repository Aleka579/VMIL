# M11：MAC / UUID 官方支持与恢复补证

2026-10-20夜当前入口：[实际App验收与有限准入](V03-M11-APP-ACCEPTANCE.md)。两个已有字段的单键App/客户机往返及清理已通过，精确构建/隔离配置登记正常策略，M11有限范围5/5；本页下方按原日期保留调研/失败/当时缺口，不把系列手册当冻结17.5.2的API保证，原生成/复制和缺失键仍未准入。

2026-10-20最新：[修复后真实复验](V03-M11-REAL-API-RETEST.md)确认精确17.5.2已有静态MAC/UUID研究通道及有限原值恢复可用，43项离线通过，已归档并清理新探针；生产准入仍0。下面按日期保留原补证/404失败历史，不能替代新结果或扩大到原生成/复制、缺失键及App闭环。

最新[真实探针结果](V03-M11-REAL-API-RESULTS.md)：已获具体授权并完成注册/读取，UUID参数PUT404、MAC流程404，独立确认原值未变，探针和本轮API进程均已清理；最终35项离线通过，真实修改/逆向恢复未通过。以下为先前补证历史。

后续最新处理见[精确通道与有限恢复](V03-M11-CHANNEL-RECOVERY.md)：已实现独立单键技术探针及28项离线验证，精确绑定分发契约/二进制，真实API待授权。下面是上一轮补证记录；“删除键/多键语义”要求仍适用于原生成/复制流程，不是已有静态字段单键原值往返的前提。产品准入及本页历史结果不改写。

核查日期：2026-10-19，Asia/Taipei。当前 worktree 基线 `ba189d5`；本轮仅查官方资料、安装附带静态契约并更新不可执行计划说明，没有真实 VM、API 或凭据操作。

**已补齐17系列产品功能及备份原则的正文依据；精确17.5.2自动写入和完整恢复闭环仍未获证。** M11保持2/5，新身份生产准入0。不能将“有官方手工流程”写成“本App已支持事务修改”。

## 1. 新取得的官方原件

本轮从[官方文档地址](https://techdocs2-prod.adobecqms.net/content/dam/broadcom/techdocs/us/en/pdf/vmware/desktop-hypervisors/workstation/vmware-workstation-pro-17-0.pdf)直接取得完整PDF，并用现有Poppler提取正文、渲染核对相关整页。

| 项目 | 实际结果 |
| --- | --- |
| 封面 | VMware Workstation Pro 17 |
| 大小 / 页数 | 6,586,010字节 / 599页 |
| SHA256 | `AB6F87D972AF88B258983EE524F88BE52D20FE1794E31C64F2778FBD5C4F2CF5` |
| PDF生成元数据 | 2025-10-10；不是产品发布日期或功能引入日期 |
| 版本范围 | 汇编目录包含17.6.4；URL的`17-0`不是冻结的17.0或17.5.2版本证明 |
| 已完整视觉核查 | 物理页与印刷页一致：封面、158、160、161、237、238页 |

公开仓库只保留事实、引用和哈希，不捆绑厂商手册。当前worktree私有证据在`local-data/m11-evidence-20261019/`：原PDF、`manual-pdfinfo.txt`、正文、六页PNG及`EVIDENCE-INDEX.json`。[M08报告](V03-M08-FEASIBILITY.md)当时只取得搜索摘要，本次正文补证与其历史结果分开保存。

## 2. 已确认的能力与边界

| 项目 | 官方正文依据 | 当前结论 |
| --- | --- | --- |
| MAC界面管理 | 手册238页：高级网卡设置可输入地址或由厂商生成；不用于远程VM | 17系列有官方产品流程；未证明本机精确构建的API setter |
| 静态MAC约束 | 同页规定`00:50:56:XX:YY:ZZ`，第四字节`00-3F`，后两字节`00-FF`，需保持网络内唯一 | 有格式依据；本项目仍不接收任意地址，未分配或修改地址 |
| UUID复制生命周期 | 160页说明初次启动、复制生成、移动保留及挂起恢复差异 | 已取得Workstation专用正文；不是仅依据混合ESXi环境的KB |
| UUID手动指定 | 161页说明关机编辑`uuid.bios`及128位VMX格式；160页说明生成/保留策略 | 文档有手工能力，不能再笼统称“厂商没有UUID修改说明”；本候选仍限复制生命周期 |
| 身份关联 | 237页列出地址来源/生成字段、`uuid.location`、`uuid.bios`与MAC关联；[KB310359](https://knowledge.broadcom.com/external/article/310359/copy-workstation-virtual-machine-from-on.html)明确复制会生成UUID及MAC | UUID复制不是单键独立事务；需同时观测关联身份 |

以上是产品系列资料。没有取得冻结在`17.5.2 build-23775571`、覆盖逐字段自动写入及恢复的完整说明，也没有执行该构建的身份实验；不能把“尚未证明”写成“技术上不可能”。旧[MAC KB324973](https://knowledge.broadcom.com/external/article/324973)和[UUID KB320246](https://knowledge.broadcom.com/external/article?legacyId=1541)的环境范围限制仍保留，不以第三方转载补足。

## 3. 为什么现有API还不能形成身份事务

本机`vmware.exe`静态版本资源仍为`17.5.2 build-23775571`。重新读取安装附带`swagger.zip`中的`json/swagger_WS.json`，条目SHA256仍为`71c8094a4ab765151111ac4fd68a2bf8ace1061d470ef4ac2c5dc6f86aebb3f7`，与M08记录一致；没有执行厂商网页、脚本或API。

| 通道 | 已知输入/输出 | 不足 |
| --- | --- | --- |
| [NIC更新](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/id/nic/index/put/) | 请求只有`type`、`vmnet`；返回对象含`macAddress` | 返回属性不是可写参数；不能用改网络类型或重建网卡冒充MAC setter |
| 本机`configparams` | 单次`name`/`value`字符串；成功返回`ErrorModel` | 无逐字段适用清单；未声明删除键、恢复原先缺失状态或多键原子更新；成功响应不等于身份复核 |
| 本机`params/{name}` | 通用名称/值读取声明 | 不证明有效网卡、SMBIOS客户机观测、字段可写或恢复语义 |
| [克隆API](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/api/vms/post/) | `name`、`parentId` | 是复制操作，不是为原VM指定UUID的setter；没有可承诺的任意UUID输入 |

在线[索引](https://developer.broadcom.com/xapis/vmware-workstation-pro-api/latest/operation-index/)仍标1.2.1，与本机含通用参数的契约分别记录。不同资料不能拼成未经证明的字段支持保证。

**项目推论：**手册的MAC静态流程涉及原生成字段的移除；生成策略也可能原先不存在。只证明“设置字符串”不足以证明能精确恢复“键不存在”。不能假定空字符串等于删除，也不能假定多次PUT原子完成。即使之后获得单键读取/写入证据，仍须解决这些恢复状态和部分成功问题，再设计有限适配；本轮不预建通用参数写入器，不回落直接替换VMX。

## 4. 恢复范围与可接受的证据

官方[备份KB304707](https://knowledge.broadcom.com/external/article/304707/best-practices-when-backing-up-a-vmware.html)要求在完全关机而非挂起时复制VM文件；该KB的版本列表较旧。17系列手册158页另明确关联文件可能位于VM目录之外；[文件定位KB308646](https://knowledge.broadcom.com/external/article?legacyId=1003880)也要求查明外置磁盘。[编辑提示KB345489](https://knowledge.broadcom.com/external/article/345489/tips-for-editing-a-vmx-file.html)提醒保留备份并避免旧VMX整份覆盖后来修改。

以下为项目根据这些资料制定的恢复验收要求，尚未执行，不是厂商对一键回滚的保证：

| 范围 | 必须保留或核实 | 拒绝/待证条件 |
| --- | --- | --- |
| 网卡声明 | 适配器对应关系、地址来源、生成/静态字段的存在性及原字节；相关UUID | 不能把自动地址恢复简单替换为静态赋值；缺失键无法恢复时不准入 |
| UUID生命周期 | 原UUID、位置与生成策略的存在性、相关MAC；原值只留私有证据 | 恢复时重新选择“复制”可能再次生成身份，不能将新生成当作恢复 |
| VM资源 | 完全关机基线、实际外置资源和磁盘/快照链、相对路径、稳定目标位置 | 只复制VMX或遗漏外置父盘不合格；不删快照/锁、不压缩磁盘来制造前置条件 |
| 启动后状态 | 配置、客户机UUID/MAC、普通网络可用性及后来无关修改分别复核 | VMX字节一致不能证明客户机/网络恢复；不覆盖后来修改 |
| 外部状态 | DHCP/网络策略等实际受影响资源的所有者、验证及恢复边界 | VM文件冷备份不恢复外部服务；不能控制或证明的影响保持准入缺口 |

完整冷备份属于灾难恢复入口，使用它可能覆盖客户机后来的数据；它不能代替可保留后来修改的字段级受控恢复。只有同时明确适用边界并通过实际往返，才可评审新字段。不能用“有完整副本”直接补勾恢复完成。

## 5. M11剩余工作的执行顺序

1. **先确认自动化可表达性。** 补精确目标构建的字段支持、读取/写入、缺失键恢复和多键失败语义；可向厂商渠道核实，但本轮不自动发送消息或创建支持单。没有依据时保留界面/手工功能的事实，App保持不可执行。
2. **按可证实的有限范围设计适配。** MAC只研究厂商生成的唯一性，UUID仍研究复制生命周期；不因手册有手动指定能力扩成任意标识编辑器。新适配模拟须验证关联字段、存在/缺失、部分成功和恢复冲突，不能只重复CPU/内存往返。
3. **准备具体独立副本方案再获授权。** 明确源VM、完整副本/冷备份目标、容量、磁盘链、每项电源和差异、恢复及清理。原VM保持不写，固定副本目标路径并保全原始证据；本页没有具体执行目标，不是操作授权。
4. **分别完成产品功能实验与App验收。** 官方界面手动生成MAC或复制UUID只能验证产品流程，不能替代App后端。先独立核对变更资源/恢复范围，再做有限后端的确认、应用、重开、恢复、客户机观测和故障验收。真实操作另行确认，未验证不开放发布策略。

本轮更新M10不可执行目录的官方引用、双语约束/风险/恢复说明；模型、报告schema、`canExecute=false`、数值生产白名单不变。现有[M11模拟记录](V03-M11-SIMULATION-RESULTS.md)保留原来源，不能替代上述字段实验。

## 6. 本轮实际验证

使用现有共享SDK10.0.401/.NET10.0.12，单并发Release构建（`--no-restore -m:1 --disable-build-servers --tl:off`）0警告/0错误。实际顺序运行既有单元168/168、模拟/临时文件集成139/139，0失败/跳过；仅更新原候选引用断言，不为说明文字新增测试用例。

本轮日志在`local-data/m11-evidence-20261019/release-build.log`、`unit-tests.log`及`integration-tests.log`；独立TRX为`artifacts/test-results/20261019-m11-evidence/m11-evidence-unit.trx`、`m11-evidence-files.trx`。沿用原`dotnet test --project`、Release/`--no-build --parallel none --report-xunit-trx`命令形式，不重写旧证据。模型拒绝、计划导出、脱敏及既有事务保护保持通过。

本轮未重跑GUI、客户机、真实API、独立副本、远程CI或Release验收。新增事实说明没有改变可执行范围或实现新适配，M11整体仍为2/5。
