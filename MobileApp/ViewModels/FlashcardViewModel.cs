using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using MobileApp.Models;

namespace MobileApp.ViewModels
{
    public class FlashcardViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Flashcard> _flashcards;
        private int _currentCardIndex;
        private bool _isShowingFront = true;
        private Flashcard _currentCard;

        public FlashcardViewModel()
        {
            // Initialize with sample data
            InitializeFlashcards();

            // Initialize commands
            FlipCardCommand = new Command(FlipCard);
            NextCardCommand = new Command(NextCard, () => CanGoNext);
            PreviousCardCommand = new Command(PreviousCard, () => CanGoPrevious);
            ShuffleCardsCommand = new Command(ShuffleCards);
            MarkAsKnownCommand = new Command(MarkAsKnown);
            MarkForPracticeCommand = new Command(MarkForPractice);

            UpdateCurrentCard();
        }

        // Properties
        public ObservableCollection<Flashcard> Flashcards
        {
            get => _flashcards;
            set
            {
                _flashcards = value;
                OnPropertyChanged();
                UpdateCurrentCard();
            }
        }

        public Flashcard CurrentCard
        {
            get => _currentCard;
            private set
            {
                _currentCard = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentCardContent));
            }
        }

        public string CurrentCardContent => IsShowingFront ? CurrentCard?.Front : CurrentCard?.Back;

        public string CardSideText => IsShowingFront ? "FRONT" : "BACK";

        public string ProgressText => $"Card {CurrentCardIndex + 1} of {Flashcards?.Count ?? 0}";

        public double Progress => Flashcards?.Count > 0 ? (double)(CurrentCardIndex + 1) / Flashcards.Count : 0;

        public bool IsShowingFront
        {
            get => _isShowingFront;
            set
            {
                _isShowingFront = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentCardContent));
                OnPropertyChanged(nameof(CardSideText));
            }
        }

        public int CurrentCardIndex
        {
            get => _currentCardIndex;
            set
            {
                _currentCardIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
                ((Command)NextCardCommand).ChangeCanExecute();
                ((Command)PreviousCardCommand).ChangeCanExecute();
            }
        }

        public bool CanGoNext => CurrentCardIndex < (Flashcards?.Count - 1 ?? 0);
        public bool CanGoPrevious => CurrentCardIndex > 0;

        // Commands
        public ICommand FlipCardCommand { get; }
        public ICommand NextCardCommand { get; }
        public ICommand PreviousCardCommand { get; }
        public ICommand ShuffleCardsCommand { get; }
        public ICommand MarkAsKnownCommand { get; }
        public ICommand MarkForPracticeCommand { get; }

        // Command implementations
        private void FlipCard()
        {
            IsShowingFront = !IsShowingFront;
        }

        private void NextCard()
        {
            if (CanGoNext)
            {
                CurrentCardIndex++;
                IsShowingFront = true;
                UpdateCurrentCard();
            }
        }

        private void PreviousCard()
        {
            if (CanGoPrevious)
            {
                CurrentCardIndex--;
                IsShowingFront = true;
                UpdateCurrentCard();
            }
        }

        private void ShuffleCards()
        {
            if (Flashcards == null || Flashcards.Count <= 1) return;

            var random = new Random();
            var shuffled = Flashcards.OrderBy(x => random.Next()).ToList();

            Flashcards.Clear();
            foreach (var card in shuffled)
            {
                Flashcards.Add(card);
            }

            CurrentCardIndex = 0;
            IsShowingFront = true;
            UpdateCurrentCard();
        }

        private void MarkAsKnown()
        {
            if (CurrentCard != null)
            {
                CurrentCard.IsKnown = true;
                CurrentCard.NeedsPractice = false;

                // Move to next card if available
                if (CanGoNext)
                {
                    NextCard();
                }
            }
        }

        private void MarkForPractice()
        {
            if (CurrentCard != null)
            {
                CurrentCard.NeedsPractice = true;
                CurrentCard.IsKnown = false;

                // Move to next card if available
                if (CanGoNext)
                {
                    NextCard();
                }
            }
        }

        private void UpdateCurrentCard()
        {
            if (Flashcards != null && CurrentCardIndex >= 0 && CurrentCardIndex < Flashcards.Count)
            {
                CurrentCard = Flashcards[CurrentCardIndex];
            }
        }

        private void InitializeFlashcards()
        {
            Flashcards = new ObservableCollection<Flashcard>
            {
                new Flashcard { Front = "What is the capital of France?", Back = "Paris" },
                new Flashcard { Front = "What is 2 + 2?", Back = "4" },
                new Flashcard { Front = "Who wrote Romeo and Juliet?", Back = "William Shakespeare" },
                new Flashcard { Front = "What is the largest planet?", Back = "Jupiter" },
                new Flashcard { Front = "What year did World War II end?", Back = "1945" },
                new Flashcard { Front = "What is the chemical symbol for gold?", Back = "Au" },
                new Flashcard { Front = "How many continents are there?", Back = "7" },
                new Flashcard { Front = "What is the speed of light?", Back = "299,792,458 m/s" },
                new Flashcard { Front = "Who painted the Mona Lisa?", Back = "Leonardo da Vinci" },
                new Flashcard { Front = "What is the smallest prime number?", Back = "2" }
            };
        }

        public void addFlashCard(QuizQuestion question)
        {
            Flashcards.Add(new Flashcard { Front = question.Question, Back = question.CorrectAnswer });


        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Flashcard model
    public class Flashcard
    {
        public string Front { get; set; }
        public string Back { get; set; }
        public bool IsKnown { get; set; }
        public bool NeedsPractice { get; set; }
        public DateTime LastReviewed { get; set; }
        public int ReviewCount { get; set; }
    }
}
