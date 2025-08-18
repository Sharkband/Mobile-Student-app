using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MobileApp.Models
{
    public class QuizQuestion
    {
        // Keep your original properties for backward compatibility
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectAnswerIndex { get; set; }

        // Add properties that your XAML expects
        public string QuestionText
        {
            get => Question;
            set => Question = value;
        }

        public string OptionA
        {
            get => Options?.Count > 0 ? Options[0] : "";
        }

        public string OptionB
        {
            get => Options?.Count > 1 ? Options[1] : "";
        }

        public string OptionC
        {
            get => Options?.Count > 2 ? Options[2] : "";
        }

        public string OptionD
        {
            get => Options?.Count > 3 ? Options[3] : "";
        }

        // Convert CorrectAnswerIndex to letter format
        public string CorrectAnswer
        {
            get
            {
                return CorrectAnswerIndex switch
                {
                    0 => "A",
                    1 => "B",
                    2 => "C",
                    3 => "D",
                    _ => "A"
                };
            }
            set
            {
                CorrectAnswerIndex = value?.ToUpper() switch
                {
                    "A" => 0,
                    "B" => 1,
                    "C" => 2,
                    "D" => 3,
                    _ => 0
                };
            }
        }

        // Keep existing properties
        public string Explanation { get; set; }
        public string Hint { get; set; }
        public string Category { get; set; }
        public string Difficulty { get; set; }

        // Constructor to ensure Options list is initialized
        public QuizQuestion()
        {
            Options = new List<string>();
        }

        // Helper method to set options easily
        public void SetOptions(string optionA, string optionB, string optionC, string optionD)
        {
            Options = new List<string> { optionA, optionB, optionC, optionD };
        }
    }
}
