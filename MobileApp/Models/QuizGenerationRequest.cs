using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Models
{
    public class QuizGenerationRequest
    {
        public string Topic { get; set; }
        public int NumberOfQuestions { get; set; }
        public string Difficulty { get; set; }
        public string QuestionType { get; set; } // "multiple-choice", "true-false", etc.
    }
}
