using System.Globalization;

namespace Darp.Results.Analyzers;

internal static class RuleIdentifiers
{
    public const string UseReturnValueIdentifier = "DR0001";
    public const string SwitchExpressionMissingArmIdentifier = "DR0002";
    public const string SuppressSwitchExpressionAnalysisIdentifier = "DR0003";

    public static string GetHelpUri(string identifier)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/{0}.md",
            identifier
        );
    }
}
