using System.Net;
using System.Text;

namespace VMIdentityLab.Core.Reports;

public static class HtmlReportRenderer
{
    public static string Render(DiagnosticReport report)
    {
        var safe = ReportJson.Deserialize(ReportJson.Serialize(report));
        static string Encode(string? text) => WebUtility.HtmlEncode(text ?? "");
        var html = new StringBuilder("<!doctype html><html lang=\"zh-CN\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>VM Identity Lab report</title><style>body{font:15px system-ui,sans-serif;max-width:1100px;margin:40px auto;padding:0 24px;color:#172333;background:#f6f8fc}table{width:100%;border-collapse:collapse;background:white}th,td{text-align:left;padding:12px;border-bottom:1px solid #dce2ed;overflow-wrap:anywhere}h1{color:#12345b}.meta{color:#52647d}</style><body><h1>VM Identity Lab</h1>");
        html.Append("<p class=\"meta\">").Append(Encode(safe.CollectedAt.ToString("O")))
            .Append(" · schema ").Append(Encode(safe.SchemaVersion)).Append(" · ")
            .Append(Encode(safe.AppVersion)).Append(" · redacted</p><p>")
            .Append(Encode(safe.System.Os)).Append(" · ").Append(Encode(safe.System.Build))
            .Append(" · ").Append(Encode(safe.System.Architecture)).Append("</p><p>")
            .Append(Encode(safe.VirtualizationEvidence.Explanation)).Append("</p><table><thead><tr><th>项目</th><th>状态</th><th>值</th><th>来源</th></tr></thead><tbody>");
        foreach (var item in safe.Findings)
            html.Append("<tr><td>").Append(Encode(FindingCatalog.Title(item.Id))).Append("</td><td>")
                .Append(Encode(item.Status.ToString())).Append("</td><td>").Append(Encode(item.Value.ToString()))
                .Append("</td><td>").Append(Encode(item.Source)).Append("</td></tr>");
        html.Append("</tbody></table>");
        if (safe.VmxSummary is { } vmx)
        {
            html.Append("<h2>VMX 配置声明</h2><p>Power state: ").Append(Encode(vmx.PowerState))
                .Append("</p><table><tbody>");
            foreach (var pair in vmx.Declarations)
                html.Append("<tr><td>").Append(Encode(pair.Key)).Append("</td><td>").Append(Encode(pair.Value)).Append("</td></tr>");
            html.Append("</tbody></table>");
        }
        html.Append("<h2>身份分析</h2><p>配置声明与系统观测未确认来自同一虚拟机。修改支持仅描述项目能力，不能当作当前目标的写入许可。敏感值脱敏后不可比较。</p>")
            .Append("<table><thead><tr><th>项目</th><th>VMX 声明</th><th>系统观测</th><th>状态</th><th>修改支持及依据</th></tr></thead><tbody>");
        foreach (var row in IdentityAnalyzer.Analyze(safe))
        {
            var declaration = row.Declaration.State switch
            {
                DeclarationState.Present => row.Declaration.Value.ToString(),
                DeclarationState.Redacted => "存在声明（已脱敏）",
                DeclarationState.NoSnapshot => "未提供配置",
                DeclarationState.NotIncluded => "摘要未包含声明",
                DeclarationState.Ambiguous => "重复声明，存在歧义",
                DeclarationState.Invalid => "声明无法确认",
                _ => "无对应配置映射"
            };
            var support = row.Basis switch
            {
                IdentityBasis.V02ExactRange => "既有精确范围策略：17.5.2.23775571/HW19/每插槽2核，4096↔6144 MiB、2↔4 vCPU；仍须通过原有检查。",
                IdentityBasis.MacHistoricalKb => "研究候选，未批准写入：KB324973，旧版 hosted 产品资料；目标版本与恢复未验证。",
                IdentityBasis.UuidWorkflowKb => "研究候选，未批准写入：KB320246，复制/迁移流程；产品/版本范围混合，非任意 UUID setter。",
                _ => "当前不支持修改：M08 尚无逐字段目标版本官方修改与恢复依据。"
            };
            html.Append("<tr><td>").Append(Encode(FindingCatalog.Title(row.Definition.Id))).Append("</td><td>")
                .Append(Encode(declaration)).Append("</td><td>")
                .Append(Encode(row.Observation?.Value.ToString() ?? "未采集")).Append("</td><td>")
                .Append(Encode(row.Observation?.Status.ToString() ?? "未采集")).Append("</td><td>")
                .Append(Encode(support)).Append("</td></tr>");
        }
        html.Append("</tbody></table>");
        foreach (var diagnostic in safe.Diagnostics)
            html.Append("<p>").Append(Encode(diagnostic.Code)).Append(": ").Append(Encode(diagnostic.Message)).Append("</p>");
        return html.Append("</body></html>").ToString();
    }
}
