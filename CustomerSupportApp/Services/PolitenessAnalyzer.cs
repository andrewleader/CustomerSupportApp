using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Windows.AI.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerSupportApp.Services
{
    public enum PolitenessLevel
    {
        Polite,
        SomewhatPolite,
        Neutral,
        Impolite
    }

    public class PolitenessAnalyzer
    {
        private static PolitenessAnalyzer? _instance;
        private static readonly object _lock = new object();
        private bool _isInitialized = false;
        private InferenceSession? _inferenceSession;
        private BertTokenizer? _tokenizer;
        private const int MaxSequenceLength = 512;

        public event EventHandler<string>? InitializationStatusChanged;

        private PolitenessAnalyzer()
        {
        }

        public static PolitenessAnalyzer Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new PolitenessAnalyzer();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            // Download EPs
            OnInitializationStatusChanged("Downloading and registering EPs...");
            await ExecutionProviderCatalog.GetDefault().EnsureAndRegisterCertifiedAsync();

            OnInitializationStatusChanged("Loading tokenizer...");
            _tokenizer = await Task.Run(() => new BertTokenizer());

            OnInitializationStatusChanged("Loading politeness model...");
            _inferenceSession = await Task.Run(() => CreateInferenceSession());

            OnInitializationStatusChanged("Model ready");

            _isInitialized = true;
        }

        private InferenceSession CreateInferenceSession()
        {
            // First we create a new instance of EnvironmentCreationOptions
            EnvironmentCreationOptions envOptions = new()
            {
                logId = "PolitenessGuard",
                logLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR
            };

            // And then use that to create the ORT environment
            using var ortEnv = OrtEnv.CreateInstanceWithOptions(ref envOptions);

            // 1. Enumerate devices
            var epDevices = ortEnv.GetEpDevices();

            const string epName = "DmlExecutionProvider";

            // 2. Filter to your desired execution provider
            var selectedEpDevices = epDevices
                .Where(d => d.EpName == epName)
                .ToList();

            if (selectedEpDevices.Count == 0)
            {
                throw new InvalidOperationException($"{epName} is not available on this system.");
            }

            // 3. Configure provider-specific options (varies based on EP)
            // and append the EP with the correct devices (varies based on EP)
            var sessionOptions = new SessionOptions();
            var epOptions = new Dictionary<string, string> { };
            sessionOptions.AppendExecutionProvider(ortEnv, selectedEpDevices, epOptions);

            return new InferenceSession("C:\\Users\\aleader\\Downloads\\model.onnx", sessionOptions);
        }

        public async Task<(PolitenessLevel level, string description, long inferenceTimeMs)> AnalyzeTextAsync(string text)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return (PolitenessLevel.Neutral, "No text to analyze", 0);
            }

            if (_inferenceSession == null || _tokenizer == null)
            {
                throw new InvalidOperationException("Model not initialized properly");
            }

            // Run inference on background thread and measure time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var level = await Task.Run(() => RunInference(text));
            stopwatch.Stop();
            
            var description = GetPolitenessDescription(level);

            return (level, description, stopwatch.ElapsedMilliseconds);
        }

        private PolitenessLevel RunInference(string text)
        {
            // Tokenize the input text
            var encoding = _tokenizer!.Encode(text, MaxSequenceLength);

            // Create input tensors
            var inputIds = new DenseTensor<long>(new[] { 1, encoding.InputIds.Length });
            var attentionMask = new DenseTensor<long>(new[] { 1, encoding.AttentionMask.Length });
            var tokenTypeIds = new DenseTensor<long>(new[] { 1, encoding.TokenTypeIds.Length });

            for (int i = 0; i < encoding.InputIds.Length; i++)
            {
                inputIds[0, i] = encoding.InputIds[i];
                attentionMask[0, i] = encoding.AttentionMask[i];
                tokenTypeIds[0, i] = encoding.TokenTypeIds[i];
            }

            // Create input dictionary for ONNX model
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
            };

            // Run inference
            using var results = _inferenceSession!.Run(inputs);
            
            // Get the logits output (assuming output name is "logits")
            var logits = results.First().AsEnumerable<float>().ToArray();

            // Apply softmax to get probabilities
            var probabilities = Softmax(logits);

            // Get the class with highest probability
            int predictedClass = GetMaxIndex(probabilities);

            // Map to PolitenessLevel
            // The Intel/polite-guard model outputs:
            // 0: polite, 1: impolite
            // We'll map this to our 4 levels based on confidence
            return MapToPolitenessLevel(predictedClass, probabilities);
        }

        private PolitenessLevel MapToPolitenessLevel(int predictedClass, float[] probabilities)
        {
            // predictedClass: 0 = polite, 1 = impolite
            float confidence = probabilities[predictedClass];

            if (predictedClass == 0) // Polite
            {
                // If confidence is very high (>0.8), it's Polite
                // Otherwise it's Somewhat Polite
                return confidence > 0.8f ? PolitenessLevel.Polite : PolitenessLevel.SomewhatPolite;
            }
            else // Impolite (class 1)
            {
                // If confidence is very high (>0.8), it's Impolite
                // Otherwise it's Neutral
                return confidence > 0.8f ? PolitenessLevel.Impolite : PolitenessLevel.Neutral;
            }
        }

        private float[] Softmax(float[] values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal)).ToArray();
            var sum = exp.Sum();
            return exp.Select(e => (float)(e / sum)).ToArray();
        }

        private int GetMaxIndex(float[] values)
        {
            int maxIndex = 0;
            float maxValue = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > maxValue)
                {
                    maxValue = values[i];
                    maxIndex = i;
                }
            }
            return maxIndex;
        }

        private string GetPolitenessDescription(PolitenessLevel level)
        {
            return level switch
            {
                PolitenessLevel.Polite => "Text is considerate and shows respect and good manners, often including courteous phrases and a friendly tone.",
                PolitenessLevel.SomewhatPolite => "Text is generally respectful but lacks warmth or formality, communicating with a decent level of courtesy.",
                PolitenessLevel.Neutral => "Text is straightforward and factual, without emotional undertones or specific attempts at politeness.",
                PolitenessLevel.Impolite => "Text is disrespectful or rude, often blunt or dismissive, showing a lack of consideration for the recipient's feelings.",
                _ => "Unable to determine politeness level"
            };
        }

        private void OnInitializationStatusChanged(string status)
        {
            InitializationStatusChanged?.Invoke(this, status);
        }
    }

    // Simple BERT tokenizer implementation
    public class BertTokenizer
    {
        private const int PadTokenId = 0;
        private const int ClsTokenId = 101;
        private const int SepTokenId = 102;
        private const int UnkTokenId = 100;

        private readonly Dictionary<string, int> _vocab;

        public BertTokenizer()
        {
            // Initialize with a basic vocabulary
            // In a real implementation, you would load this from vocab.txt
            _vocab = new Dictionary<string, int>();
            LoadBasicVocab();
        }

        private void LoadBasicVocab()
        {
            // This is a simplified vocabulary. In production, you'd load the actual
            // BERT vocabulary file (vocab.txt) that comes with the model
            // For now, we'll use a simple character-level approach
            _vocab["[PAD]"] = PadTokenId;
            _vocab["[UNK]"] = UnkTokenId;
            _vocab["[CLS]"] = ClsTokenId;
            _vocab["[SEP]"] = SepTokenId;
        }

        public BertEncoding Encode(string text, int maxLength)
        {
            // Simplified tokenization - in production use a proper WordPiece tokenizer
            var tokens = SimpleTokenize(text.ToLower());
            
            // Add special tokens
            var inputIds = new List<long> { ClsTokenId };
            
            // Convert tokens to IDs
            foreach (var token in tokens)
            {
                if (inputIds.Count >= maxLength - 1)
                    break;
                
                // Simple hash-based ID generation for demo
                // In production, use actual vocabulary lookup
                int tokenId = GetTokenId(token);
                inputIds.Add(tokenId);
            }
            
            // Add SEP token
            inputIds.Add(SepTokenId);
            
            // Create attention mask (1 for real tokens, 0 for padding)
            var attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToList();
            
            // Create token type IDs (all 0s for single sequence)
            var tokenTypeIds = Enumerable.Repeat(0L, inputIds.Count).ToList();
            
            // Pad to max length
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
            // Very simple tokenization - split on whitespace and punctuation
            // In production, use WordPiece tokenization
            return text.Split(new[] { ' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t' }, 
                             StringSplitOptions.RemoveEmptyEntries)
                       .ToList();
        }

        private int GetTokenId(string token)
        {
            if (_vocab.TryGetValue(token, out int id))
                return id;
            
            // Simple hash function for unknown tokens
            // In production, use proper vocabulary lookup
            int hash = 0;
            foreach (char c in token)
            {
                hash = (hash * 31 + c) % 30000; // Keep in reasonable vocab range
            }
            return Math.Max(200, hash); // Avoid special token IDs
        }
    }

    public class BertEncoding
    {
        public long[] InputIds { get; set; } = Array.Empty<long>();
        public long[] AttentionMask { get; set; } = Array.Empty<long>();
        public long[] TokenTypeIds { get; set; } = Array.Empty<long>();
    }
}
