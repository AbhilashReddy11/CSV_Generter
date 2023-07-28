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

        //lKBD40bX5sUEtBBk8UE4T3BlbkFJhqIsLUH1YdahrFYbyPyH
    }
}
