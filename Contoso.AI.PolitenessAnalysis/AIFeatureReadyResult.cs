namespace Contoso.AI.PolitenessAnalysis;

public class AIFeatureReadyResult
{
    public AIFeatureReadyResultState Status { get; init; }
    public Exception? ExtendedError { get; init; }
    
    public static AIFeatureReadyResult Success() => new() { Status = AIFeatureReadyResultState.Success };
    public static AIFeatureReadyResult Failed(Exception error) => new() { Status = AIFeatureReadyResultState.Failed, ExtendedError = error };
}
