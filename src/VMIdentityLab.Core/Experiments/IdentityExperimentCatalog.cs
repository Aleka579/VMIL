using System.Collections.Frozen;

namespace VMIdentityLab.Core.Experiments;

public enum IdentityExperimentKind { MacUniqueness, UuidOnCopy }
public enum IdentityExperimentGap
{
    TargetVersionUnknown, OutsideResearchBaseline, TargetVersionEvidenceMissing,
    ValueConstraintsUnverified, OfficialChannelUnverified, RecoveryUnverified,
    IndependentCopyTestMissing, WriteNotApproved, PowerOffUnconfirmed,
    AmbiguousConfiguration, UnsupportedConfiguration, MissingDeclaration,
    InvalidDeclaration, AdapterScopeUnconfirmed
}

public sealed record ExperimentText(string Chinese, string English)
{
    public string Get(bool english) => english ? English : Chinese;
}

public sealed record IdentityExperimentDefinition(IdentityExperimentKind Kind, string FieldId,
    ExperimentText Title, ExperimentText Intent, ExperimentText ValueConstraints,
    ExperimentText Risk, ExperimentText Recovery, ExperimentText EvidenceScope, Uri OfficialReference);

// A closed research catalog. These definitions are never write-policy entries or executable presets.
public static class IdentityExperimentCatalog
{
    public static Version ResearchBaseline { get; } = new(17, 5, 2, 23775571);
    public static DateOnly EvidenceReviewedOn { get; } = new(2026, 10, 19);
    private const string Manual = "https://techdocs2-prod.adobecqms.net/content/dam/broadcom/techdocs/us/en/pdf/vmware/desktop-hypervisors/workstation/vmware-workstation-pro-17-0.pdf";

    private static readonly FrozenDictionary<IdentityExperimentKind, IdentityExperimentDefinition> Definitions = new[]
    {
        new IdentityExperimentDefinition(IdentityExperimentKind.MacUniqueness, "network.macAddress",
            new("MAC 唯一性管理", "MAC uniqueness management"),
            new("为独立副本规划唯一地址；尚未分配地址", "Plan a unique address for an independent copy; no address allocated"),
            new("17系列手册记载厂商地址格式及界面生成流程。本候选只规划厂商生成且网络内唯一的地址，不接受自定义或物理机地址；目标构建仍待验证。",
                "The 17-series manual documents vendor address constraints and GUI generation. This candidate plans a vendor-generated, network-unique address only; no custom or physical-machine input. Target-build behavior remains unverified."),
            new("可能造成地址冲突、DHCP 租约及客户机网络配置变化；配置声明不等于有效网卡地址。",
                "Possible address conflicts, DHCP lease and guest network changes. A declaration does not identify the effective NIC address."),
            new("须保留地址来源、生成字段和UUID关联，核实完整关机基线及外置资源；独立验证原地址和连通性恢复，不承诺恢复外部DHCP状态。恢复闭环未验证。",
                "Preserve address origin, generated fields and UUID relationships; account for a powered-off baseline and external VM resources. Verify original address and connectivity independently. External DHCP rollback is not promised; recovery remains untested."),
            new("Workstation Pro 17手册第237-238页已核实，记载界面及静态设置。该汇编包含17.6.4，不能仅凭URL认定精确17.5.2构建、自动写入通道或恢复已获支持。",
                "Workstation Pro 17 manual pp.237-238 documents GUI and static configuration. The compilation includes 17.6.4; its URL does not establish exact 17.5.2-build automation or recovery support."),
            new(Manual + "#page=238")),
        new IdentityExperimentDefinition(IdentityExperimentKind.UuidOnCopy, "smbios.uuid",
            new("UUID 复制生命周期", "UUID lifecycle on copy"),
            new("规划复制时由厂商流程生成新 UUID；尚未生成 UUID", "Plan vendor-generated UUID on copy; no UUID generated"),
            new("17系列手册记载复制生成、保留及手动指定UUID；本候选仍仅规划复制流程，不接收任意UUID。精确构建、自动化及关联状态待验证。",
                "The 17-series manual documents copy generation, retention and manual UUID assignment. This candidate remains limited to copy lifecycle planning, with no arbitrary UUID input. Exact-build automation and related state remain unverified."),
            new("复制生成身份会联动MAC，可能改变客户机网络和依赖身份的配置；不以第三方授权或检测结论变化作为验收。",
                "Copy-generated identity also changes MAC and may affect guest networking or dependent settings. Third-party licensing or detector outcomes are not acceptance criteria."),
            new("须核实UUID、生成策略及MAC的完整恢复范围，保留稳定路径和包含外置资源的关机基线；VMX备份不能保证客户机或网络状态恢复，往返仍未验证。",
                "Establish recovery for UUID, generation policy and MAC, preserving stable paths and a powered-off baseline including external resources. VMX backup does not guarantee guest or network recovery; round-trip testing is still missing."),
            new("Workstation Pro 17手册第160-161页已核实复制生命周期和手动指定。该系列资料不等于本机17.5.2逐字段API及恢复已验证，生产写入仍拒绝。",
                "Workstation Pro 17 manual pp.160-161 documents copy lifecycle and manual assignment. Series documentation does not verify field-specific API or recovery on installed 17.5.2; production writes remain denied."),
            new(Manual + "#page=160"))
    }.ToFrozenDictionary(item => item.Kind);

    public static IdentityExperimentDefinition Get(IdentityExperimentKind kind) =>
        Definitions.TryGetValue(kind, out var definition) ? definition
            : throw new ArgumentException("This experiment is not registered.", nameof(kind));

    public static ExperimentText Describe(IdentityExperimentGap gap) => gap switch
    {
        IdentityExperimentGap.TargetVersionUnknown => new("未识别目标 Workstation 完整版本。", "Full target Workstation version is unknown."),
        IdentityExperimentGap.OutsideResearchBaseline => new("目标版本与研究基线不一致。", "Target version differs from the research baseline."),
        IdentityExperimentGap.TargetVersionEvidenceMissing => new("缺少逐字段、适用于目标构建的官方依据。", "Field-specific official evidence for the target build is missing."),
        IdentityExperimentGap.ValueConstraintsUnverified => new("目标版本的允许值及关联约束未验证。", "Target-version values and related constraints are unverified."),
        IdentityExperimentGap.OfficialChannelUnverified => new("逐字段官方自动写入通道及状态/并发语义未验证；界面或手动配置资料不能替代API依据。", "Field-specific official automation and state/concurrency behavior are unverified; GUI or manual configuration documentation is not API evidence."),
        IdentityExperimentGap.RecoveryUnverified => new("恢复范围和恢复后结果未验证。", "Recovery scope and recovered state are unverified."),
        IdentityExperimentGap.IndependentCopyTestMissing => new("缺少获授权独立副本的往返测试。", "Authorized independent-copy round-trip testing is missing."),
        IdentityExperimentGap.WriteNotApproved => new("本项未进入生产写入白名单；审阅计划不授予写入权限。", "This item is not in the production write policy; reviewing a plan grants no write permission."),
        IdentityExperimentGap.PowerOffUnconfirmed => new("未可靠确认完全关机；状态仅代表已提供的观测。", "Powered-off state is not reliably confirmed; state reflects supplied observations only."),
        IdentityExperimentGap.AmbiguousConfiguration => new("配置存在重复键或语法歧义，不能据此形成写入请求。", "Duplicate keys or ambiguous syntax prevent a write request."),
        IdentityExperimentGap.UnsupportedConfiguration => new("配置存在不支持的编码/语法或加密内容。", "Configuration contains unsupported encoding/syntax or encryption."),
        IdentityExperimentGap.MissingDeclaration => new("配置未包含该标识声明；不推断默认值。", "Identity declaration is absent; defaults are not inferred."),
        IdentityExperimentGap.InvalidDeclaration => new("标识声明未通过基本语法检查；不展示原值。", "Identity declaration failed basic syntax checks; raw values are omitted."),
        IdentityExperimentGap.AdapterScopeUnconfirmed => new("网卡或地址声明不唯一，需先明确有效适配器及地址来源。", "NIC/address declarations are not unique; effective adapter and address source need review."),
        _ => throw new ArgumentException("Unknown experiment gap.", nameof(gap))
    };
}
