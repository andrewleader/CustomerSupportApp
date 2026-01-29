namespace Contoso.AI.PolitenessAnalysis;

public class PolitenessAnalysisResponse
{
    public PolitenessLevel Level { get; init; }
    public string Description { get; init; } = string.Empty;
    public long InferenceTimeMs { get; init; }
}
