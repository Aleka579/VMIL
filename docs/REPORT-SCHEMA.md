# 报告格式与隐私约定

## v0.3格式分工

默认诊断JSON继续schemaVersion=1.0；appVersion由真实程序集填写，旧诊断可导入。M10计划为独立`vmil.identity-plan/1`且固定未执行，不可作为诊断/写入指令。M12私有输入为`vmil.private-identity-snapshot/1`，包含会话、阶段、采集时间/来源/状态、系统范围、可选接口GUID及原始MAC/UUID；仅明确选择后保存到私有新文件，不公开提交。

三阶段结论为`vmil.identity-validation/1`，由IdentityValidationResult独立定义，导出JSON/HTML保持脱敏；配置、Windows观测和VMIL分类分开。原值仅用于私有内存比较，不嵌入结论/默认诊断。配对恢复历史schema3含私有目标/备份路径及前后标识，保留schema1/2兼容读取，但没有身份前后值的旧记录不能证明身份恢复。会话/系统/接口范围不同、多地址、缺失或失败采集、旧脱敏标记均明确不可比较；替换输入失败清除旧结论。详见[使用指南](USER-GUIDE.md)及[M12证据](V03-M12-REAL-ACCEPTANCE.md)。

以下1.0约定保持，不能把M12显式私有采集误称默认脱敏报告。

状态：v0.1格式1.0已通过合成测试及真实宿主/客户机采集、界面和导出；本轮可选vmrest来源保持既有格式，见[本轮结果](READINESS-TEST-RESULTS-2026-10-11.md)。

2026-10-18 M09继续沿用1.0，新增可选标识采集和推导的身份分析视图，没有新增顶层字段或状态枚举。[M09结果](V03-M09-RESULTS.md)限定本机功能验证，不扩展旧客户机/发布证据。

## 数据结构

| 字段 | 含义 | 规则 |
| --- | --- | --- |
| schemaVersion | 报告格式版本 | 导入时校验；不支持版本明确提示 |
| appVersion | 生成应用版本 | 实际应用构建后填写，不能用计划版本冒充 |
| collectedAt | 采集时间 | ISO 8601，含时区偏移 |
| privacyMode | 隐私模式 | v0.1 默认 redacted |
| system | OS、Build、架构等可公开摘要 | 不含机器名、用户名或序列号 |
| findings | 五类采集项 | 每项有 id、category、source、observedAt、status、类型化 value、sensitivity、explanation |
| virtualizationEvidence | 证据解释 | 分开呈现 Hypervisor、VMware 客体和软件痕迹 |
| vmxSummary | 可选配置摘要 | 与客体观测区分；不导出整份原始 VMX |
| diagnostics | 脱敏失败/限制摘要 | 不含完整原始异常、命令凭据或路径 |

采集状态：success、accessDenied、notSupported、timeout、failed。success 仅表示接口执行成功，虚拟化判定仍依证据解释。value 的类型与单位由字段定义，不通过本地化字符串做数值比较。

value.kind 为 unknown、text、number、boolean、redacted；仅对应的 text/number/boolean 字段可有值。unit 为 none、count、bytes、mebibytes、gibibytes、megahertz。导入最多 5 MiB、JSON 深度 32、500 个采集项；重复 ID、矛盾值和未知结构拒绝，未审查字段不进入默认导出。

vmxSummary.powerState只接受poweredOn、poweredOff、suspended、unknown；已评审的本机官方接口来源为vmrest.power，接口不可用为unknown/unavailable。此字段是查询快照；顶层collectedAt记录系统报告采集时间，不代表之后API查询的时间。v0.1导出不含API账号、端口、VM注册ID或真实VMX路径，状态不能当作后续写入许可。

## 合成示例

以下内容仅说明格式，不代表已经执行采集或测试。

```json
{
  "schemaVersion": "1.0",
  "appVersion": "0.1.0-alpha.2",
  "collectedAt": "2026-10-09T00:00:00+08:00",
  "privacyMode": "redacted",
  "system": {
    "os": "Windows（合成示例）",
    "build": null,
    "architecture": "x64"
  },
  "findings": [
    {
      "id": "hypervisor.present",
      "category": "cpu",
      "source": "CPUID.1.ECX[31]",
      "observedAt": "2026-10-09T00:00:00+08:00",
      "status": "success",
      "value": { "kind": "boolean", "boolean": true, "unit": "none" },
      "sensitivity": "public",
      "explanation": "存在 Hypervisor 信号，不能单独断定 VMware 客户机身份。"
    }
  ],
  "virtualizationEvidence": {
    "hypervisorDetected": true,
    "vmwareGuestEvidence": "inconclusive"
  },
  "vmxSummary": null,
  "diagnostics": []
}
```

## 脱敏策略

- 公开摘要保留 OS/Build、CPU 功能与类型、内存容量、设备类别/厂商/型号、Tools 版本和采集状态等诊断字段。
- 用户名、机器名、路径、序列号、UUID、MAC、访问凭据和敏感配置默认省略或以带类型的脱敏标记替代。
- 原始敏感值不能藏在 explanation、diagnostics、异常、未知配置或日志上下文中。
- 不提供默认原始报告导出，也不创建持续遥测或远程上传。
- JSON 导入大小、深度与结构限制在实现时确定并测试；错误信息同样脱敏。

## 对比

### M09 可选标识

默认采集14项，未选择时不调用三个标识接口，报告中没有这些结果。GUI勾选或CLI `--include-identifiers` 后，增加 `smbios.uuid`、`smbios.serialNumber`、`network.macAddress`；接口来源分别为 `Win32_ComputerSystemProduct.UUID`、`Win32_BIOS.SerialNumber`、`NetworkInterface.GetAllNetworkInterfaces/GetPhysicalAddress`。

上述三项始终以 `sensitivity=identifier` 和 `value.kind=redacted` 返回，仍保留真实采集状态及时间；失败不因脱敏而变成成功。旧报告通用 `source=identifier` 保留原样，未审查来源为 `unverified`，不重标成新的接口。原值不显示、不保存到报告/日志，不提供原值比较；不承诺托管内存安全擦除。

身份分析界面的“未采集”来自缺失的已知字段，不是新的 `CollectionStatus`；也不证明标识不存在。VMX敏感声明的存在性只用于用户显式打开的文件视图，公开摘要仍不导出这些配置原值，摘要缺失不代表原文件没有声明。

### 比较规则

只对比两份报告都可用、类型一致、单位已统一的字段。success 与 timeout 等状态差异单独展示；缺失、未验证和脱敏标记不解释成实际硬件变化。

分别显示配置声明与实际采集值。报告格式不兼容时清楚提示，不静默忽略关键字段后产生健康结论。

## HTML

使用静态 HTML；来源字符串经过正确编码后输出，不执行脚本或加载远程资源。恶意字符串、特殊字符、路径脱敏和导出失败属于必测场景。

## 证据保存

真实报告放在用户选择的私有位置或 local-data/ 中，不进入仓库。提交样例必须由合成数据生成并经过审查。

