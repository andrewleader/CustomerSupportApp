using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CustomerSupportApp.Models;

namespace CustomerSupportApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private CustomerQuestion? _selectedQuestion;
        private string _responseText = string.Empty;

        public ObservableCollection<CustomerQuestion> CustomerQuestions { get; set; }

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
                }
            }
        }

        public MainViewModel()
        {
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
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
                },
                new CustomerQuestion
                {
                    Id = 4,
                    CustomerName = "David Brown",
                    CustomerEmail = "david.brown@email.com",
                    Subject = "Export functionality not working",
                    Message = "When I try to export my data to CSV format, the export button becomes unresponsive and nothing happens. I've tried this on multiple browsers with the same result. I'm using Chrome version 120 on Windows 11.",
                    ReceivedDate = DateTime.Now.AddHours(-8),
                    Priority = "Normal",
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
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
                    IsRead = false
                }
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
