using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Models
{
    public class QuizStatsEventArgs : EventArgs
    {
        public string SectionName { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalQuizzes { get; set; }
        public double Accuracy { get; set; }
        public string Difficulty { get; set; }
        public bool IsCompleted { get; set; } = true; // false for progress events
        public int CurrentQuestionIndex { get; set; }
    }
}
