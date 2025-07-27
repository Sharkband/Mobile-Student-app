using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Models
{
    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public string Hint { get; set; }
        public string Category { get; set; }
        public string Difficulty { get; set; }  
        public int CorrectAnswerIndex { get; set; }
        
    }
}
