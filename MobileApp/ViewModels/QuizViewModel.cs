using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

using MobileApp.Models;

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
            InitializeQuizSections();
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

        private void InitializeQuizSections()
        {
            QuizSections = new ObservableCollection<QuizSection>
            {
                new QuizSection
                {
                    Id = "general",
                    Name = "General Knowledge",
                    Description = "Mixed topics covering various subjects",
                    Icon = "🧠",
                    Color = "#2196F3",
                    QuestionCount = 5,
                    Difficulty = "Mixed",
                    Questions = GetGeneralQuestions()
                },
                new QuizSection
                {
                    Id = "science",
                    Name = "Science",
                    Description = "Physics, Chemistry, Biology, and Earth Science",
                    Icon = "🔬",
                    Color = "#4CAF50",
                    QuestionCount = 4,
                    Difficulty = "Medium",
                    Questions = GetScienceQuestions()
                },
                new QuizSection
                {
                    Id = "history",
                    Name = "History",
                    Description = "World history and historical events",
                    Icon = "📚",
                    Color = "#FF9800",
                    QuestionCount = 3,
                    Difficulty = "Hard",
                    Questions = GetHistoryQuestions()
                },
                new QuizSection
                {
                    Id = "geography",
                    Name = "Geography",
                    Description = "Countries, capitals, and world geography",
                    Icon = "🌍",
                    Color = "#9C27B0",
                    QuestionCount = 3,
                    Difficulty = "Easy",
                    Questions = GetGeographyQuestions()
                },
                new QuizSection
                {
                    Id = "literature",
                    Name = "Literature",
                    Description = "Famous books, authors, and literary works",
                    Icon = "📖",
                    Color = "#E91E63",
                    QuestionCount = 2,
                    Difficulty = "Medium",
                    Questions = GetLiteratureQuestions()
                }
            };

            
        }

        

        public List<QuizQuestion> GetGeneralQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "What is 2 + 2?",
                    OptionA = "3",
                    OptionB = "4",
                    OptionC = "5",
                    OptionD = "6",
                    CorrectAnswer = "B",
                    Explanation = "Basic addition: 2 + 2 = 4",
                    Hint = "Count on your fingers!",
                    Category = "general",
                    Difficulty = 1
                },
                new QuizQuestion
                {
                    QuestionText = "How many days are in a leap year?",
                    OptionA = "365",
                    OptionB = "366",
                    OptionC = "367",
                    OptionD = "364",
                    CorrectAnswer = "B",
                    Explanation = "A leap year has 366 days, with February having 29 days instead of 28.",
                    Hint = "It happens every 4 years.",
                    Category = "general",
                    Difficulty = 1
                },
                new QuizQuestion
                {
                    QuestionText = "What is the largest mammal in the world?",
                    OptionA = "Elephant",
                    OptionB = "Blue Whale",
                    OptionC = "Giraffe",
                    OptionD = "Hippo",
                    CorrectAnswer = "B",
                    Explanation = "The Blue Whale is the largest mammal and largest animal ever known to have lived on Earth.",
                    Hint = "It lives in the ocean.",
                    Category = "general",
                    Difficulty = 2
                },
                new QuizQuestion
                {
                    QuestionText = "Which programming language is known as the 'mother of all languages'?",
                    OptionA = "C",
                    OptionB = "Assembly",
                    OptionC = "Fortran",
                    OptionD = "COBOL",
                    CorrectAnswer = "A",
                    Explanation = "C is often called the mother of modern programming languages due to its influence.",
                    Hint = "It's a single letter.",
                    Category = "general",
                    Difficulty = 3
                },
                new QuizQuestion
                {
                    QuestionText = "What does 'www' stand for?",
                    OptionA = "World Wide Web",
                    OptionB = "World Wide Wire",
                    OptionC = "World Wide Window",
                    OptionD = "World Wide Wireless",
                    CorrectAnswer = "A",
                    Explanation = "WWW stands for World Wide Web, the information system on the Internet.",
                    Hint = "You use it to browse the internet.",
                    Category = "general",
                    Difficulty = 1
                }
            };
        }

        public List<QuizQuestion> GetScienceQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "What is the chemical symbol for gold?",
                    OptionA = "Go",
                    OptionB = "Gd",
                    OptionC = "Au",
                    OptionD = "Ag",
                    CorrectAnswer = "C",
                    Explanation = "Au comes from the Latin word 'aurum'.",
                    Hint = "It comes from Latin.",
                    Category = "science",
                    Difficulty = 2
                },
                new QuizQuestion
                {
                    QuestionText = "What is the largest planet in our solar system?",
                    OptionA = "Earth",
                    OptionB = "Jupiter",
                    OptionC = "Saturn",
                    OptionD = "Neptune",
                    CorrectAnswer = "B",
                    Explanation = "Jupiter is the largest planet, with a mass greater than all other planets combined.",
                    Hint = "It has a famous red spot.",
                    Category = "science",
                    Difficulty = 1
                }
            };
        }
        public List<QuizQuestion> GetHistoryQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "In what year did World War II end?",
                    OptionA = "1944",
                    OptionB = "1945",
                    OptionC = "1946",
                    OptionD = "1947",
                    CorrectAnswer = "B",
                    Explanation = "World War II ended in 1945 with the surrender of Japan in September.",
                    Hint = "The atomic bombs were dropped this year.",
                    Category = "history",
                    Difficulty = 2
                },
                new QuizQuestion
                {
                    QuestionText = "Who was the first person to walk on the moon?",
                    OptionA = "Buzz Aldrin",
                    OptionB = "Neil Armstrong",
                    OptionC = "John Glenn",
                    OptionD = "Alan Shepard",
                    CorrectAnswer = "B",
                    Explanation = "Neil Armstrong was the first person to walk on the moon on July 20, 1969.",
                    Hint = "He said 'That's one small step for man...'",
                    Category = "history",
                    Difficulty = 2
                }
            };
        }

        public List<QuizQuestion> GetGeographyQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "What is the capital of France?",
                    OptionA = "Paris",
                    OptionB = "London",
                    OptionC = "Berlin",
                    OptionD = "Madrid",
                    CorrectAnswer = "A",
                    Explanation = "Paris is the capital and largest city of France.",
                    Hint = "It's known as the City of Light.",
                    Category = "geography",
                    Difficulty = 1
                },
                new QuizQuestion
                {
                    QuestionText = "Which is the longest river in the world?",
                    OptionA = "Amazon River",
                    OptionB = "Nile River",
                    OptionC = "Mississippi River",
                    OptionD = "Yangtze River",
                    CorrectAnswer = "B",
                    Explanation = "The Nile River is generally considered the longest river in the world at about 6,650 km.",
                    Hint = "It flows through Egypt.",
                    Category = "geography",
                    Difficulty = 2
                }
            };
        }

        public List<QuizQuestion> GetLiteratureQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    QuestionText = "Who wrote Romeo and Juliet?",
                    OptionA = "Charles Dickens",
                    OptionB = "Jane Austen",
                    OptionC = "William Shakespeare",
                    OptionD = "Mark Twain",
                    CorrectAnswer = "C",
                    Explanation = "William Shakespeare wrote this famous tragedy in the early part of his career.",
                    Hint = "This playwright is from Stratford-upon-Avon.",
                    Category = "literature",
                    Difficulty = 2
                },
                new QuizQuestion
                {
                    QuestionText = "Who wrote Romeo and Juliet?",
                    OptionA = "Charles Dickens",
                    OptionB = "Jane Austen",
                    OptionC = "William Shakespeare",
                    OptionD = "Mark Twain",
                    CorrectAnswer = "C",
                    Explanation = "William Shakespeare wrote this famous tragedy in the early part of his career.",
                    Hint = "This playwright is from Stratford-upon-Avon.",
                    Category = "literature",
                    Difficulty = 2
                }
            };
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