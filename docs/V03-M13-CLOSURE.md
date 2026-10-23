# M13：复审问题修复与交付补齐

2026-10-22后续：[真实CI/宿主](V03-M13-CI-RESULTS.md)及[最终原包客户机](V03-M13-CI-GUEST-ACCEPTANCE.md)验收均通过，M13当前4/5；仅剩具体预发布及服务器回执。附加worktree已保全原件后释放，以下保留原修复阶段和本地候选来源，不用本页旧包代替最终CI包。

## 修复范围

- VMX复核：原逻辑取一行最后一个引号后的文本，漏掉注释自身带引号的新增、修改和删除。改由既有VMX解析器的同一条语法提取注释，不新增第二套解析。官方API允许的换行/键顺序规范化仍可通过；其他字段或注释变化保留未决记录、备份和提案并阻止重放。
- 身份能力：MAC/BIOS UUID改为已有有限策略，明确Workstation 17手册238/160–161页、精确17.5.2.23775571及vmrest1.3.1.23775571、HW19、单静态hostonly且启动断开、已有字段单键应用/恢复与客户机核对。双语界面和HTML同步，不把项目能力显示当作当前目标写入授权。M10生成/复制、其他版本/拓扑/缺失键和不支持标识仍禁用。
- 计划状态：当前开发/候选版本明确为0.3.0-alpha.1，M13已开始；明确日期的原M08–M12历史记录不改写。

## 已实际执行

命令在当前worktree执行，复用主项目既有SDK10.0.401和NuGet缓存，不安装系统依赖。构建/测试使用项目`global.json`及Release配置，默认串行。

| 检查 | 结果 | 私有证据入口 |
| --- | --- | --- |
| 修复前模拟事务回归 | 206项中203通过、3失败；带引号注释新增/修改/删除均复现未被拒绝。未变的带引号/空引号/未配对引号注释通过 | `artifacts/test-results/m13-closure-20261021/red-all/quoted-comments-red.trx` |
| 修复后Release构建 | 0警告、0错误 | 原生`dotnet build VMIdentityLab.sln -c Release --no-restore -m:1 --disable-build-servers --tl:off` |
| 单元测试 | 240/240，0失败/跳过；覆盖当前能力依据、未采集状态、HTML同步及既有不支持范围 | `artifacts/test-results/m13-closure-20261021/green/closure-unit.trx` |
| 模拟集成测试 | 206/206，0失败/跳过；新增3个注释变化均保留pending/备份/提案、PUT仅1次且拒绝重放；3种未变注释正常完成 | `artifacts/test-results/m13-closure-20261021/green/closure-integration.trx` |
| 双语资源/三阶段夹具 | 77/77；包含全部身份支持/依据枚举到两语言资源的映射，无VMware连接 | `local-data/m13-closure-20261021/validation-fixtures/fixture-check.json` |
| 新干净候选 | 来源bdec9107b6f457a1736402eb36af1d8096fb74a6，五组件版本/提交一致，SDK10.0.401、自包含两运行时10.0.13 | `artifacts/releases/local-v03-bdec910/` |
| ZIP内容与完整性 | 413文件，65,516,830字节，解压147,353,762字节；CRC/清单/逐文件SHA/许可/内容通过，Management13、CodeDom13与官方运行时字节一致；个人路径/令牌特征无命中 | `local-data/m13-closure-20261021/final-package-check.json` |
| 原包普通权限CLI/PNG | 14/14，默认14项/显式17项、1.0/0.3版号、JSON/HTML脱敏及拒绝覆盖、双语身份PNG/像素检查；宿主Win11 Build26100/X64 | `local-data/m13-closure-20261021/host-smoke-final/smoke-check.json` |
| 原包正常App | 43/43，两语言五页、MAC/UUID实际行及选中后的有限依据、两字段三阶段合成输入/配对记录、脱敏导出、保留VMware分类、联网未验证和正常退出0；自有测试App已退出 | `local-data/m13-closure-20261021/final-gui-ready/gui-check.json` |

测试命令沿用`dotnet test --project <测试项目.csproj> -c Release --no-build --parallel none --report-xunit-trx`，结果目录如表。最初尝试的方法筛选没有匹配测试（退出5），不算通过；随后实际运行整套模拟集成完成红/绿验证，原失败保留。

本地ZIP SHA256：`247077d10c610210b7283a159f07ed14a35723ee1a7e95add6782e0ea93da928`；同目录含ZIP摘要及413条逐文件摘要。这是本地候选，不是CI或Release资产，旧包不覆盖。

人工查看英文PNG时仍遇到M09已记载的查看链路缺字显示差异；新旧英文PNG字节一致，SHA256均为`d63f17d1680b71d40588bda5486279b0fe635db46f44186946ce71a9b77703cf`。独立原生解码为1220×800、alpha255，三个观测区域文字像素65/181/36；不据查看工具显示差异猜测新渲染缺陷或改宿主设置。正常App实际两语言行及依据另有43项验收；原查看链路根因仍未确认。

## 待完成

1. 以实际GitHub公开HEAD为基线准备审阅差异，排除本机日志/内部历史/真实证据；具体确认后上传并运行CI。2026-10-21只读核实main仍为e27543f、M09分支676d0ec；公开Release只有v0.2.0-alpha.1。
2. 对确切新CI原ZIP在声明Windows目标验收，不继承旧包结果；准备精确来源/摘要/说明的Release，再按具体授权发布与回读服务器资产。

本轮本地模拟不操作真实VM或电源、不发送身份PUT，不扩大原M11准入。配置备份仍不包含磁盘、NVRAM或客户机状态。
