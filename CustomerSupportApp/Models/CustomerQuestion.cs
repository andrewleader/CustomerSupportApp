using System;

namespace CustomerSupportApp.Models
{
    public class CustomerQuestion
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        public string Priority { get; set; } = "Normal";
        public bool IsRead { get; set; }
    }
}
