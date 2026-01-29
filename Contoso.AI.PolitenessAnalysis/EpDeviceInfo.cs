using Microsoft.ML.OnnxRuntime;

namespace Contoso.AI.PolitenessAnalysis;

public class EpDeviceInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public OrtEpDevice Device { get; init; } = null!;
}
