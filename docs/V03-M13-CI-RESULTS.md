# M13：公开源码与真实CI核对

2026-10-22更新：真实CI与确切原包宿主验收通过后，用户要求继续，现已完成[原包客户机验收](V03-M13-CI-GUEST-ACCEPTANCE.md)。M13完整4/5，Release仍未发布；下方2026-10-21的CI/宿主原始范围保持不变。

| 项目 | 实际状态 |
| --- | --- |
| 公开分支 | [codex/v03-m13-ci-20261021](https://github.com/Aleka579/VMIL/tree/codex/v03-m13-ci-20261021) |
| 源提交 | `26656db690d594bebcf2d69d273bfcd5dfc376bc`，唯一公开父676d0ec |
| 源码核对 | 161份批准文件逐项Git blob一致；相对父修改48/新增60/删除0；其他公开文件完整保留，额外变化0 |
| GitHub CI | [Actions39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060)，completed/success；job119278979163，Release零警告/错误 |
| 实际TRX | 原始下载ZIP中unit240/240、files206/206；错误、失败、超时、中止、未执行均0 |
| SDK/运行时 | 实际SDK10.0.402，NETCore/WindowsDesktop10.0.13，Management13及CodeDom13官方字节核对通过 |
| 旧公开内容 | main仍e27543f；不改旧tag/Release，不上传内部Git历史或真实VM数据 |
| 最终CI原包 | artifact12354145051，65,286,300字节、413文件/解压147,354,242字节；CRC/来源/版本/许可/逐文件清单及SHA通过，distribution=ci-artifact |
| 原包宿主验收 | Win11家庭中文版Build26100/X64、普通权限；CLI/PNG14/14及正常双语App43/43，App退出0；产品413文件及原ZIP测试后SHA完全不变 |
| 客户机/Release | 2026-10-22 Win10教育版Build18363/x64客户机28/50通过，测试后完整性/归档/清理完成，保持VM运行；Release未发布 |

源码来自[修复候选](V03-M13-CLOSURE.md)的批准覆盖层，产品代码未改变；本地240/206、资源/夹具77、原包CLI/PNG14及正常GUI43保留各自来源，不能代替上述新CI和最终原包目标验收。

本轮独立核对了新CI三个原始ZIP的大小/SHA，与服务器digest一致；不是旧本地候选或重打包结果。产品ZIP SHA256：`b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779`。

本地待审阅三资产位于`artifacts/releases/ci-v03-26656db/`：原CI ZIP逐字节副本、`.zip.sha256`及413条`.files.sha256`。产品不含开发驱动、测试平台、VM数据或PDB；个人路径/私钥/令牌特征扫描无命中。独立driver artifact12352643017仅用于测试，四入口及产品五组件实际版本/完整源提交均为0.3.0-alpha.1+26656db，不进入产品ZIP。

正常App验收使用上述CI原包及同CI驱动：两语言五页、实际MAC/UUID行和选中依据、两字段三阶段合成快照及配对历史、版本/schema、脱敏JSON/HTML、VMware分类保留和联网未验证、正常退出等43项通过。CLI默认14/显式17、脱敏、版本/HTML、已有输出不覆盖及两语言PNG/像素14项通过；本轮未查询或操作真实VM，没有凭据/电源/配置写入。

私有`CI-ARTIFACT-CHECK.json`记录服务器/下载/清单/TRX核对；`HOST-ACCEPTANCE-CHECK.json`记录宿主和测试后完整性；`ci-build-job.log`、原ZIP、初始排队及最终结果各自保留。临时签名下载地址只用于传输，控制文件已删除，不在日志/公开说明中保存。

2026-10-22已继续客户机验收并通过，未创建完整副本或执行身份写入，不将M11/M12旧往返计给本包。下一步为固定原CI字节的具体预发布；全新OS、其他Build/联网仍未验证。结果文档是CI之后补记，不添加到已批准26656db构建，也不移动旧发布。

## English

Public commit26656db passed [CI39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060): zero build warnings/errors, 240 unit and 206 simulated integration tests. The original 65,286,300-byte ZIP contains 413 files with SDK10.0.402/runtime10.0.13; its SHA256 is b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779. Source, server digests, CRC, licenses and all file hashes matched. Ordinary-user Windows11 build26100 host checks passed 14/43. On 2026-10-22, the unchanged CI bytes also passed Windows10 Education build18363 x64 guest checks 28/50, followed by final integrity verification and cleanup. M13 is4/5, with actual publication and receipts pending. The separate CI driver is excluded from the product ZIP; no VM configuration or power operations were performed by the test tools.

全部附加worktree已按用户要求保全原件后释放；迁移清单和原始验收回执仅私有保存，不随公开源码上传。
