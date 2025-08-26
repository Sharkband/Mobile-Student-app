
using MobileApp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MobileApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        

        private string _welcomeMessage;
        private string _motivationalMessage;
        private double _dailyProgress;
        private string _dailyProgressText;
        private int _currentStreak;
        private int _totalPoints;
        private int _totalSubjects;
        private string _lastQuizScore;
        private string _lastQuizAccuracy;
        private int totalQuizzes;
        

        public int TotalQuizzesCompleted { get; private set; }
        public double AverageAccuracy { get; private set; }
        public int TotalQuestionsAnswered { get; private set; }
        public string LastQuizResult { get; private set; }
        public ObservableCollection<QuizStatsEventArgs> RecentQuizzes { get; private set; }

        public MainViewModel()
        {
            // Initialize default values
            WelcomeMessage = "Welcome back!";
            MotivationalMessage = "Ready to learn something new?";
            DailyProgress = 0.0;
            DailyProgressText = "0.0%";
            CurrentStreak = 0;
            TotalPoints = 0;
            TotalSubjects = 0;

            RecentQuizzes = new ObservableCollection<QuizStatsEventArgs>();

            LoadStatsFromPreferences();

            // Subscribe to static events - this works even with new instances
            QuizEventManager.QuizCompleted += OnQuizCompletedEvent;

            // Initialize commands
            NavigateToAiCommand = new Command(async () => await NavigateToAi());
            NavigateToQuizCommand = new Command(async () => await NavigateToQuiz());
            NavigateToFlashcardsCommand = new Command(async () => await NavigateToFlashcards());
        }

        #region Properties

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string MotivationalMessage
        {
            get => _motivationalMessage;
            set => SetProperty(ref _motivationalMessage, value);
        }

        public double DailyProgress
        {
            get => _dailyProgress;
            set
            {
                SetProperty(ref _dailyProgress, value);
                DailyProgressText = $"{(int)(value * 100)}%";
            }
        }

        public string DailyProgressText
        {
            get => _dailyProgressText;
            private set => SetProperty(ref _dailyProgressText, value);
        }

        public int CurrentStreak
        {
            get => _currentStreak;
            set => SetProperty(ref _currentStreak, value);
        }

        public int TotalPoints
        {
            get => _totalPoints;
            set => SetProperty(ref _totalPoints, value);
        }

        public int TotalSubjects
        {
            get => _totalSubjects;
            set => SetProperty(ref _totalSubjects, value);
        }

        public string LastQuizScore
        {
            get => _lastQuizScore;
            set => SetProperty(ref _lastQuizScore, value);
        }

        public string LastQuizAccuracy
        {
            get => _lastQuizAccuracy;
            set => SetProperty(ref _lastQuizAccuracy, value);
        }

        #endregion

        #region Commands

        public ICommand NavigateToAiCommand { get; private set; }
        public ICommand NavigateToQuizCommand { get; private set; }
        public ICommand NavigateToFlashcardsCommand { get; private set; }

        #endregion

        #region Methods

        private void OnQuizCompletedEvent(object sender, QuizStatsEventArgs e)
        {
            OnQuizCompleted(e);
        }


        public void OnQuizCompleted( QuizStatsEventArgs e)
        {
            if (e == null) return;
            // Update overall stats
            TotalQuizzesCompleted++;
            TotalQuestionsAnswered += e.TotalQuestions;

            totalQuizzes = e.TotalQuizzes;
            TotalPoints += e.Score * 50;

            // Calculate running average
            var totalAccuracy = RecentQuizzes.Sum(q => q.Accuracy) + e.Accuracy;
            AverageAccuracy = totalAccuracy / (RecentQuizzes.Count + 1);

            // Add to recent quizzes
            RecentQuizzes.Insert(0, e);
            

            LastQuizResult = $"{e.SectionName}";

            LastQuizAccuracy = $"{e.Accuracy:F1}%";
            LastQuizScore = $"{e.Score}/{e.TotalQuestions}"; 

            UpdateValues();

            SaveStatsToPreferences();
            // Notify UI
            OnPropertyChanged(nameof(DailyProgress));
            OnPropertyChanged(nameof(DailyProgressText));
            OnPropertyChanged(nameof(TotalPoints));
            OnPropertyChanged(nameof(CurrentStreak));
            OnPropertyChanged(nameof(TotalSubjects));
            OnPropertyChanged(nameof(DailyProgress));
            OnPropertyChanged(nameof(DailyProgressText));
            OnPropertyChanged(nameof(LastQuizResult));
            OnPropertyChanged(nameof(LastQuizScore));
            OnPropertyChanged(nameof(LastQuizAccuracy));
            /*
            OnPropertyChanged(nameof(TotalQuizzesCompleted));
            OnPropertyChanged(nameof(AverageAccuracy));
            OnPropertyChanged(nameof(TotalQuestionsAnswered));
            
            */
        }

        private void UpdateValues()
        {
            CurrentStreak = TotalQuizzesCompleted;
            TotalSubjects = totalQuizzes;
            DailyProgress = AverageAccuracy / 100.0;
            DailyProgressText = $"{AverageAccuracy:F2}%";
            

        }

        private void LoadStatsFromPreferences()
        {
            try
            {
                TotalQuizzesCompleted = Preferences.Get("TotalQuizzesCompleted", 0);
                TotalQuestionsAnswered = Preferences.Get("TotalQuestionsAnswered", 0);
                TotalPoints = Preferences.Get("TotalPoints", 0);
                CurrentStreak = Preferences.Get("CurrentStreak", 0);
                TotalSubjects = Preferences.Get("TotalSubjects", 0);
                AverageAccuracy = Preferences.Get("AverageAccuracy", 0.0);
                LastQuizResult = Preferences.Get("LastQuizResult", "No quizzes completed yet");

                // Load recent quizzes from JSON
                var recentQuizzesJson = Preferences.Get("RecentQuizzes", "[]");
                if (!string.IsNullOrEmpty(recentQuizzesJson))
                {
                    var recentQuizzes = JsonConvert.DeserializeObject<List<QuizStatsEventArgs>>(recentQuizzesJson) ?? new List<QuizStatsEventArgs>();

                    RecentQuizzes.Clear();
                    foreach (var quiz in recentQuizzes.Take(10))
                    {
                        RecentQuizzes.Add(quiz);
                    }
                }

                UpdateValues();

                System.Diagnostics.Debug.WriteLine($"Loaded stats from preferences - Quizzes: {TotalQuizzesCompleted}, Average: {AverageAccuracy:F1}%");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stats: {ex.Message}");
            }
        }


        private void SaveStatsToPreferences()
        {
            try
            {
                Preferences.Set("TotalQuizzesCompleted", TotalQuizzesCompleted);
                Preferences.Set("TotalQuestionsAnswered", TotalQuestionsAnswered);
                Preferences.Set("TotalPoints", TotalPoints);
                Preferences.Set("CurrentStreak", CurrentStreak);
                Preferences.Set("TotalSubjects", TotalSubjects);
                Preferences.Set("AverageAccuracy", AverageAccuracy);
                Preferences.Set("LastQuizResult", LastQuizResult ?? "");

                // Save recent quizzes as JSON
                var recentQuizzesJson = JsonConvert.SerializeObject(RecentQuizzes.Take(10).ToList());
                Preferences.Set("RecentQuizzes", recentQuizzesJson);

                System.Diagnostics.Debug.WriteLine($"Saved stats to preferences");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving stats: {ex.Message}");
            }
        }

        private async Task NavigateToQuiz()
        {
            // TODO: Navigate to quiz page
            await Shell.Current.GoToAsync("//quiz");
            //await Application.Current.MainPage.DisplayAlert("Navigation", "Navigate to Quiz Page", "OK");
        }

        private async Task NavigateToAi()
        {
            // TODO: Navigate to quiz page
            await Shell.Current.GoToAsync("//AIQuizCreator");
            //await Application.Current.MainPage.DisplayAlert("Navigation", "Navigate to Quiz Page", "OK");
        }

        private async Task NavigateToFlashcards()
        {
            // TODO: Navigate to flashcards page  
            await Shell.Current.GoToAsync("//flashcards");
            //await Application.Current.MainPage.DisplayAlert("Navigation", "Navigate to Flashcards Page", "OK");
        }

        public void UpdateUserStats(int streak, int points, int subjects)
        {
            CurrentStreak = streak;
            TotalPoints = points;
            TotalSubjects = subjects;
        }

        public void UpdateDailyProgress(double progress)
        {
            DailyProgress = Math.Max(0, Math.Min(1, progress)); // Clamp between 0 and 1
        }

        public void UpdateWelcomeMessage(string userName = null)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                WelcomeMessage = $"Welcome back, {userName}!";
            }

            // Update motivational message based on time of day
            var hour = DateTime.Now.Hour;
            MotivationalMessage = hour switch
            {
                >= 5 and < 12 => "Good morning! Ready to start learning?",
                >= 12 and < 17 => "Good afternoon! Time for some brain exercise!",
                >= 17 and < 21 => "Good evening! Let's wrap up with some study time!",
                _ => "Late night learning session? You're dedicated!"
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
