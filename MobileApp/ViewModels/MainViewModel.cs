
using System;
using System.Collections.Generic;
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

        public MainViewModel()
        {
            // Initialize default values
            WelcomeMessage = "Welcome back!";
            MotivationalMessage = "Ready to learn something new?";
            DailyProgress = 0.65;
            DailyProgressText = "65%";
            CurrentStreak = 12;
            TotalPoints = 850;
            TotalSubjects = 5;

            // Initialize commands
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

        #endregion

        #region Commands

        public ICommand NavigateToQuizCommand { get; private set; }
        public ICommand NavigateToFlashcardsCommand { get; private set; }

        #endregion

        #region Methods

        private async Task NavigateToQuiz()
        {
            // TODO: Navigate to quiz page
            // Example: await Shell.Current.GoToAsync("//quiz");
            await Application.Current.MainPage.DisplayAlert("Navigation", "Navigate to Quiz Page", "OK");
        }

        private async Task NavigateToFlashcards()
        {
            // TODO: Navigate to flashcards page  
            // Example: await Shell.Current.GoToAsync("//flashcards");
            await Application.Current.MainPage.DisplayAlert("Navigation", "Navigate to Flashcards Page", "OK");
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
