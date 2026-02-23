using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli diagnose</c> – runs environment checks and reports any issues
/// found in the Windows/Docker setup required to run Ceph.
/// </summary>
public class DiagnoseCommand : Command
{
    public DiagnoseCommand() : base("diagnose", "Detect common Windows/Docker issues that affect running Ceph")
    {
        var jsonOption = new Option<bool>(
            "--json",
            description: "Output results as JSON",
            getDefaultValue: () => false);

        AddOption(jsonOption);

        this.SetHandler(Handle, jsonOption);
    }

    private static void Handle(bool json)
    {
        var checker = new EnvironmentChecker();
        var results = checker.RunAll();

        if (json)
        {
            OutputJson(results);
            return;
        }

        Console.WriteLine("=== Ceph Environment Diagnostics ===");
        Console.WriteLine();

        int passed = 0;
        int failed = 0;

        foreach (var result in results)
        {
            string icon = result.Passed ? "✔" : "✖";
            string color = result.Passed ? "\x1b[32m" : "\x1b[31m";
            string reset = "\x1b[0m";

            Console.WriteLine($"  {color}{icon}{reset}  {result.Name}");
            Console.WriteLine($"     {result.Message}");

            if (!result.Passed && result.RemediationHint is not null)
                Console.WriteLine($"     {"\x1b[33m"}Hint: {result.RemediationHint}{reset}");

            Console.WriteLine();

            if (result.Passed) passed++; else failed++;
        }

        Console.WriteLine($"Results: {passed} passed, {failed} failed");

        if (failed > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Run 'ceph-cli fix' to attempt automatic remediation of detected issues.");
        }
    }

    private static void OutputJson(IReadOnlyList<EnvironmentChecker.CheckResult> results)
    {
        // Simple manual JSON serialisation to avoid requiring a heavy dependency.
        Console.WriteLine("[");
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            string comma = i < results.Count - 1 ? "," : string.Empty;
            Console.WriteLine("  {");
            Console.WriteLine($"    \"name\": {JsonString(r.Name)},");
            Console.WriteLine($"    \"passed\": {(r.Passed ? "true" : "false")},");
            Console.WriteLine($"    \"message\": {JsonString(r.Message)},");
            Console.WriteLine($"    \"remediationHint\": {(r.RemediationHint is null ? "null" : JsonString(r.RemediationHint))}");
            Console.WriteLine("  }" + comma);
        }
        Console.WriteLine("]");
    }

    private static string JsonString(string value)
        => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
}
