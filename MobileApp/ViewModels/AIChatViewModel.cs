using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using MobileApp.Services;
using MobileApp.ViewModels;
using MobileApp.Models;

public class AIChatViewModel : INotifyPropertyChanged
{
    private readonly IAIChatService _aiChatService;
    private readonly QuizViewModel _quizViewModel; // Reference to your existing quiz view model

    private string _currentMessage = string.Empty;
    private bool _isLoading = false;
    private bool _isGeneratingQuiz = false;

    public AIChatViewModel(IAIChatService aiChatService, QuizViewModel quizViewModel)
    {
        _aiChatService = aiChatService;
        _quizViewModel = quizViewModel;

        ChatMessages = new ObservableCollection<ChatMessage>();
        SendMessageCommand = new Command(async () => await SendMessageAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(CurrentMessage));
        GenerateQuizCommand = new Command<string>(async (topic) => await GenerateQuizAsync(topic), (topic) => !IsGeneratingQuiz);
        ClearChatCommand = new Command(() => ChatMessages.Clear());

        // Add welcome message
        ChatMessages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = "Hi! I'm your AI quiz assistant. I can help you create new quiz questions. Try asking me something like:\n\n• 'Create 5 math questions about algebra'\n• 'Generate quiz questions about World War 2'\n• 'Make some science questions about photosynthesis'",
            Timestamp = DateTime.Now
        });
    }

    public ObservableCollection<ChatMessage> ChatMessages { get; }

    public string CurrentMessage
    {
        get => _currentMessage;
        set
        {
            _currentMessage = value;
            OnPropertyChanged();
            ((Command)SendMessageCommand).ChangeCanExecute();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            ((Command)SendMessageCommand).ChangeCanExecute();
        }
    }

    public bool IsGeneratingQuiz
    {
        get => _isGeneratingQuiz;
        set
        {
            _isGeneratingQuiz = value;
            OnPropertyChanged();
        }
    }

    public ICommand SendMessageCommand { get; }
    public ICommand GenerateQuizCommand { get; }
    public ICommand ClearChatCommand { get; }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || IsLoading)
            return;

        var userMessage = CurrentMessage.Trim();
        CurrentMessage = string.Empty;

        // Add user message to chat
        ChatMessages.Add(new ChatMessage
        {
            Role = "user",
            Content = userMessage,
            Timestamp = DateTime.Now
        });

        IsLoading = true;

        try
        {
            // Check if user is asking for quiz generation
            if (IsQuizGenerationRequest(userMessage))
            {
                await HandleQuizGenerationRequest(userMessage);
            }
            else
            {
                // Regular chat response
                var response = await _aiChatService.SendChatMessageAsync(userMessage, ChatMessages.ToList());

                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = response,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"Sorry, I encountered an error: {ex.Message}",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool IsQuizGenerationRequest(string message)
    {
        var keywords = new[] { "create", "generate", "make", "quiz", "questions", "test" };
        var lowerMessage = message.ToLower();

        return keywords.Any(keyword => lowerMessage.Contains(keyword)) &&
               (lowerMessage.Contains("question") || lowerMessage.Contains("quiz"));
    }

    private async Task HandleQuizGenerationRequest(string userMessage)
    {
        IsGeneratingQuiz = true;

        try
        {
            // Parse the request to extract topic, number of questions, etc.
            var request = ParseQuizGenerationRequest(userMessage);

            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"I'll generate {request.NumberOfQuestions} {request.Difficulty} quiz questions about {request.Topic}. This may take a moment...",
                Timestamp = DateTime.Now
            });

            var questions = await _aiChatService.GenerateQuizQuestionsAsync(request);

            if (questions.Any())
            {
                // Add questions to your quiz view model
                foreach (var question in questions)
                {
                    _quizViewModel.AddQuestion(question);
                }

                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = $"Great! I've generated {questions.Count} quiz questions about {request.Topic} and added them to your quiz. You can now use them in your quiz!",
                    Timestamp = DateTime.Now
                });

                // Optionally show a preview of the questions
                var preview = string.Join("\n\n", questions.Take(2).Select((q, i) =>
                    $"Preview {i + 1}: {q.Question}\nA) {q.Options[0]}\nB) {q.Options[1]}\nC) {q.Options[2]}\nD) {q.Options[3]}"));

                if (questions.Count > 2)
                    preview += $"\n\n... and {questions.Count - 2} more questions!";

                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = preview,
                    Timestamp = DateTime.Now
                });
            }
            else
            {
                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = "I couldn't generate quiz questions from that request. Could you try rephrasing it? For example: 'Create 3 questions about biology'",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"Sorry, I had trouble generating the quiz questions: {ex.Message}",
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsGeneratingQuiz = false;
        }
    }

    private async Task GenerateQuizAsync(string topic)
    {
        var request = new QuizGenerationRequest
        {
            Topic = topic,
            NumberOfQuestions = 5,
            Difficulty = "medium",
            QuestionType = "multiple-choice"
        };

        await HandleQuizGenerationRequest($"Create {request.NumberOfQuestions} {request.Difficulty} questions about {topic}");
    }

    private QuizGenerationRequest ParseQuizGenerationRequest(string message)
    {
        var request = new QuizGenerationRequest
        {
            NumberOfQuestions = 5, // default
            Difficulty = "medium", // default
            QuestionType = "multiple-choice" // default
        };

        // Extract number of questions
        var numberMatch = System.Text.RegularExpressions.Regex.Match(message, @"\b(\d+)\b");
        if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int number))
        {
            request.NumberOfQuestions = Math.Max(1, Math.Min(10, number)); // Limit between 1-10
        }

        // Extract difficulty
        var lowerMessage = message.ToLower();
        if (lowerMessage.Contains("easy") || lowerMessage.Contains("beginner"))
            request.Difficulty = "easy";
        else if (lowerMessage.Contains("hard") || lowerMessage.Contains("difficult") || lowerMessage.Contains("advanced"))
            request.Difficulty = "hard";

        // Extract topic (everything after "about" or similar keywords)
        var topicKeywords = new[] { "about", "on", "regarding", "concerning" };
        foreach (var keyword in topicKeywords)
        {
            var index = lowerMessage.IndexOf(keyword);
            if (index >= 0)
            {
                request.Topic = message.Substring(index + keyword.Length).Trim();
                break;
            }
        }

        // If no topic found, try to extract it differently
        if (string.IsNullOrEmpty(request.Topic))
        {
            // Remove common words and take the remaining as topic
            var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commonWords = new[] { "create", "generate", "make", "quiz", "questions", "test", "some", "a", "an", "the" };
            var topicWords = words.Where(w => !commonWords.Contains(w.ToLower())).ToArray();
            request.Topic = string.Join(" ", topicWords);
        }

        if (string.IsNullOrEmpty(request.Topic))
            request.Topic = "general knowledge";

        return request;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
