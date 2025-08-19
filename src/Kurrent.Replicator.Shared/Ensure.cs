using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Replicator.Shared; 

public static class Ensure {
    public static string NotEmpty(string? value, string parameter)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentNullException(parameter)
            : value;
    public static bool NotNullOrEmpty(string? value, [NotNullWhen(true)] out string outParameter) {
        if (string.IsNullOrWhiteSpace(value)) {
            outParameter = null!;
            return false;
        }
        outParameter = value;
        return true;
    }
}