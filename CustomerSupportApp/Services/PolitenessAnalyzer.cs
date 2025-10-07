using System;
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
        private Random _random = new Random();

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

            // Simulate model loading with status updates
            OnInitializationStatusChanged("Initializing model...");
            await Task.Delay(3000);

            OnInitializationStatusChanged("Loading politeness model...");
            await Task.Delay(3000);

            OnInitializationStatusChanged("Model ready");
            await Task.Delay(300);

            _isInitialized = true;
        }

        public async Task<(PolitenessLevel level, string description)> AnalyzeTextAsync(string text)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return (PolitenessLevel.Neutral, "No text to analyze");
            }

            // Simulate inference time
            await Task.Delay(300);

            // Mock implementation - randomly select a politeness level based on text characteristics
            // In the future, this will be replaced with an AI model
            var level = DeterminePolitenessLevel(text);
            var description = GetPolitenessDescription(level);

            return (level, description);
        }

        private PolitenessLevel DeterminePolitenessLevel(string text)
        {
            // Simple heuristic for demo purposes
            text = text.ToLower();

            // Check for polite indicators
            if (text.Contains("dear") || text.Contains("sincerely") || text.Contains("apologize") ||
                text.Contains("thank you") || text.Contains("please") || text.Contains("would be happy") ||
                text.Contains("appreciate"))
            {
                return PolitenessLevel.Polite;
            }

            // Check for impolite indicators
            if (text.Contains("user error") || text.Contains("should know") || text.Contains("obviously") ||
                text.Contains("just ") && (text.Contains("check") || text.Contains("look")) ||
                text.Contains("you need to") || text.Length < 100)
            {
                return PolitenessLevel.Impolite;
            }

            // Check for somewhat polite
            if (text.Contains("hi ") || text.Contains("thanks") || text.Contains("let me know"))
            {
                return PolitenessLevel.SomewhatPolite;
            }

            // Default to neutral
            return PolitenessLevel.Neutral;
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
}
