# v0.3.0-alpha.1 公开预发布回执

2026-10-23已按用户明确授权公开[预发布](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1)。服务器发布时间为2026-10-23T02:57:26Z，即Asia/Taipei 2026-10-23 10:57:26。M13完整5/5；实际支持范围和未验证环境保持原声明。

## 来源与执行

| 项目 | 已核对结果 |
| --- | --- |
| 仓库 / Release ID | Aleka579/VMIL / 421405067 |
| 发布状态 | draft=false，prerelease=true，published_at非空；以make_latest=false发布 |
| tag及产品来源 | v0.3.0-alpha.1 → commit `26656db690d594bebcf2d69d273bfcd5dfc376bc` |
| 原CI / 产品artifact | [39616530060](https://github.com/Aleka579/VMIL/actions/runs/39616530060) / 12354145051，success且未过期 |
| 发布任务 | [39892345315](https://github.com/Aleka579/VMIL/actions/runs/39892345315)，job120214791403及发布步骤success |
| 公开发布支持文件 | 提交[ff0fe3b](https://github.com/Aleka579/VMIL/commit/ff0fe3bf9be7777990e611be1f3651a7ed32325e)，只以26656db为父，14项审阅变化、0删除，生产/测试blob不变 |
| 独立核对 | tag精确目标、公开状态、三资产名称/数量/大小/服务器digest、发布正文与冻结说明均一致 |
| 保留内容 | main仍e27543f；旧v0.2 Release ID及三资产ID/名称/大小/digest均未变，不上传内部历史或真实VM数据 |

发布器先下载并校验指定原CI ZIP、BUILD-INFO、SDK10.0.402/两运行时10.0.13及413文件摘要；创建草稿、逐项上传并校验服务器结果后才公开。产品没有重新构建或重压缩，也未采用发布分支新CI生成的包。发布说明与三资产固定；本回执随后作为文档补记，不回写产品tag或包。

## 三份公开资产

| 下载 | 资产ID | 字节 | SHA256 |
| --- | ---: | ---: | --- |
| [Windows x64 原CI ZIP](https://github.com/Aleka579/VMIL/releases/download/v0.3.0-alpha.1/VMIdentityLab-0.3.0-alpha.1-win-x64.zip) | 667784419 | 65286300 | `b607f1cb61b16c19e2fd788a5dfa3f45f0ef74ca3117bc8d170e9072e73ef779` |
| [ZIP摘要](https://github.com/Aleka579/VMIL/releases/download/v0.3.0-alpha.1/VMIdentityLab-0.3.0-alpha.1-win-x64.zip.sha256) | 667784491 | 107 | `857c29b1d977348a503199a9f625fed347c9f21b6b1bb70fb7c9840cd7cc728b` |
| [413文件摘要清单](https://github.com/Aleka579/VMIL/releases/download/v0.3.0-alpha.1/VMIdentityLab-0.3.0-alpha.1-win-x64.files.sha256) | 667784504 | 41128 | `f1d82dc4257605a1f766f0e66d021de3b56ad7f998472929a5588411c6677de4` |

上述服务器大小/digest与已验收本地三资产一致。清单沿用原ZIP条目顺序和CRLF，不修改既有摘要；独立测试驱动和真实证据不随产品发布。

## 验收范围

- 原产品CI：240单元/206模拟集成通过，Release零警告/错误。
- 原CI字节包：Win11 Build26100普通权限宿主CLI/PNG14及双语正常App43通过；[Win10教育版Build18363/x64客户机CLI/PNG28及正常App50](V03-M13-CI-GUEST-ACCEPTANCE.md)通过，测试后413文件/原ZIP未变。
- 客户机原失败、首个App未知退出码及后续独立退出探针0分别保留；测试目录与凭据已清理。发布不重复VM测试，也没有VM配置、电源、快照或副本操作。
- [身份写入政策](VMWARE-CONFIG-POLICY.md)继续仅限已实测精确版本、已有有效MAC/UUID、隔离单静态网卡及单字段事务；M10计划不可执行。修改两标识不移除其他VMware证据。
- 全新OS、其他Build/VMware版本/拓扑及网络连通性未验证，不因公开发布扩大支持声明。

MIT / Copyright (c) 2026 Daniel Waltz。安全问题通过[私密报告渠道](https://github.com/Aleka579/VMIL/security/advisories/new)提交，由Daniel Waltz维护。

## English

Published [v0.3.0-alpha.1](https://github.com/Aleka579/VMIL/releases/tag/v0.3.0-alpha.1) as a prerelease at 2026-10-23T02:57:26Z. Release421405067 and [publication run39892345315](https://github.com/Aleka579/VMIL/actions/runs/39892345315) succeeded. The tag resolves exactly to26656db; all three uploaded asset names, sizes and server digests match the reviewed assets. The original CI ZIP was reused without rebuilding or recompression. Main and the previous release assets remain unchanged.

M13 is5/5 within the declared scope: original CI240/206, host14/43 and exact-byte guest28/50 acceptance passed. The publication-support commit and subsequent documentation are separate from the product source. Existing failure evidence and compatibility limits remain recorded; publication adds no new VM, power or configuration tests.
