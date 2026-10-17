using VMIdentityLab.Core.Reports;

namespace VMIdentityLab.Core.Comparison;

public enum ComparisonKind { Same, Different, StatusChanged, NotComparable }
public enum ComparisonReason { Equal, ValueDiffers, Missing, CollectionStatusChanged, Unavailable, Redacted, TypeMismatch, UnitMismatch, NumericOverflow }
public sealed record ComparisonRow(string Id, Finding? Left, Finding? Right, ComparisonKind Kind, ComparisonReason Reason);

public static class ReportComparer
{
    public static IReadOnlyList<ComparisonRow> Compare(DiagnosticReport leftReport, DiagnosticReport rightReport)
    {
        ReportJson.Validate(leftReport);
        ReportJson.Validate(rightReport);
        var left = leftReport.Findings.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var right = rightReport.Findings.ToDictionary(item => item.Id, StringComparer.Ordinal);
        return left.Keys.Union(right.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)
            .Select(id => CompareItem(id, left.GetValueOrDefault(id), right.GetValueOrDefault(id))).ToArray();
    }

    private static ComparisonRow CompareItem(string id, Finding? left, Finding? right)
    {
        ComparisonRow Row(ComparisonKind kind, ComparisonReason reason) => new(id, left, right, kind, reason);
        if (left is null || right is null) return Row(ComparisonKind.NotComparable, ComparisonReason.Missing);
        if (left.Status != right.Status) return Row(ComparisonKind.StatusChanged, ComparisonReason.CollectionStatusChanged);
        if (left.Status != CollectionStatus.Success || left.Value.Kind == ValueKind.Unknown || right.Value.Kind == ValueKind.Unknown)
            return Row(ComparisonKind.NotComparable, ComparisonReason.Unavailable);
        if (left.Sensitivity != DataSensitivity.Public || right.Sensitivity != DataSensitivity.Public
            || left.Value.Kind == ValueKind.Redacted || right.Value.Kind == ValueKind.Redacted
            || (FindingCatalog.ById.TryGetValue(id, out var definition) && definition.Sensitivity != DataSensitivity.Public))
            return Row(ComparisonKind.NotComparable, ComparisonReason.Redacted);
        if (left.Value.Kind != right.Value.Kind) return Row(ComparisonKind.NotComparable, ComparisonReason.TypeMismatch);

        bool same;
        switch (left.Value.Kind)
        {
            case ValueKind.Number:
                if (!TryNormalize(left.Value, out var leftNumber, out var leftDimension)
                    || !TryNormalize(right.Value, out var rightNumber, out var rightDimension))
                    return Row(ComparisonKind.NotComparable, ComparisonReason.NumericOverflow);
                if (leftDimension != rightDimension) return Row(ComparisonKind.NotComparable, ComparisonReason.UnitMismatch);
                same = leftNumber == rightNumber;
                break;
            case ValueKind.Text:
                same = string.Equals(left.Value.Text, right.Value.Text, StringComparison.Ordinal);
                break;
            case ValueKind.Boolean:
                same = left.Value.Boolean == right.Value.Boolean;
                break;
            default:
                return Row(ComparisonKind.NotComparable, ComparisonReason.Unavailable);
        }
        return Row(same ? ComparisonKind.Same : ComparisonKind.Different,
            same ? ComparisonReason.Equal : ComparisonReason.ValueDiffers);
    }

    private static bool TryNormalize(FindingValue value, out decimal number, out string dimension)
    {
        number = value.Number!.Value;
        dimension = value.Unit switch
        {
            ValueUnit.Bytes or ValueUnit.Mebibytes or ValueUnit.Gibibytes => "memory",
            ValueUnit.Count => "count",
            ValueUnit.Megahertz => "frequency",
            _ => "scalar"
        };
        try
        {
            number *= value.Unit switch { ValueUnit.Mebibytes => 1048576m, ValueUnit.Gibibytes => 1073741824m, _ => 1m };
            return true;
        }
        catch (OverflowException) { return false; }
    }
}
