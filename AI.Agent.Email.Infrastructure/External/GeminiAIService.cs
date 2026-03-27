using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI.Agent.Email.Infrastructure.External;

public class GeminiAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAIService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey");
        _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    }

    public async Task<AIAnalysisResult> AnalyzeEmailAsync(string subject, string body)
    {
        try
        {
            var prompt = BuildAnalysisPrompt(subject, body);
            var result = await CallGeminiApiAsync(prompt);
            return ParseAnalysisResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing email with Gemini API");
            return new AIAnalysisResult
            {
                Category = null,
                Intent = null,
                Priority = "Medium",
                Confidence = 0.0,
                SuggestedAction = "ManualReview",
                Reply = null
            };
        }
    }

    public async Task<string?> GenerateReplyAsync(string subject, string body, string tone = "professional")
    {
        try
        {
            var prompt = BuildReplyPrompt(subject, body, tone);
            var result = await CallGeminiApiAsync(prompt);
            return result?.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reply with Gemini API");
            return null;
        }
    }

    private string BuildAnalysisPrompt(string subject, string body)
    {
        return $@"Analyze the following email and return a JSON response:

Subject: {subject}
Body: {body}

Provide analysis in this exact JSON format:
{{
  ""category"": ""Important"" | ""Spam"" | ""Work"" | ""Personal"" | ""Urgent"",
  ""intent"": ""MeetingRequest"" | ""TaskAssignment"" | ""FollowUp"" | ""Complaint"" | ""GeneralInquiry"",
  ""priority"": ""High"" | ""Medium"" | ""Low"",
  ""confidence"": 0.0 to 1.0,
  ""suggestedAction"": ""Reply"" | ""ScheduleMeeting"" | ""CreateTask"" | ""SetReminder"" | ""Ignore"",
  ""reply"": ""Generated professional reply or null if not applicable"",
  ""extractedData"": {{
    ""meetingDate"": ""ISO 8601 date or null"",
    ""taskTitle"": ""Extracted task title or null"",
    ""followUpDate"": ""ISO 8601 date or null""
  }}
}}

Instructions:
- category: Classify the email importance
- intent: Identify the primary purpose
- priority: Assess urgency (High = immediate action needed)
- confidence: Your certainty level (0.0-1.0)
- suggestedAction: Best automated response
- reply: Professional response text if applicable
- extractedData: Extract dates, tasks, deadlines mentioned";
    }

    private string BuildReplyPrompt(string subject, string body, string tone)
    {
        return $@"Write a {tone} email reply to:

Original Subject: {subject}
Original Body: {body}

Write a concise, professional reply. Keep it under 200 words. Only return the reply text, no JSON formatting.";
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 1024,
                responseMimeType = "application/json"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"v1beta/models/{_model}:generateContent?key={_apiKey}",
            content);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

        return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
    }

    private AIAnalysisResult ParseAnalysisResult(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var result = JsonSerializer.Deserialize<AIAnalysisResult>(json, options);
            return result ?? new AIAnalysisResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Gemini response: {Response}", json);
            return new AIAnalysisResult
            {
                Category = null,
                Intent = null,
                Priority = "Medium",
                Confidence = 0.0,
                SuggestedAction = "ManualReview",
                Reply = null
            };
        }
    }
}

public class AIAnalysisResult
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    [JsonPropertyName("suggestedAction")]
    public string? SuggestedAction { get; set; }

    [JsonPropertyName("reply")]
    public string? Reply { get; set; }

    [JsonPropertyName("extractedData")]
    public ExtractedData? ExtractedData { get; set; }
}

public class ExtractedData
{
    [JsonPropertyName("meetingDate")]
    public string? MeetingDate { get; set; }

    [JsonPropertyName("taskTitle")]
    public string? TaskTitle { get; set; }

    [JsonPropertyName("followUpDate")]
    public string? FollowUpDate { get; set; }
}

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
