# Alpha ZIP 体积分析

分析日期：2026-10-10，Asia/Taipei。输入为既有 VMIdentityLab-0.1.0-alpha.1-win-x64.zip；基线源码 3ebc381。范围仅为文件体积/来源/误打包审查与后续记录，没有改代码、切换框架、重新构建或重新打包。

## 结论

解压后 **140.28 MiB** 中，**99.34% 来自 .NET 10.0.12 / WPF 自包含运行时**。应用启动器、四个程序集及依赖元数据仅 **0.39 MiB**。在本次 413 个文件的清单/发布依赖/官方资产对照中，**未发现明显误打包的 SDK、测试、调试或 VM 数据文件**。本轮未重新进行内容级隐私审计。

可确认的轻微冗余是三份字节相同的第三方声明，多出 156,082 字节（0.15 MiB）；框架语言资源有后续筛选空间。它们不解释整个 140 MiB，本轮保留原包。

## 测量口径与输入

| 项目 | 实测 |
| --- | --- |
| ZIP 实际文件长度 | 65,383,430 字节 = 62.35 MiB = 65.38 MB |
| ZIP 条目压缩长度之和 | 65,325,792 字节；ZIP 元数据开销 57,638 字节 |
| 解压文件逻辑长度之和 | 147,089,645 字节 = 140.28 MiB = 147.09 MB |
| 文件数量 | 413 |
| 既有独立解压目录 | 同为 413 文件 / 147,089,645 字节；逐个相对文件名与长度无差异 |
| ZIP SHA256 | 2E04C1ECA0C17F51C8A0477C55A3CD40EA49940DCB4E38902B9B328A45C27FDB，与上轮记录一致 |

MiB = 1,048,576 字节，MB = 1,000,000 字节。本报告统计文件逻辑大小，不含 NTFS 分配单元/目录元数据，也不把磁盘体积解释成运行时内存占用。

## 主要组成

| 分类 | 文件数 | 字节 | MiB | 占比 |
| --- | ---: | ---: | ---: | ---: |
| .NET 核心运行时 | 186 | 80,340,528 | 76.62 | 54.62% |
| WPF / WindowsDesktop 运行时（含卫星资源） | 209 | 65,782,360 | 62.73 | 44.72% |
| 应用启动器、四个程序集和元数据 | 7 | 407,279 | 0.39 | 0.28% |
| System.Management（WMI） | 1 | 314,152 | 0.30 | 0.21% |
| README、LICENSE、依赖声明 | 10 | 245,326 | 0.23 | 0.17% |
| 总计 | 413 | 147,089,645 | 140.28 | 100% |

运行时来源按官方 NuGet 资产的相对文件名与长度对应；这不是对全部文件逐字节真实性认证。System.Management.dll 的 314,152 字节对应本机官方包 runtimes/win/lib/net10.0/，项目确实使用 WMI。应用五个二进制本体合计 368,640 字节，另有 38,639 字节 deps/runtimeconfig 元数据。

## 最大的 15 个文件

| 文件 | MiB | 来源 |
| --- | ---: | --- |
| System.Private.CoreLib.dll | 15.29 | .NET 核心运行时 |
| PresentationFramework.dll | 15.08 | WPF 运行时 |
| PresentationCore.dll | 7.94 | WPF 运行时 |
| System.Private.Xml.dll | 7.43 | .NET 核心运行时 |
| D3DCompiler_47_cor3.dll | 4.52 | WPF 运行时 |
| coreclr.dll | 4.40 | .NET 核心运行时 |
| System.Linq.Expressions.dll | 3.47 | .NET 核心运行时 |
| System.Data.Common.dll | 2.64 | .NET 核心运行时 |
| System.Security.Cryptography.dll | 2.43 | .NET 核心运行时 |
| Microsoft.DiaSymReader.Native.amd64.dll | 2.27 | .NET 核心运行时 |
| WindowsBase.dll | 2.08 | WPF 运行时 |
| clrjit.dll | 2.00 | .NET 核心运行时 |
| System.Private.DataContractSerialization.dll | 1.97 | .NET 核心运行时 |
| wpfgfx_cor3.dll | 1.87 | WPF 运行时 |
| System.Private.Windows.Core.dll | 1.82 | WPF 运行时 |

这些大文件均对应微软运行时包。XML、LINQ、加密、HTTP、数据契约等通用库由当前自包含运行时分发携带，不能因应用没有直接调用就判为误打包。Microsoft.DiaSymReader.Native.amd64.dll 是核对到的官方原生运行时资产，不能将其文件名当作开发 PDB 混入。

## 多语言资源

13 个框架语言目录、156 个 resources.dll，共 8,688,648 字节 = **8.29 MiB**，全部对应 WindowsDesktop 运行时资产。应用自己的中英文 XAML 资源编译在应用程序集内，与这些框架卫星资源不同。

| 目录 | 文件数 | 字节 | MiB |
| --- | ---: | ---: | ---: |
| cs | 12 | 644,696 | 0.61 |
| de | 12 | 677,424 | 0.65 |
| es | 12 | 657,376 | 0.63 |
| fr | 12 | 668,208 | 0.64 |
| it | 12 | 663,288 | 0.63 |
| ja | 12 | 717,992 | 0.68 |
| ko | 12 | 677,112 | 0.65 |
| pl | 12 | 672,424 | 0.64 |
| pt-BR | 12 | 654,464 | 0.62 |
| ru | 12 | 798,216 | 0.76 |
| tr | 12 | 641,072 | 0.61 |
| zh-Hans | 12 | 606,968 | 0.58 |
| zh-Hant | 12 | 609,408 | 0.58 |

如果将来只保留框架简体中文 zh-Hans 与默认英文回退，其他目录的当前文件量上界为 8,081,680 字节 = **7.71 MiB（5.49%）**。这是清单上界，不是已经验证的发布削减：系统 UI 文化、回退、异常提示与控件仍需测试；当前不删除。

## 误打包与轻微冗余核查

| 核查项 | 实际发现 | 判断 |
| --- | --- | --- |
| SDK / Roslyn / MSBuild / NuGet 缓存 | 没有这些文件/目录 | 未发现开发工具混入 |
| xUnit / Testing.Platform / ApplicationInsights | 清单无对应文件 | 未发现测试或测试遥测依赖混入 |
| PDB、XML API 文档、日志、TRX、源代码与仓库文件 | 清单无对应文件 | 未发现构建/调试残留；nuspec 是单列许可来源资料 |
| VMware 程序、真实 VMX/磁盘/快照、ISO | 清单无对应文件 | 未发现 VM 资料混入 |
| WindowsForms 专属运行时 | 官方 RuntimeList.xml 的 WindowsForms-only 资产未从 Desktop 包进入当前 ZIP | 没有因 WindowsDesktop 包同时提供 WPF/Forms 就混入完整 WinForms |
| Microsoft.VisualBasic.dll / System.Drawing.dll | 分别 17,192 / 20,304 字节，对应 NETCore 兼容资产 | 同名不代表 WindowsForms 专属二进制混入 |
| System.CodeDom.dll | 476,968 字节，对应 WindowsDesktop；WMI 的 NuGet 声明亦有此依赖 | 不是可直接删除的孤立文件 |
| 三份 THIRD-PARTY-NOTICES | 各 78,041 字节，SHA256 均为 6D15E10A101C6BFFF2AB4429ED061BF76C456FC4B23AD6B03E0D0F8377148A21 | 确认重复；可将来合并索引，但许可全文必须保留 |
| 两个 nuspec | 合计 3,153 字节，位于 third-party/，记录 MIT/版权与来源 | 分发来源资料，不是 NuGet 缓存；收益极小 |

本轮不删文件、不重写许可证、不做运行时裁剪。其他优化见 [后续清单](PACKAGE-OPTIMIZATION-BACKLOG.md)。

## 依据与复查入口

本报告及 CSV 的 413 文件/140.28 MiB 数字保留为去重前基线。2026-10-10 用户追加授权后已去除两个重复副本并明确共享引用：当前 ZIP 为 411 文件、146,933,880 字节（约 140.13 MiB），ZIP 文件 65,356,142 字节；SHA256 407710650948DD50BCEB59E4F42146D82D9AFF1A551B46A8291C6D5FAFCFBAF8。原 ZIP 保存在 releases/archive/。共同声明全文和其余 410 个条目哈希未变，只有许可索引文字更新；其他优化未执行。

- ZIP 与独立解压目录：原 checkout 的 artifacts/releases/ 与 artifacts/package-smoke/win-x64/。
- 全文件相对名、长度、压缩长度及分类：[清单](package-size-inventory.csv)。
- 发布配置：App.csproj 的 UseWPF，CI 的 --self-contained true / win-x64；runtimeconfig includedFrameworks 为 NETCore/WindowsDesktop 10.0.12。
- 发布 deps 包声明：四个项目、NETCore/WindowsDesktop runtimepack、System.Management。
- 官方本地资产：microsoft.netcore.app.runtime.win-x64/10.0.12/runtimes/、microsoft.windowsdesktop.app.runtime.win-x64/10.0.12/runtimes/ 和 data/RuntimeList.xml；上述来源无需运行 DLL。
- 操作和范围记录：本 worktree 唯一 WORK_LOG.md。未执行应用、构建、VM 或受限计划校验。
