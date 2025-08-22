using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Models
{
    public static class QuizEventManager
    {
        public static event EventHandler<QuizStatsEventArgs> QuizCompleted;

        public static void RaiseQuizCompleted(QuizStatsEventArgs stats)
        {
            QuizCompleted?.Invoke(null, stats);
        }
    }
}
