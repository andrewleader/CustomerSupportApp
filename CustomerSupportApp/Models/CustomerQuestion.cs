using System;
using System.Collections.Generic;

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
        
        // Each response type has 4 variations: [0]=Impolite, [1]=Neutral, [2]=Somewhat Polite, [3]=Very Polite
        public List<string[]> SuggestedResponses { get; set; } = new List<string[]>();
    }
}
