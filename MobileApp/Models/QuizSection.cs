using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Models
{
    public class QuizSection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string DifficultyColor { get; set; }
        public int QuestionCount { get; set; }
        public string Difficulty { get; set; }

        public List<QuizQuestion> Questions { get; set; } = new ();
    }
}
