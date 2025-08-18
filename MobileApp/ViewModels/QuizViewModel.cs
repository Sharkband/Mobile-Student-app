using Microsoft.Maui.Controls;
using MobileApp.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace MobileApp.ViewModels
{
    public class QuizViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<QuizQuestion> _questions;
        private ObservableCollection<QuizSection> _quizSections;
        private QuizSection _selectedSection;
        private int _currentQuestionIndex;
        private QuizQuestion _currentQuestion;
        private int _score;
        private int _totalAnswered;
        private bool _isAnswerSelected;
        private string _selectedAnswer;
        private Timer _timer;
        private int _timeRemaining = 30; // seconds per question
        private bool _isFeedbackVisible;
        private string _feedbackText;
        private string _explanationText;
        private bool _isExplanationVisible;
        private bool _isSectionSelectionVisible = true;
        private bool _isQuizVisible = false;

        public QuizViewModel()
        {
            _questions = new ObservableCollection<QuizQuestion>();
            _quizSections = new ObservableCollection<QuizSection>();
            InitializeCommands();
        }

        // Properties for Quiz Sections
        public ObservableCollection<QuizSection> QuizSections
        {
            get => _quizSections;
            set
            {
                _quizSections = value;
                OnPropertyChanged();
            }
        }

        public QuizSection SelectedSection
        {
            get => _selectedSection;
            set
            {
                _selectedSection = value;
                OnPropertyChanged();
            }
        }

        public bool IsSectionSelectionVisible
        {
            get => _isSectionSelectionVisible;
            set
            {
                _isSectionSelectionVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsQuizVisible
        {
            get => _isQuizVisible;
            set
            {
                _isQuizVisible = value;
                OnPropertyChanged();
            }
        }

        // Properties
        public ObservableCollection<QuizQuestion> Questions
        {
            get => _questions;
            set
            {
                _questions = value;
                OnPropertyChanged();
                UpdateCurrentQuestion();
            }
        }

        public QuizQuestion CurrentQuestion
        {
            get => _currentQuestion;
            private set
            {
                _currentQuestion = value;
                OnPropertyChanged();
                UpdateOptionColors();
            }
        }

        public string QuestionProgressText => $"Question {CurrentQuestionIndex + 1} of {Questions?.Count ?? 0}";

        public double Progress => Questions?.Count > 0 ? (double)(CurrentQuestionIndex + 1) / Questions.Count : 0;

        public string ScoreText => $"Score: {Score}/{TotalAnswered}";

        public string TimerText => $"Time: {TimeRemaining:D2}:{0:D2}";

        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            set
            {
                _currentQuestionIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(QuestionProgressText));
                OnPropertyChanged(nameof(Progress));
            }
        }

        public int Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScoreText));
            }
        }

        public int TotalAnswered
        {
            get => _totalAnswered;
            set
            {
                _totalAnswered = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScoreText));
            }
        }

        public int TimeRemaining
        {
            get => _timeRemaining;
            set
            {
                _timeRemaining = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimerText));
            }
        }

        public bool IsAnswerSelected
        {
            get => _isAnswerSelected;
            set
            {
                _isAnswerSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHintButtonVisible));
                OnPropertyChanged(nameof(IsSkipButtonVisible));
                OnPropertyChanged(nameof(IsNextButtonVisible));
            }
        }

        public string SelectedAnswer
        {
            get => _selectedAnswer;
            set
            {
                _selectedAnswer = value;
                OnPropertyChanged();
            }
        }

        public bool IsFeedbackVisible
        {
            get => _isFeedbackVisible;
            set
            {
                _isFeedbackVisible = value;
                OnPropertyChanged();
            }
        }

        public string FeedbackText
        {
            get => _feedbackText;
            set
            {
                _feedbackText = value;
                OnPropertyChanged();
            }
        }

        public string ExplanationText
        {
            get => _explanationText;
            set
            {
                _explanationText = value;
                OnPropertyChanged();
            }
        }

        public bool IsExplanationVisible
        {
            get => _isExplanationVisible;
            set
            {
                _isExplanationVisible = value;
                OnPropertyChanged();
            }
        }

        // Button visibility properties
        public bool IsHintButtonVisible => !IsAnswerSelected;
        public bool IsSkipButtonVisible => !IsAnswerSelected;
        public bool IsNextButtonVisible => IsAnswerSelected;

        // Option color properties
        public string OptionABackgroundColor { get; private set; } = "#F5F5F5";
        public string OptionABorderColor { get; private set; } = "Gray";
        public string OptionBBackgroundColor { get; private set; } = "#F5F5F5";
        public string OptionBBorderColor { get; private set; } = "Gray";
        public string OptionCBackgroundColor { get; private set; } = "#F5F5F5";
        public string OptionCBorderColor { get; private set; } = "Gray";
        public string OptionDBackgroundColor { get; private set; } = "#F5F5F5";
        public string OptionDBorderColor { get; private set; } = "Gray";

        // Feedback color properties
        public string FeedbackBackgroundColor { get; private set; } = "#E8F5E8";
        public string FeedbackBorderColor { get; private set; } = "#4CAF50";
        public string FeedbackTextColor { get; private set; } = "#2E7D32";

        // Commands
        public ICommand SelectAnswerCommand { get; private set; }
        public ICommand NextQuestionCommand { get; private set; }
        public ICommand ShowHintCommand { get; private set; }
        public ICommand SkipQuestionCommand { get; private set; }
        public ICommand StartQuizCommand { get; private set; }
        public ICommand BackToSectionsCommand { get; private set; }
        public ICommand SelectSectionCommand { get; private set; }

        private void InitializeCommands()
        {
            SelectAnswerCommand = new Command<string>(SelectAnswer);
            NextQuestionCommand = new Command(NextQuestion);
            ShowHintCommand = new Command(ShowHint);
            SkipQuestionCommand = new Command(SkipQuestion);
            StartQuizCommand = new Command<QuizSection>(StartQuiz);
            BackToSectionsCommand = new Command(BackToSections);
            SelectSectionCommand = new Command<QuizSection>(SelectSection);
        }

        private void SelectSection(QuizSection section)
        {
            SelectedSection = section;
        }

        private void StartQuiz(QuizSection section)
        {
            if (section == null) return;

            SelectedSection = section;

            // Filter questions by selected section
            Questions = new ObservableCollection<QuizQuestion>(section.Questions ?? new List<QuizQuestion>());

            // Reset quiz state
            CurrentQuestionIndex = 0;
            Score = 0;
            TotalAnswered = 0;

            // Show quiz UI
            IsSectionSelectionVisible = false;
            IsQuizVisible = true;

            UpdateCurrentQuestion();
            StartTimer();
        }

        private void BackToSections()
        {
            _timer?.Dispose();
            IsSectionSelectionVisible = true;
            IsQuizVisible = false;
            ResetQuizState();
        }

        private void ResetQuizState()
        {
            IsAnswerSelected = false;
            SelectedAnswer = null;
            IsFeedbackVisible = false;
            CurrentQuestionIndex = 0;
            Score = 0;
            TotalAnswered = 0;
        }

        private void SelectAnswer(string answer)
        {
            if (IsAnswerSelected) return;

            SelectedAnswer = answer;
            IsAnswerSelected = true;
            TotalAnswered++;

            _timer?.Dispose(); // Stop the timer

            bool isCorrect = answer == CurrentQuestion.CorrectAnswer;

            if (isCorrect)
            {
                Score++;
                FeedbackText = "Correct! Well done.";
                FeedbackBackgroundColor = "#E8F5E8";
                FeedbackBorderColor = "#4CAF50";
                FeedbackTextColor = "#2E7D32";
            }
            else
            {
                FeedbackText = $"Incorrect. The correct answer is {CurrentQuestion.CorrectAnswer}.";
                FeedbackBackgroundColor = "#FFEBEE";
                FeedbackBorderColor = "#F44336";
                FeedbackTextColor = "#C62828";
            }

            ExplanationText = CurrentQuestion.Explanation;
            IsExplanationVisible = !string.IsNullOrEmpty(CurrentQuestion.Explanation);
            IsFeedbackVisible = true;

            UpdateOptionColorsAfterAnswer();
            OnPropertyChanged(nameof(FeedbackBackgroundColor));
            OnPropertyChanged(nameof(FeedbackBorderColor));
            OnPropertyChanged(nameof(FeedbackTextColor));
        }

        private void NextQuestion()
        {
            if (CurrentQuestionIndex < Questions.Count - 1)
            {
                CurrentQuestionIndex++;
                UpdateCurrentQuestion();
                ResetQuestionState();
                StartTimer();
            }
            else
            {
                // Quiz completed
                ShowQuizResults();
            }
        }

        private void ShowHint()
        {
            if (CurrentQuestion != null && !string.IsNullOrEmpty(CurrentQuestion.Hint))
            {
                // Show hint in feedback area temporarily
                FeedbackText = $"Hint: {CurrentQuestion.Hint}";
                FeedbackBackgroundColor = "#FFF3E0";
                FeedbackBorderColor = "#FF9800";
                FeedbackTextColor = "#E65100";
                IsFeedbackVisible = true;
                IsExplanationVisible = false;

                OnPropertyChanged(nameof(FeedbackBackgroundColor));
                OnPropertyChanged(nameof(FeedbackBorderColor));
                OnPropertyChanged(nameof(FeedbackTextColor));

                // Hide hint after 3 seconds
                Device.StartTimer(TimeSpan.FromSeconds(3), () =>
                {
                    IsFeedbackVisible = false;
                    return false;
                });
            }
        }

        private void SkipQuestion()
        {
            TotalAnswered++;
            NextQuestion();
        }

        private void UpdateCurrentQuestion()
        {
            if (Questions != null && CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Questions.Count)
            {
                CurrentQuestion = Questions[CurrentQuestionIndex];
            }
        }

        private void ResetQuestionState()
        {
            IsAnswerSelected = false;
            SelectedAnswer = null;
            IsFeedbackVisible = false;
            TimeRemaining = 30;
            UpdateOptionColors();
        }

        private void UpdateOptionColors()
        {
            // Reset to default colors
            OptionABackgroundColor = "#F5F5F5";
            OptionABorderColor = "Gray";
            OptionBBackgroundColor = "#F5F5F5";
            OptionBBorderColor = "Gray";
            OptionCBackgroundColor = "#F5F5F5";
            OptionCBorderColor = "Gray";
            OptionDBackgroundColor = "#F5F5F5";
            OptionDBorderColor = "Gray";

            OnPropertyChanged(nameof(OptionABackgroundColor));
            OnPropertyChanged(nameof(OptionABorderColor));
            OnPropertyChanged(nameof(OptionBBackgroundColor));
            OnPropertyChanged(nameof(OptionBBorderColor));
            OnPropertyChanged(nameof(OptionCBackgroundColor));
            OnPropertyChanged(nameof(OptionCBorderColor));
            OnPropertyChanged(nameof(OptionDBackgroundColor));
            OnPropertyChanged(nameof(OptionDBorderColor));
        }

        private void UpdateOptionColorsAfterAnswer()
        {
            // Highlight correct answer in green
            switch (CurrentQuestion.CorrectAnswer)
            {
                case "A":
                    OptionABackgroundColor = "#E8F5E8";
                    OptionABorderColor = "#4CAF50";
                    break;
                case "B":
                    OptionBBackgroundColor = "#E8F5E8";
                    OptionBBorderColor = "#4CAF50";
                    break;
                case "C":
                    OptionCBackgroundColor = "#E8F5E8";
                    OptionCBorderColor = "#4CAF50";
                    break;
                case "D":
                    OptionDBackgroundColor = "#E8F5E8";
                    OptionDBorderColor = "#4CAF50";
                    break;
            }

            // Highlight selected wrong answer in red
            if (SelectedAnswer != CurrentQuestion.CorrectAnswer)
            {
                switch (SelectedAnswer)
                {
                    case "A":
                        OptionABackgroundColor = "#FFEBEE";
                        OptionABorderColor = "#F44336";
                        break;
                    case "B":
                        OptionBBackgroundColor = "#FFEBEE";
                        OptionBBorderColor = "#F44336";
                        break;
                    case "C":
                        OptionCBackgroundColor = "#FFEBEE";
                        OptionCBorderColor = "#F44336";
                        break;
                    case "D":
                        OptionDBackgroundColor = "#FFEBEE";
                        OptionDBorderColor = "#F44336";
                        break;
                }
            }

            OnPropertyChanged(nameof(OptionABackgroundColor));
            OnPropertyChanged(nameof(OptionABorderColor));
            OnPropertyChanged(nameof(OptionBBackgroundColor));
            OnPropertyChanged(nameof(OptionBBorderColor));
            OnPropertyChanged(nameof(OptionCBackgroundColor));
            OnPropertyChanged(nameof(OptionCBorderColor));
            OnPropertyChanged(nameof(OptionDBackgroundColor));
            OnPropertyChanged(nameof(OptionDBorderColor));
        }

        private void StartTimer()
        {
            _timer?.Dispose();
            TimeRemaining = 30;

            _timer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void TimerCallback(object state)
        {
            TimeRemaining--;

            if (TimeRemaining <= 0)
            {
                _timer?.Dispose();

                // Auto-skip question when time runs out
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (!IsAnswerSelected)
                    {
                        SkipQuestion();
                    }
                });
            }
        }

        private void ShowQuizResults()
        {
            // Navigate to results page or show results dialog
            var accuracy = TotalAnswered > 0 ? (Score * 100.0 / TotalAnswered) : 0;
            var message = $"Quiz: {SelectedSection?.Name}\n" +
                         $"Your final score: {Score}/{TotalAnswered}\n" +
                         $"Accuracy: {accuracy:F1}%";

            Application.Current.MainPage.DisplayAlert("Quiz Complete", message, "OK");

            // Return to section selection
            BackToSections();
        }
        public void AddQuestion(QuizQuestion question)
        {
            if (question != null)
            {
                // Set category and difficulty if not already set
                if (string.IsNullOrEmpty(question.Category))
                    question.Category = "AI Generated";

                if (string.IsNullOrEmpty(question.Difficulty))
                    question.Difficulty = "Medium";

                Questions.Add(question);
                OnPropertyChanged(nameof(Questions));

                // Optional: Save to local storage or database
                SaveQuestionToStorage(question);
            }
        }

        public void AddQuestions(IEnumerable<QuizQuestion> questions)
        {
            foreach (var question in questions)
            {
                AddQuestion(question);
            }
        }

        public void RemoveQuestion(QuizQuestion question)
        {
            if (Questions.Contains(question))
            {
                Questions.Remove(question);
                OnPropertyChanged(nameof(Questions));

                // Optional: Remove from storage
                RemoveQuestionFromStorage(question);
            }
        }

        public void ClearAllQuestions()
        {
            Questions.Clear();
            OnPropertyChanged(nameof(Questions));

            // Optional: Clear storage
            ClearQuestionsFromStorage();
        }

        // Optional: Persistence methods (implement based on your storage solution)
        private void SaveQuestionToStorage(QuizQuestion question)
        {
            // Implement your storage logic here
            // Could be SQLite, Preferences, file system, etc.
        }

        private void SaveSectionToStorage(QuizSection questionSection)
        {
            // Implement your storage logic here
            // Could be SQLite, Preferences, file system, etc.
        }

        private void RemoveQuestionFromStorage(QuizQuestion question)
        {
            // Implement removal from storage
        }

        private void RemoveSectionFromStorage(QuizSection questionSection)
        {
            // Implement your storage logic here
            // Could be SQLite, Preferences, file system, etc.
        }

        private void ClearQuestionsFromStorage()
        {
            // Implement clearing storage
        }

        // Method to load existing questions if you have persistence
        public async Task LoadQuestionsAsync()
        {
            // Implement loading from storage
            // var savedQuestions = await LoadFromStorage();
            // foreach (var question in savedQuestions)
            // {
            //     Questions.Add(question);
            // }
        }

        // Optional: Method to export/share generated questions
        public async Task<string> ExportQuestionsAsync()
        {
            var json = JsonConvert.SerializeObject(Questions, Formatting.Indented);
            return json;
        }

        // Optional: Method to import questions
        public async Task ImportQuestionsAsync(string json)
        {
            try
            {
                var importedQuestions = JsonConvert.DeserializeObject<List<QuizQuestion>>(json);
                if (importedQuestions != null)
                {
                    AddQuestions(importedQuestions);
                }
            }
            catch (Exception ex)
            {
                // Handle import error
                throw new Exception($"Failed to import questions: {ex.Message}");
            }
        }

        public QuizSection AddQuizSection(string topic, string difficulty = "Medium")
        {
            // Generate a clean section name from the topic
            var sectionName = GenerateSectionName(topic);

            var newSection = new QuizSection
            {
                Id = Guid.NewGuid().ToString(),
                Name = topic,
                Color = "#006F8D",
                Description = $"Quiz about {topic}",
                Difficulty = difficulty,
                Questions = new List<QuizQuestion>()
            };

            QuizSections.Add(newSection);
            OnPropertyChanged(nameof(QuizSections));

            // Optional: Save to storage
            SaveSectionToStorage(newSection);

            return newSection;
        }

        private string GenerateSectionName(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return $"Quiz {DateTime.Now:MMdd-HHmm}";

            // Clean up the topic
            var cleaned = topic.Trim();

            // Capitalize first letter
            if (cleaned.Length > 0)
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);

            // Add "Quiz" if not already present
            if (!cleaned.ToLower().Contains("quiz"))
                cleaned += " Quiz";

            // Handle duplicate names
            var existingNames = QuizSections.Select(s => s.Name).ToHashSet();
            var originalName = cleaned;
            var counter = 1;

            while (existingNames.Contains(cleaned))
            {
                cleaned = $"{originalName} ({counter++})";
            }

            return cleaned;
        }

        public void AddQuestionsToSection(QuizSection section, List<QuizQuestion> questions)
        {
            foreach (var question in questions)
            {
                if (question != null)
                {
                    section.Questions.Add(question);
                }
            }
            section.QuestionCount = questions.Count;
            OnPropertyChanged(nameof(QuizSections));
        }





        public void Dispose()
        {
            _timer?.Dispose();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    
}