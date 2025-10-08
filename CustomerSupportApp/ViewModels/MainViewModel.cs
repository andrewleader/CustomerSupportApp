using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CustomerSupportApp.Models;
using CustomerSupportApp.Services;

namespace CustomerSupportApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private CustomerQuestion? _selectedQuestion;
        private string _responseText = string.Empty;
        private int _currentResponseIndex = 0;
        private int _currentPolitenessLevel = 3; // Start at most polite (0=impolite, 1=neutral, 2=somewhat polite, 3=very polite)
        private Random _random = new Random();
        private PolitenessAnalyzer _politenessAnalyzer;
        private CancellationTokenSource? _debounceTokenSource;
        private string _politenessStatus = string.Empty;
        private string _politenessLevel = string.Empty;
        private string _inferenceTime = string.Empty;
        private ObservableCollection<EpDeviceInfo> _availableDevices = new ObservableCollection<EpDeviceInfo>();
        private EpDeviceInfo? _selectedDevice;
        private const int DebounceDelayMs = 800;

        public ObservableCollection<CustomerQuestion> CustomerQuestions { get; set; }

        public ObservableCollection<EpDeviceInfo> AvailableDevices
        {
            get => _availableDevices;
            set
            {
                if (_availableDevices != value)
                {
                    _availableDevices = value;
                    OnPropertyChanged();
                }
            }
        }

        public EpDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    OnPropertyChanged();
                    _ = OnDeviceSelectionChangedAsync();
                }
            }
        }

        public CustomerQuestion? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                if (_selectedQuestion != value)
                {
                    _selectedQuestion = value;
                    OnPropertyChanged();
                    ResponseText = string.Empty; // Clear response when selecting new question
                    _currentResponseIndex = 0;
                }
            }
        }

        public string ResponseText
        {
            get => _responseText;
            set
            {
                if (_responseText != value)
                {
                    _responseText = value;
                    OnPropertyChanged();
                    _ = AnalyzeResponseWithDebounceAsync();
                }
            }
        }

        public string PolitenessStatus
        {
            get => _politenessStatus;
            set
            {
                if (_politenessStatus != value)
                {
                    _politenessStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PolitenessLevel
        {
            get => _politenessLevel;
            set
            {
                if (_politenessLevel != value)
                {
                    _politenessLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InferenceTime
        {
            get => _inferenceTime;
            set
            {
                if (_inferenceTime != value)
                {
                    _inferenceTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public void GetRandomResponse()
        {
            if (SelectedQuestion?.SuggestedResponses != null && SelectedQuestion.SuggestedResponses.Count > 0)
            {
                int newIndex;
                do
                {
                    newIndex = _random.Next(0, SelectedQuestion.SuggestedResponses.Count);
                } while (newIndex == _currentResponseIndex && SelectedQuestion.SuggestedResponses.Count > 1);
                
                _currentResponseIndex = newIndex;
                _currentPolitenessLevel = 3; // Default to very polite
                ResponseText = SelectedQuestion.SuggestedResponses[_currentResponseIndex][_currentPolitenessLevel];
            }
        }

        public void NavigateToLessPolite()
        {
            if (SelectedQuestion?.SuggestedResponses != null && 
                _currentResponseIndex >= 0 && 
                _currentResponseIndex < SelectedQuestion.SuggestedResponses.Count &&
                _currentPolitenessLevel > 0)
            {
                _currentPolitenessLevel--;
                ResponseText = SelectedQuestion.SuggestedResponses[_currentResponseIndex][_currentPolitenessLevel];
            }
        }

        public void NavigateToMorePolite()
        {
            if (SelectedQuestion?.SuggestedResponses != null && 
                _currentResponseIndex >= 0 && 
                _currentResponseIndex < SelectedQuestion.SuggestedResponses.Count &&
                _currentPolitenessLevel < 3)
            {
                _currentPolitenessLevel++;
                ResponseText = SelectedQuestion.SuggestedResponses[_currentResponseIndex][_currentPolitenessLevel];
            }
        }

        private async Task AnalyzeResponseWithDebounceAsync()
        {
            // Cancel previous debounce
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            var token = _debounceTokenSource.Token;

            try
            {
                if (string.IsNullOrWhiteSpace(_responseText))
                {
                    PolitenessStatus = "";
                    PolitenessLevel = "";
                    InferenceTime = "";
                    return;
                }

                // Wait for debounce period
                await Task.Delay(DebounceDelayMs, token);

                // If not cancelled, proceed with analysis
                if (!token.IsCancellationRequested)
                {
                    PolitenessStatus = "Running inference...";
                    PolitenessLevel = "";
                    InferenceTime = "";

                    var result = await _politenessAnalyzer.AnalyzeTextAsync(_responseText);

                    if (!token.IsCancellationRequested)
                    {
                        PolitenessLevel = GetPolitenessLevelText(result.level);
                        InferenceTime = $"{result.inferenceTimeMs}ms";
                        PolitenessStatus = "";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when debounce is cancelled
            }
            catch (Exception)
            {
                PolitenessStatus = "Analysis error";
                PolitenessLevel = "";
                InferenceTime = "";
            }
        }

        private string GetPolitenessLevelText(PolitenessLevel level)
        {
            return level switch
            {
                Services.PolitenessLevel.Polite => "Polite",
                Services.PolitenessLevel.SomewhatPolite => "Somewhat Polite",
                Services.PolitenessLevel.Neutral => "Neutral",
                Services.PolitenessLevel.Impolite => "Impolite",
                _ => "Unknown"
            };
        }

        public MainViewModel()
        {
            _politenessAnalyzer = PolitenessAnalyzer.Instance;
            _politenessAnalyzer.InitializationStatusChanged += OnPolitenessInitializationStatusChanged;

            // Initialize the analyzer asynchronously and load devices
            _ = InitializePolitenessAnalyzerAsync();

            CustomerQuestions = new ObservableCollection<CustomerQuestion>
            {
                new CustomerQuestion
                {
                    Id = 1,
                    CustomerName = "Alice Johnson",
                    CustomerEmail = "alice.johnson@email.com",
                    Subject = "Unable to login to my account",
                    Message = "Hello, I've been trying to login to my account for the past hour but I keep getting an error message saying 'Invalid credentials'. I'm absolutely sure I'm using the correct password. Can you please help me resolve this issue?",
                    ReceivedDate = DateTime.Now.AddHours(-2),
                    Priority = "High",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        // Response Type 1: Professional password reset with detailed steps
                        new string[]
                        {
                            // [0] Impolite
                            "Check your caps lock and make sure you're typing the password correctly. Most login issues are user error. If that doesn't work, use the 'Forgot Password' link.",
                            
                            // [1] Neutral
                            "I've reset your password. You should receive an email with a reset link within 5 minutes. Use the link to set a new password and try logging in again.",
                            
                            // [2] Somewhat Polite
                            "Hi Alice,\n\nI understand how frustrating login issues can be. Let me help you get back into your account.\n\nI've initiated a password reset for your account. You should receive an email shortly with instructions. Once you reset your password, you should be able to log in without any issues.\n\nPlease let me know if you continue to experience problems.\n\nThanks,\nSupport Team",
                            
                            // [3] Very Polite
                            "Dear Alice,\n\nThank you for reaching out to us. I sincerely apologize for the inconvenience you're experiencing with your account login.\n\nI'd be happy to help you resolve this issue right away. To assist you better, I've reset your password on our end. Please check your email for a password reset link that will arrive within the next 5 minutes.\n\nIf you don't receive it, please check your spam folder. Feel free to reply to this email if you need any further assistance.\n\nBest regards,\nContoso Support Team"
                        },
                        
                        // Response Type 2: Quick troubleshooting approach
                        new string[]
                        {
                            // [0] Impolite
                            "Use the 'Forgot Password' feature on the login page. It's right there.",
                            
                            // [1] Neutral
                            "The quickest solution is to use the 'Forgot Password' feature on the login page. If that doesn't work, I'm happy to manually reset your password for you.",
                            
                            // [2] Somewhat Polite
                            "I can help with that login issue. The quickest solution is to use the 'Forgot Password' feature on the login page. If that doesn't work, I'm happy to manually reset your password for you.",
                            
                            // [3] Very Polite
                            "Thank you for contacting us about this login issue. I completely understand how frustrating this must be.\n\nThe quickest solution is to use the 'Forgot Password' feature on the login page. If that doesn't work, I'm happy to manually reset your password for you."
                        },
                        
                        // Response Type 3: Technical diagnostic approach
                        new string[]
                        {
                            // [0] Impolite
                            "Clear your browser cache and cookies. Make sure cookies are enabled. Try a different browser if it still doesn't work.",
                            
                            // [1] Neutral
                            "This could be a browser cache issue. Try clearing your cache and cookies, then attempt to log in again. If the problem persists, I'll reset your password.",
                            
                            // [2] Somewhat Polite
                            "I'd like to help you troubleshoot this. Could you try clearing your browser's cache and cookies, then attempting to log in again? If that doesn't resolve it, I'll be happy to reset your password for you.",
                            
                            // [3] Very Polite
                            "I sincerely apologize for this login difficulty. Let's try a few troubleshooting steps together.\n\nCould you please try clearing your browser's cache and cookies, then attempt to log in again? Sometimes cached data can cause authentication issues.\n\nIf this doesn't resolve the issue, I'll be more than happy to reset your password immediately. I'm here to help get you back into your account as quickly as possible."
                        },
                        
                        // Response Type 4: Account verification approach
                        new string[]
                        {
                            // [0] Impolite
                            "You need to verify you're using the right email address. Check if you have multiple accounts. Reset the password if needed.",
                            
                            // [1] Neutral
                            "I'll check your account status. Please confirm the email address you're using to log in. Once verified, I can reset your password if needed.",
                            
                            // [2] Somewhat Polite
                            "Let me look into your account to see what might be causing this. Could you confirm the email address you're using to log in? Once I verify everything, I can assist with resetting your password if necessary.",
                            
                            // [3] Very Polite
                            "Thank you for bringing this to my attention. I'd be delighted to help you regain access to your account.\n\nTo ensure we're addressing the right account, could you please confirm the email address you're using to log in? Once I've verified your account details, I'll be happy to reset your password or assist with any other access issues.\n\nI appreciate your patience as we work through this together."
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 2,
                    CustomerName = "Bob Smith",
                    CustomerEmail = "bob.smith@email.com",
                    Subject = "Billing inquiry - duplicate charge",
                    Message = "I noticed that I was charged twice for my subscription this month. The amount of $49.99 appears twice on my credit card statement dated " + DateTime.Now.AddDays(-3).ToString("MM/dd/yyyy") + ". Please investigate and refund the duplicate charge.",
                    ReceivedDate = DateTime.Now.AddHours(-4),
                    Priority = "High",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        // Response Type 1: Immediate refund with compensation
                        new string[]
                        {
                            // [0] Impolite
                            "Sometimes banks show pending charges that look like duplicates. Wait a few days and it might resolve itself. If not, contact us again.",
                            
                            // [1] Neutral
                            "I've confirmed there was a duplicate charge. The refund of $49.99 has been processed and should appear in 3-5 business days.",
                            
                            // [2] Somewhat Polite
                            "Hi Bob,\n\nI apologize for the duplicate charge. I've reviewed your account and confirmed the error.\n\nThe refund of $49.99 has been processed and should show up in 3-5 business days. I'll also monitor your account to ensure this doesn't happen again.\n\nThank you for your patience.\n\nRegards,\nBilling Support",
                            
                            // [3] Very Polite
                            "Dear Bob,\n\nThank you for bringing this to our attention, and I sincerely apologize for any confusion this may have caused.\n\nI've immediately investigated your account and confirmed that there was indeed a duplicate charge of $49.99 on your account. This was due to a processing error on our end.\n\nI've initiated a full refund of $49.99, which should appear in your account within 3-5 business days. Additionally, I've added a $10 credit to your account as an apology for this inconvenience.\n\nPlease don't hesitate to reach out if you have any other concerns.\n\nBest regards,\nContoso Billing Team"
                        },
                        
                        // Response Type 2: Investigation-focused response
                        new string[]
                        {
                            // [0] Impolite
                            "We'll look into this. Refunds typically take 7-10 business days to process. Check back then.",
                            
                            // [1] Neutral
                            "I'll investigate the duplicate charge on your account. Once confirmed, I'll process the refund within 24 hours. You should see it in 5-7 business days.",
                            
                            // [2] Somewhat Polite
                            "Thank you for reporting this billing issue. I'll investigate the duplicate charge right away and confirm what happened. Once verified, I'll process your refund immediately. You can expect to see it within 5-7 business days.",
                            
                            // [3] Very Polite
                            "Dear Bob,\n\nThank you so much for bringing this billing discrepancy to our attention. I sincerely apologize for any inconvenience this may have caused.\n\nI'm personally investigating this duplicate charge right now to understand exactly what happened. Once I've confirmed the error (which I expect to do within the next hour), I'll immediately process your refund of $49.99.\n\nYou should see the credit in your account within 5-7 business days. I'll also send you a confirmation email once the refund has been processed.\n\nThank you for your patience and understanding."
                        },
                        
                        // Response Type 3: System error explanation
                        new string[]
                        {
                            // [0] Impolite
                            "There was a billing system glitch. Your refund is being processed. Wait for it to show up in your account.",
                            
                            // [1] Neutral
                            "This was caused by a billing system error that affected several accounts. Your refund of $49.99 is being processed and will appear in 3-5 business days.",
                            
                            // [2] Somewhat Polite
                            "I apologize for this error. We experienced a billing system issue that caused some duplicate charges. I've confirmed your account was affected, and your refund of $49.99 is already being processed. You should receive it within 3-5 business days.",
                            
                            // [3] Very Polite
                            "Dear Bob,\n\nI sincerely apologize for this billing error. We recently experienced a technical issue with our billing system that unfortunately resulted in duplicate charges for some of our valued customers, including yourself.\n\nI want to assure you that this was entirely our fault, and I've already initiated a full refund of $49.99 to your account. The refund should appear within 3-5 business days.\n\nAs a gesture of our apology for this inconvenience, I've also added a $5 credit to your account for your next billing cycle.\n\nWe value your business and apologize again for this error."
                        },
                        
                        // Response Type 4: Proactive monitoring response
                        new string[]
                        {
                            // [0] Impolite
                            "I'll refund you. Make sure to check your statement next month to verify it doesn't happen again.",
                            
                            // [1] Neutral
                            "Your refund of $49.99 is being processed. I've also flagged your account to prevent future duplicate charges. Monitor your next statement to ensure it's correct.",
                            
                            // [2] Somewhat Polite
                            "I apologize for this duplicate charge. I've processed your refund of $49.99, which should appear in 3-5 days. I've also added monitoring to your account to prevent this from happening again. Please feel free to contact us if you notice any other irregularities.",
                            
                            // [3] Very Polite
                            "Dear Bob,\n\nThank you for alerting us to this billing issue. I'm truly sorry for the inconvenience this has caused you.\n\nI've immediately processed a full refund of $49.99, which should be credited to your account within 3-5 business days. Additionally, I've taken the following steps to ensure this doesn't happen again:\n\n• Added extra monitoring to your account\n• Flagged your billing profile for our technical team to review\n• Set up an alert for your next billing cycle\n\nI'll personally monitor your account over the next billing period to ensure everything processes correctly. If you have any concerns at all, please don't hesitate to reach out directly.\n\nThank you for your understanding and continued trust in Contoso."
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 3,
                    CustomerName = "Carol Williams",
                    CustomerEmail = "carol.w@email.com",
                    Subject = "Feature request: Dark mode",
                    Message = "I love using your application, but I spend a lot of time working at night and would really appreciate a dark mode option. Is this something that's planned for future releases?",
                    ReceivedDate = DateTime.Now.AddHours(-6),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        // Response Type 1: Confirmed with timeline
                        new string[]
                        {
                            // [0] Impolite
                            "You can adjust your screen brightness in the meantime. Dark mode isn't currently a priority for us.",
                            
                            // [1] Neutral
                            "Dark mode is planned for Q2 2024. We'll notify users when it's released.",
                            
                            // [2] Somewhat Polite
                            "Hi Carol,\n\nGreat news! Dark mode is coming in our next update. We expect to release it in Q2 2024.\n\nThank you for the suggestion - we love hearing from users about features they'd like to see.\n\nBest,\nSupport Team",
                            
                            // [3] Very Polite
                            "Dear Carol,\n\nThank you so much for your feedback and for being such a valued user of our application!\n\nI'm excited to share that dark mode is indeed on our roadmap! Our product team has been actively working on this feature, and we're planning to release it in our next major update, scheduled for Q2 2024.\n\nWe truly appreciate users like you who take the time to share suggestions that help us improve. I've also added your vote to this feature request to help prioritize it.\n\nIn the meantime, if you have any other suggestions or need assistance with anything else, please don't hesitate to reach out.\n\nBest regards,\nContoso Product Team"
                        },
                        
                        // Response Type 2: Under consideration
                        new string[]
                        {
                            // [0] Impolite
                            "Thanks for the suggestion. We'll pass it along to the product team. No timeline currently.",
                            
                            // [1] Neutral
                            "We're considering dark mode for a future update. I've logged your feature request with the product team.",
                            
                            // [2] Somewhat Polite
                            "Thank you for this great suggestion! Dark mode is definitely something we're considering. I've added your request to our feature backlog and the team will review it for upcoming releases.",
                            
                            // [3] Very Polite
                            "Dear Carol,\n\nThank you so much for taking the time to share this thoughtful suggestion! We genuinely appreciate feedback from engaged users like yourself.\n\nDark mode is absolutely something we're actively considering for future releases. I've personally added your request to our feature backlog and highlighted it for our product team's review.\n\nWhile I can't commit to a specific timeline just yet, I want you to know that your input is valuable and will definitely be considered as we plan our roadmap.\n\nPlease feel free to share any other ideas you have - we love hearing from you!"
                        },
                        
                        // Response Type 3: Workaround offered
                        new string[]
                        {
                            // [0] Impolite
                            "Use your operating system's dark mode setting if you have one. We don't have dark mode built in.",
                            
                            // [1] Neutral
                            "Dark mode isn't available yet, but you can use your OS-level dark mode settings as a temporary solution. We're considering adding native dark mode in the future.",
                            
                            // [2] Somewhat Polite
                            "Thanks for the suggestion! While we don't have dark mode built into the app yet, you might find your operating system's dark mode setting helpful in the meantime. I've also forwarded your request to our product team for future consideration.",
                            
                            // [3] Very Polite
                            "Dear Carol,\n\nThank you for this excellent feature request! I completely understand how helpful dark mode would be for evening work sessions.\n\nWhile we don't currently have a native dark mode option in our application, you might find it helpful to enable your operating system's dark mode setting as a temporary workaround. This can help reduce eye strain during nighttime use.\n\nThat said, I've forwarded your request to our product team with my full support. Your feedback helps us prioritize features that matter most to our users.\n\nThank you again for being such an engaged member of our community!"
                        },
                        
                        // Response Type 4: Beta program invitation
                        new string[]
                        {
                            // [0] Impolite
                            "We're working on it. Check our website for updates about when it might be available.",
                            
                            // [1] Neutral
                            "Dark mode is in development. If you're interested, you can join our beta program to test it early. I'll send you the signup link.",
                            
                            // [2] Somewhat Polite
                            "Great news - we're actually working on dark mode right now! If you'd like early access, I can add you to our beta program. You'll get to test the feature before it's officially released and provide feedback.",
                            
                            // [3] Very Polite
                            "Dear Carol,\n\nWhat perfect timing! I'm thrilled to let you know that our team is actually working on dark mode as we speak!\n\nBecause you've shown such enthusiasm for this feature, I'd love to invite you to our exclusive beta testing program. As a beta tester, you'll get early access to dark mode before it's released to all users, and your feedback will directly influence the final design.\n\nIf you're interested, I'd be delighted to send you an invitation. We value your input and would love to have engaged users like you helping us perfect this feature.\n\nPlease let me know if you'd like to participate!"
                        }
                    }
                },
                // Questions 4-10: Add simplified response structures for brevity
                new CustomerQuestion
                {
                    Id = 4,
                    CustomerName = "David Brown",
                    CustomerEmail = "david.brown@email.com",
                    Subject = "Export functionality not working",
                    Message = "When I try to export my data to CSV format, the export button becomes unresponsive and nothing happens. I've tried this on multiple browsers with the same result. I'm using Chrome version 120 on Windows 11.",
                    ReceivedDate = DateTime.Now.AddHours(-8),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "The export feature works fine on our end. Make sure you have a stable internet connection.",
                            "This is a known issue with Chrome 120. Try using a different browser or export as Excel format instead.",
                            "Hi David,\n\nThanks for the detailed report. This is a known issue with Chrome 120 that we're fixing.\n\nIn the meantime, try exporting as Excel format or use a different browser. The fix should be live next week.\n\nThanks,\nTech Support",
                            "Dear David,\n\nThank you for reporting this issue and providing detailed information about your environment.\n\nI've identified the problem - there's a known issue with the CSV export feature in Chrome version 120 that our team is currently addressing. As a workaround, you can try one of these solutions:\n\n1. Use the 'Export as Excel' option instead, which works in all browsers\n2. Try the export in Firefox or Edge browser\n3. Clear your browser cache and try again\n\nOur development team is working on a fix that should be deployed by next week. I'll follow up with you once it's resolved.\n\nThank you for your patience!\n\nBest regards,\nContoso Technical Support"
                        },
                        new string[] {
                            "Have you tried clearing your cache? That usually fixes export issues.",
                            "Clear your browser cache and try again. Let me know if the problem continues.",
                            "Could you try clearing your browser cache and attempting the export again? This often resolves similar issues. I'm here if you need further assistance.",
                            "Thank you for reaching out about this export issue. Could you please try clearing your browser's cache and cookies, then attempt the export again? I've found this resolves most export-related issues. If this doesn't work, I'm happy to investigate further and find an alternative solution for you."
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 5,
                    CustomerName = "Emily Davis",
                    CustomerEmail = "emily.davis@email.com",
                    Subject = "How to upgrade my subscription plan?",
                    Message = "I'm currently on the Basic plan and would like to upgrade to the Professional plan. I couldn't find a clear option in the settings. Could you guide me through the process?",
                    ReceivedDate = DateTime.Now.AddHours(-10),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "Check the settings menu. Everything you need is there under Billing.",
                            "Go to Settings > Billing > Upgrade Plan. Follow the prompts to upgrade to Professional.",
                            "Hi Emily,\n\nGreat choice! To upgrade:\n1. Go to Settings > Billing\n2. Click 'Upgrade Plan'\n3. Select Professional and complete checkout\n\nOr I can upgrade you directly - just let me know!\n\nThanks,\nSupport Team",
                            "Dear Emily,\n\nThank you for your interest in upgrading to our Professional plan! I'd be delighted to help you with this.\n\nHere's how to upgrade:\n\n1. Log into your account\n2. Click on your profile icon in the top right corner\n3. Select 'Billing & Subscriptions'\n4. Click 'Upgrade Plan'\n5. Choose the Professional plan and complete the payment\n\nAlternatively, I can upgrade your account for you right now if you prefer. Just reply to this email and I'll take care of everything.\n\nThe Professional plan includes advanced analytics, priority support, and unlimited storage. You'll be billed $99/month, and you can cancel anytime.\n\nLet me know if you need any assistance!\n\nBest regards,\nContoso Sales Team"
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 6,
                    CustomerName = "Frank Miller",
                    CustomerEmail = "frank.miller@email.com",
                    Subject = "Integration with third-party tools",
                    Message = "Does your platform support integration with Salesforce? We're evaluating your solution for our team and this integration is critical for our workflow.",
                    ReceivedDate = DateTime.Now.AddHours(-12),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "Yes, we have integrations. You'll need to upgrade to see all the options available.",
                            "We support Salesforce integration on Professional and Enterprise plans. Check our documentation for setup details.",
                            "Hi Frank,\n\nYes! We have full Salesforce integration available on Professional and Enterprise plans.\n\nI can set up a demo to show you how it works. Are you available for a call this week?\n\nBest,\nSupport Team",
                            "Dear Frank,\n\nThank you for considering Contoso for your team! I'm happy to help with your integration requirements.\n\nYes, we do offer Salesforce integration! Our Professional and Enterprise plans include:\n\n• Bi-directional data sync with Salesforce\n• Custom field mapping\n• Real-time updates\n• Automated workflow triggers\n\nI'd love to schedule a quick demo to show you exactly how the integration works and ensure it meets your team's needs. We also offer a 30-day trial of our Enterprise plan so you can test the integration risk-free.\n\nWould you be available for a 30-minute call this week? I can also send you our integration documentation if you'd like to review it first.\n\nLooking forward to working with you!\n\nBest regards,\nContoso Solutions Team"
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 7,
                    CustomerName = "Grace Lee",
                    CustomerEmail = "grace.lee@email.com",
                    Subject = "Password reset link not working",
                    Message = "I requested a password reset link 30 minutes ago, received the email, but when I click on the link it says 'Link expired or invalid'. I've tried requesting a new link multiple times with the same issue.",
                    ReceivedDate = DateTime.Now.AddHours(-14),
                    Priority = "High",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "The links expire quickly for security reasons. You need to use them faster after requesting them.",
                            "Try requesting a new password reset link and use it within 15 minutes. Contact us if it still doesn't work.",
                            "Hi Grace,\n\nSorry about the reset link issues. I've sent you a new one that's valid for 24 hours.\n\nIf it still doesn't work, try copying/pasting the link instead of clicking it. Or call our support line for immediate help.\n\nThanks,\nSupport Team",
                            "Dear Grace,\n\nI sincerely apologize for the trouble you're experiencing with the password reset. Let me help you resolve this immediately.\n\nI've manually reset your password on our end and sent you a new secure reset link to your email. This link will be valid for 24 hours. Please check your inbox (and spam folder just in case) within the next few minutes.\n\nIf you continue to experience issues, please try:\n1. Copying and pasting the link directly into your browser instead of clicking it\n2. Using a different browser or device\n3. Clearing your browser cache\n\nIf none of these work, please call our priority support line at 1-800-CONTOSO and reference ticket #CS-7891. We'll get you back into your account right away.\n\nBest regards,\nContoso Security Team"
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 8,
                    CustomerName = "Henry Wilson",
                    CustomerEmail = "henry.wilson@email.com",
                    Subject = "Mobile app crashes on startup",
                    Message = "The mobile app (iOS version 2.4.1) crashes immediately upon startup on my iPhone 14 Pro. I've tried reinstalling the app but the problem persists. This started happening after the latest update.",
                    ReceivedDate = DateTime.Now.AddHours(-16),
                    Priority = "High",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "Have you tried turning your phone off and on again? That usually fixes most app crashes.",
                            "This is a known issue with the latest update. A fix is being released soon. Try using the web version in the meantime.",
                            "Hi Henry,\n\nWe've found the bug affecting iPhone 14 Pro users. A fix is coming in 24-48 hours.\n\nTemporary solution: Uninstall the app, restart your phone, and install version 2.4.0 from the App Store.\n\nI'll notify you when the update is live.\n\nThanks,\nMobile Support",
                            "Dear Henry,\n\nThank you for reporting this critical issue, and I sincerely apologize for the disruption this has caused.\n\nWe've identified a bug in version 2.4.1 that affects iPhone 14 Pro devices running iOS 17.2 or higher. Our development team has already created a fix, and version 2.4.2 is currently under review by Apple.\n\nIn the meantime, here's a temporary workaround:\n1. Go to Settings > General > iPhone Storage\n2. Find the Contoso app and delete it\n3. Restart your iPhone\n4. Download version 2.4.0 from the App Store (we've made it available as a rollback option)\n\nThe new version 2.4.2 should be available in the App Store within 24-48 hours. I'll personally email you when it's released.\n\nAgain, I apologize for this inconvenience. Thank you for your patience.\n\nBest regards,\nContoso Mobile Team"
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 9,
                    CustomerName = "Isabel Martinez",
                    CustomerEmail = "isabel.martinez@email.com",
                    Subject = "Documentation request",
                    Message = "I'm a developer trying to integrate with your API. Is there comprehensive API documentation available? I couldn't find detailed information about authentication and rate limits on your website.",
                    ReceivedDate = DateTime.Now.AddHours(-18),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "Have you checked our website? All the documentation is there if you look for it.",
                            "API documentation is available at developers.contoso.com/docs. You'll find authentication and rate limit information there.",
                            "Hi Isabel,\n\nOur full API docs are at: https://developers.contoso.com/docs\n\nYou'll find authentication, rate limits, and code samples there. Let me know if you need anything else!\n\nBest,\nDev Support",
                            "Dear Isabel,\n\nThank you for your interest in integrating with our API! I'd be happy to point you to our developer resources.\n\nOur comprehensive API documentation is available at: https://developers.contoso.com/docs\n\nThis includes:\n• Complete API reference with all endpoints\n• Authentication guide (OAuth 2.0 and API key methods)\n• Rate limiting details (1000 requests/hour for standard tier, 10000 for enterprise)\n• Code examples in Python, JavaScript, Java, and C#\n• Interactive API explorer\n\nI'm also sending you our Developer Quick Start Guide PDF and a sample integration project to help you get started faster.\n\nIf you need elevated rate limits or have specific integration questions, please let me know. We also offer dedicated developer support for enterprise customers.\n\nHappy coding!\n\nBest regards,\nContoso Developer Relations"
                        }
                    }
                },
                new CustomerQuestion
                {
                    Id = 10,
                    CustomerName = "Jack Thompson",
                    CustomerEmail = "jack.thompson@email.com",
                    Subject = "Account deletion request",
                    Message = "I would like to delete my account and all associated data. Please let me know the process and confirm once my data has been completely removed from your systems.",
                    ReceivedDate = DateTime.Now.AddHours(-20),
                    Priority = "Normal",
                    IsRead = false,
                    SuggestedResponses = new List<string[]>
                    {
                        new string[] {
                            "Are you sure you want to delete your account? You'll lose everything and can't get it back. Go to Settings > Account > Delete if you're certain.",
                            "To delete your account, go to Settings > Account > Delete Account. Follow the prompts. Note that this is permanent.",
                            "Hi Jack,\n\nI can help with that. Please note that deletion is permanent and includes all your data.\n\nReply with 'CONFIRM DELETE' and I'll process it within 24 hours. You'll get a confirmation email when it's complete.\n\nThanks,\nSupport Team",
                            "Dear Jack,\n\nI'm sorry to see you go, but I respect your decision and will help you through this process.\n\nBefore we proceed with account deletion, I want to make sure you're aware that:\n• All your data, projects, and files will be permanently deleted\n• This action cannot be undone\n• Your subscription will be cancelled, and you'll retain access until the end of your current billing period\n\nIf you're certain you want to proceed, please reply to this email with \"CONFIRM DELETE\" and I'll process your request within 24 hours. You'll receive a final confirmation email once your data has been completely removed from our systems.\n\nIf there's anything we could have done better or if you're leaving due to a specific issue, I'd love to hear your feedback. It would help us improve our service.\n\nBest regards,\nContoso Account Team"
                        }
                    }
                }
            };
        }

        private async Task InitializePolitenessAnalyzerAsync()
        {
            try
            {
                // Pre-initialize (download EPs, load tokenizer, enumerate devices)
                await _politenessAnalyzer.PreInitializeAsync();

                // Get available devices
                var devices = await _politenessAnalyzer.GetAvailableDevicesAsync();
                AvailableDevices = new ObservableCollection<EpDeviceInfo>(devices);

                // Select the first device by default
                if (AvailableDevices.Count > 0)
                {
                    SelectedDevice = AvailableDevices[0];
                }
            }
            catch (Exception)
            {
                PolitenessStatus = "Initialization failed";
            }
        }

        private async Task OnDeviceSelectionChangedAsync()
        {
            if (_selectedDevice == null)
                return;

            try
            {
                // Initialize the inference session with the selected device
                await _politenessAnalyzer.InitializeWithDeviceAsync(_selectedDevice.Device);
                
                // Re-analyze current text if any
                if (!string.IsNullOrWhiteSpace(_responseText))
                {
                    _ = AnalyzeResponseWithDebounceAsync();
                }
            }
            catch (Exception)
            {
                PolitenessStatus = "Device initialization failed";
            }
        }

        private void OnPolitenessInitializationStatusChanged(object? sender, string status)
        {
            PolitenessStatus = status;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
