using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using MobileApp.Models;

namespace MobileApp.Services
{
    public interface IAIChatService
    {
        Task<string> SendChatMessageAsync(string message, List<ChatMessage> conversationHistory = null);
        Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(QuizGenerationRequest request);
        Task<List<QuizQuestion>> ParseQuizFromChatResponse(string chatResponse);
    }

    public class GroqAIService : IAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl = "https://api.groq.com/openai/v1";

        public GroqAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            _apiKey = configuration?["Groq:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Groq API key is not configured. Please add 'Groq:ApiKey' to your configuration.");
            }

            _modelName = configuration["Groq:Model"] ?? "llama3-8b-8192";

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> SendChatMessageAsync(string message, List<ChatMessage> conversationHistory = null)
        {
            var messages = BuildMessagesWithHistory(message, conversationHistory);

            var requestBody = new
            {
                model = _modelName,
                messages = messages,
                max_tokens = 1024,
                temperature = 0.7,
                top_p = 1,
                stream = false
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                Debug.WriteLine("Sending request to Groq API");
                Debug.WriteLine($"{_baseUrl}/chat/completions");
                Debug.WriteLine($"Model: {_modelName}");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Response Status: {response.StatusCode}");
                Debug.WriteLine($"Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent);

                    // Add null checks to prevent NullReferenceException
                    if (result?.choices == null || result.choices.Length == 0)
                    {
                        throw new Exception("Invalid response: No choices returned from Groq API");
                    }

                    if (result.choices[0]?.message?.content == null)
                    {
                        throw new Exception("Invalid response: No content in message from Groq API");
                    }

                    return result.choices[0].message.content.Trim();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // Rate limit hit, wait and retry
                    await Task.Delay(2000);
                    return await SendChatMessageAsync(message, conversationHistory);
                }
                else
                {
                    throw new Exception($"Groq API Error: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send chat message to Groq: {ex.Message}");
            }
        }

        public async Task<List<QuizQuestion>> GenerateQuizQuestionsAsync(QuizGenerationRequest request)
        {
            var prompt = $@"Create {request.NumberOfQuestions} multiple-choice quiz questions about {request.Topic} at {request.Difficulty} difficulty level.

Format each question exactly like this:
Question: [question text]
A) [option A]
B) [option B] 
C) [option C]
D) [option D]
Correct Answer: [A/B/C/D]
Explanation: [brief explanation]
---

Important: Make sure each question is well-formatted and separated by '---'. Begin:";

            var response = await SendChatMessageAsync(prompt);
            return await ParseQuizFromChatResponse(response);
        }

        public async Task<List<QuizQuestion>> ParseQuizFromChatResponse(string chatResponse)
        {
            var questions = new List<QuizQuestion>();

            if (string.IsNullOrWhiteSpace(chatResponse))
            {
                Debug.WriteLine("Warning: Chat response is null or empty");
                return questions;
            }

            var questionBlocks = chatResponse.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in questionBlocks)
            {
                try
                {
                    var quiz = ParseSingleQuestion(block.Trim());
                    if (quiz != null)
                        questions.Add(quiz);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing question: {ex.Message}");
                }
            }

            return questions;
        }

        private QuizQuestion ParseSingleQuestion(string questionBlock)
        {
            if (string.IsNullOrWhiteSpace(questionBlock))
                return null;

            var lines = questionBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 6) return null;

            var question = new QuizQuestion
            {
                Options = new List<string>()
            };

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("Question:", StringComparison.OrdinalIgnoreCase))
                {
                    question.Question = trimmedLine.Substring(9).Trim();
                }
                else if (trimmedLine.StartsWith("A)", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("B)", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("C)", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("D)", StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmedLine.Length > 2)
                        question.Options.Add(trimmedLine.Substring(2).Trim());
                }
                else if (trimmedLine.StartsWith("Correct Answer:", StringComparison.OrdinalIgnoreCase))
                {
                    var answer = trimmedLine.Substring(15).Trim().ToUpper();
                    question.CorrectAnswerIndex = answer switch
                    {
                        "A" => 0,
                        "B" => 1,
                        "C" => 2,
                        "D" => 3,
                        _ => 0
                    };
                }
                else if (trimmedLine.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase))
                {
                    question.Explanation = trimmedLine.Substring(12).Trim();
                }
            }

            return !string.IsNullOrWhiteSpace(question.Question) && question.Options.Count == 4 ? question : null;
        }

        private List<object> BuildMessagesWithHistory(string message, List<ChatMessage> history)
        {
            var messages = new List<object>();

            // Add system message
            messages.Add(new { role = "system", content = "You are a helpful AI assistant that creates educational quiz questions. Always follow the exact formatting requested for quiz questions." });

            // Add conversation history (limit to last 10 messages to stay within context limits)
            if (history != null && history.Any())
            {
                foreach (var msg in history.TakeLast(10))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
            }

            // Add current user message
            messages.Add(new { role = "user", content = message });

            return messages;
        }
    }

    // Response models for the Groq API (same as OpenAI format)
    public class ChatCompletionResponse
    {
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
        public string model { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
        public string finish_reason { get; set; }
        public int index { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}