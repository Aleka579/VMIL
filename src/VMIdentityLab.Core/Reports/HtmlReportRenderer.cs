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
        foreach (var diagnostic in safe.Diagnostics)
            html.Append("<p>").Append(Encode(diagnostic.Code)).Append(": ").Append(Encode(diagnostic.Message)).Append("</p>");
        return html.Append("</body></html>").ToString();
    }
}
