namespace Contoso.AI.PolitenessAnalysis;

internal class BertTokenizer
{
    private const int PadTokenId = 0;
    private const int ClsTokenId = 101;
    private const int SepTokenId = 102;
    private const int UnkTokenId = 100;

    private readonly Dictionary<string, int> _vocab;

    public BertTokenizer()
    {
        _vocab = new Dictionary<string, int>();
        LoadBasicVocab();
    }

    private void LoadBasicVocab()
    {
        _vocab["[PAD]"] = PadTokenId;
        _vocab["[UNK]"] = UnkTokenId;
        _vocab["[CLS]"] = ClsTokenId;
        _vocab["[SEP]"] = SepTokenId;
    }

    public BertEncoding Encode(string text, int maxLength)
    {
        var tokens = SimpleTokenize(text.ToLower());
        
        var inputIds = new List<long> { ClsTokenId };
        
        foreach (var token in tokens)
        {
            if (inputIds.Count >= maxLength - 1)
                break;
            
            int tokenId = GetTokenId(token);
            inputIds.Add(tokenId);
        }
        
        inputIds.Add(SepTokenId);
        
        var attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToList();
        var tokenTypeIds = Enumerable.Repeat(0L, inputIds.Count).ToList();
        
        while (inputIds.Count < maxLength)
        {
            inputIds.Add(PadTokenId);
            attentionMask.Add(0);
            tokenTypeIds.Add(0);
        }

        return new BertEncoding
        {
            InputIds = inputIds.ToArray(),
            AttentionMask = attentionMask.ToArray(),
            TokenTypeIds = tokenTypeIds.ToArray()
        };
    }

    private List<string> SimpleTokenize(string text)
    {
        return text.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t' }, 
                         StringSplitOptions.RemoveEmptyEntries)
                   .ToList();
    }

    private int GetTokenId(string token)
    {
        if (_vocab.TryGetValue(token, out int id))
            return id;
        
        int hash = 0;
        foreach (char c in token)
        {
            hash = (hash * 31 + c) % 30000;
        }
        return Math.Max(200, hash);
    }
}

internal class BertEncoding
{
    public long[] InputIds { get; init; } = Array.Empty<long>();
    public long[] AttentionMask { get; init; } = Array.Empty<long>();
    public long[] TokenTypeIds { get; init; } = Array.Empty<long>();
}
