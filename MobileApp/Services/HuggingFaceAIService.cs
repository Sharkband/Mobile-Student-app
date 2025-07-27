using Microsoft.Extensions.Configuration;
using System;
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
    public class HuggingFaceAIService : IAIChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl = "https://api-inference.huggingface.co/models";

        public HuggingFaceAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["HuggingFace:ApiKey"]; // Free from huggingface.co/settings/tokens
            _modelName = configuration["HuggingFace:Model"] ?? "microsoft/DialoGPT-large"; // or "meta-llama/Llama-2-7b-chat-hf"

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> SendChatMessageAsync(string message, List<ChatMessage> conversationHistory = null)
        {
            var prompt = BuildPromptWithHistory(message, conversationHistory);

            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = 500,
                    temperature = 0.7,
                    do_sample = true,
                    return_full_text = false
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/{_modelName}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic[]>(responseContent);
                    return result[0].generated_text.ToString().Trim();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    // Model is loading, wait and retry
                    await Task.Delay(10000);
                    return await SendChatMessageAsync(message, conversationHistory);
                }
                else
                {
                    throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send chat message: {ex.Message}");
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

Begin:";

            var response = await SendChatMessageAsync(prompt);
            return await ParseQuizFromChatResponse(response);
        }

        public async Task<List<QuizQuestion>> ParseQuizFromChatResponse(string chatResponse)
        {
            var questions = new List<QuizQuestion>();
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
            var lines = questionBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 6) return null;

            var question = new QuizQuestion
            {
                Options = new List<string>()
            };

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("Question:"))
                {
                    question.Question = trimmedLine.Substring(9).Trim();
                }
                else if (trimmedLine.StartsWith("A)") || trimmedLine.StartsWith("B)") ||
                         trimmedLine.StartsWith("C)") || trimmedLine.StartsWith("D)"))
                {
                    question.Options.Add(trimmedLine.Substring(2).Trim());
                }
                else if (trimmedLine.StartsWith("Correct Answer:"))
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
                else if (trimmedLine.StartsWith("Explanation:"))
                {
                    question.Explanation = trimmedLine.Substring(12).Trim();
                }
            }

            return question.Question != null && question.Options.Count == 4 ? question : null;
        }

        private string BuildPromptWithHistory(string message, List<ChatMessage> history)
        {
            var prompt = "You are a helpful AI assistant that creates educational quiz questions.\n\n";

            if (history != null && history.Any())
            {
                foreach (var msg in history.TakeLast(5))
                {
                    prompt += $"{(msg.Role == "user" ? "Human" : "Assistant")}: {msg.Content}\n";
                }
            }

            prompt += $"Human: {message}\nAssistant:";
            return prompt;
        }
    }
}
