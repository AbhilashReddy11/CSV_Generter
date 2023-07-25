// File: InputOutputModel.cs
using System.ComponentModel.DataAnnotations;

namespace CSV_Generter.Models
{
    public class InputOutputModel
    {
        [Required]
        public string Prompt { get; set; }
        public string Response { get; set; }
        public string ResponseFileUrl { get; set; }
        public List<string> ConversationHistory { get; set; } = new List<string>();
        //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
    }
}
