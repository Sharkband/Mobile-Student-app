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
    private readonly QuizViewModel _quizViewModel; // Reference to existing quiz view model
    private readonly FlashcardViewModel _flashCardViewModel; // Reference to existing flash card view model

    private string _currentMessage = string.Empty;
    private bool _isLoading = false;
    private bool _isGeneratingQuiz = false;

    public AIChatViewModel(IAIChatService aiChatService, QuizViewModel quizViewModel, FlashcardViewModel flashcardViewModel)
    {
        _aiChatService = aiChatService;
        _quizViewModel = quizViewModel;
        _flashCardViewModel = flashcardViewModel;

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

    public bool IsNotLoading => !IsLoading;

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
                    Content = response ?? "I received an empty response. Please try again.",
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            var errorMessage = GetSafeErrorMessage(ex);
            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"Sorry, I encountered an error: {errorMessage}",
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
        if (string.IsNullOrWhiteSpace(message))
            return false;

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
            System.Diagnostics.Debug.WriteLine("=== Starting HandleQuizGenerationRequest ===");
            System.Diagnostics.Debug.WriteLine($"User message: {userMessage}");

            // Parse the request to extract topic, number of questions, etc.
            var request = ParseQuizGenerationRequest(userMessage);
            System.Diagnostics.Debug.WriteLine($"Parsed request - Topic: {request?.Topic}, Questions: {request?.NumberOfQuestions}, Difficulty: {request?.Difficulty}");

            if (request == null)
            {
                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = "I couldn't understand your quiz request. Please try something like 'Create 5 questions about math'.",
                    Timestamp = DateTime.Now
                });
                return;
            }

            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"I'll generate {request.NumberOfQuestions} {request.Difficulty} quiz questions about {request.Topic}. This may take a moment...",
                Timestamp = DateTime.Now
            });

            System.Diagnostics.Debug.WriteLine("Calling GenerateQuizQuestionsAsync...");
            var questions = await _aiChatService.GenerateQuizQuestionsAsync(request);
            System.Diagnostics.Debug.WriteLine($"Received {questions?.Count ?? 0} questions");

            if (questions != null && questions.Any())
            {
                System.Diagnostics.Debug.WriteLine("Adding questions to quiz view model...");
                // Add questions to your quiz view model
                foreach (var question in questions)
                {
                    if (question != null)
                    {
                        _quizViewModel.AddQuestion(question);
                        _flashCardViewModel.addFlashCard(question);
                    }
                }

                var newSection = _quizViewModel.AddQuizSection(request.Topic, request.Difficulty);

                _quizViewModel.AddQuestionsToSection(newSection, questions);

                ChatMessages.Add(new ChatMessage
                {
                    Role = "assistant",
                    Content = $"Great! I've generated {questions.Count} quiz questions about {request.Topic} and added them to your quiz. You can now use them in your quiz!",
                    Timestamp = DateTime.Now
                });

                // Optionally show a preview of the questions
                try
                {
                    var validQuestions = questions.Where(q => q != null && !string.IsNullOrEmpty(q.Question) && q.Options != null && q.Options.Count >= 4).Take(2);
                    if (validQuestions.Any())
                    {
                        var preview = string.Join("\n\n", validQuestions.Select((q, i) =>
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
                }
                catch (Exception previewEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating preview: {previewEx.Message}");
                    // Don't show preview if there's an error, but continue
                }
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
            System.Diagnostics.Debug.WriteLine($"ERROR in HandleQuizGenerationRequest: {ex?.GetType()?.Name}: {ex?.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex?.StackTrace}");

            var errorMessage = GetSafeErrorMessage(ex);
            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"Sorry, I had trouble generating the quiz questions: {errorMessage}",
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
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                topic = "general knowledge";
            }

            var request = new QuizGenerationRequest
            {
                Topic = topic,
                NumberOfQuestions = 5,
                Difficulty = "medium",
                QuestionType = "multiple-choice"
            };

            await HandleQuizGenerationRequest($"Create {request.NumberOfQuestions} {request.Difficulty} questions about {topic}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR in GenerateQuizAsync: {ex?.Message}");
            var errorMessage = GetSafeErrorMessage(ex);
            ChatMessages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = $"Error generating quiz: {errorMessage}",
                Timestamp = DateTime.Now
            });
        }
    }

    private QuizGenerationRequest ParseQuizGenerationRequest(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

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
                if (index >= 0 && index + keyword.Length < message.Length)
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing quiz request: {ex?.Message}");
            return new QuizGenerationRequest
            {
                Topic = "general knowledge",
                NumberOfQuestions = 5,
                Difficulty = "medium",
                QuestionType = "multiple-choice"
            };
        }
    }

    private string GetSafeErrorMessage(Exception ex)
    {
        try
        {
            if (ex == null)
                return "Unknown error occurred";

            var message = ex.Message ?? "No error message available";

            // Limit message length to prevent UI issues
            if (message.Length > 200)
                message = message.Substring(0, 200) + "...";

            return message;
        }
        catch
        {
            return "Error processing error message";
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
