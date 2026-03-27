using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI.Agent.Email.Infrastructure.External;

public class GmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailService> _logger;

    public GmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<GmailMessage>> FetchUnreadEmailsAsync(string accessToken, int maxResults = 50)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync(
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages?q=is:unread&maxResults={maxResults}");
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var listResponse = JsonSerializer.Deserialize<GmailMessageListResponse>(json);
            
            if (listResponse?.Messages == null)
                return new List<GmailMessage>();

            var fullMessages = new List<GmailMessage>();
            foreach (var messageRef in listResponse.Messages)
            {
                var fullMessage = await GetEmailAsync(accessToken, messageRef.Id);
                if (fullMessage != null)
                    fullMessages.Add(fullMessage);
            }

            return fullMessages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching unread emails from Gmail");
            throw;
        }
    }

    public async Task<GmailMessage?> GetEmailAsync(string accessToken, string messageId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var response = await _httpClient.GetAsync(
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}");
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<GmailMessage>(json);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching email {MessageId} from Gmail", messageId);
            return null;
        }
    }

    public async Task<bool> SendReplyAsync(string accessToken, string originalMessageId, string to, string subject, string body)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            // Get original thread ID
            var originalMessage = await GetEmailAsync(accessToken, originalMessageId);
            if (originalMessage == null)
                return false;

            // Build raw email message
            var rawMessage = BuildRawReplyMessage(to, subject, body, originalMessage.ThreadId);
            
            var requestBody = new { raw = rawMessage };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                "https://gmail.googleapis.com/gmail/v1/users/me/messages/send",
                content);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reply via Gmail");
            return false;
        }
    }

    public async Task<(bool success, string accessToken, DateTime expiryTime)> RefreshAccessTokenAsync(string refreshToken)
    {
        var newAccessToken = string.Empty;
        var expiryTime = DateTime.MinValue;

        try
        {
            var clientId = _configuration["Gmail:ClientId"];
            var clientSecret = _configuration["Gmail:ClientSecret"];
            
            var requestBody = new Dictionary<string, string>
            {
                ["client_id"] = clientId ?? string.Empty,
                ["client_secret"] = clientSecret ?? string.Empty,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            };

            var content = new FormUrlEncodedContent(requestBody);
            var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);
            
            if (tokenResponse?.AccessToken != null)
            {
                newAccessToken = tokenResponse.AccessToken;
                expiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                return (true, newAccessToken, expiryTime);
            }

            return (false, string.Empty, DateTime.MinValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Gmail access token");
            return (false, string.Empty, DateTime.MinValue);
        }
    }

    private string BuildRawReplyMessage(string to, string subject, string body, string threadId)
    {
        var message = $"To: {to}\r\n" +
                       $"Subject: Re: {subject}\r\n" +
                       $"In-Reply-To: {threadId}\r\n" +
                       $"References: {threadId}\r\n" +
                       $"Content-Type: text/plain; charset=UTF-8\r\n\r\n" +
                       body;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(message))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    public static string ExtractHeaderValue(GmailMessage message, string headerName)
    {
        return message.Payload?.Headers?.FirstOrDefault(h => h.Name?.Equals(headerName, StringComparison.OrdinalIgnoreCase) == true)?.Value ?? string.Empty;
    }

    public static string ExtractBody(GmailMessage message)
    {
        if (message.Payload == null) return string.Empty;

        // Try to get text/plain body first
        var textPart = message.Payload.Parts?.FirstOrDefault(p => p.MimeType == "text/plain");
        if (textPart?.Body?.Data != null)
        {
            return DecodeBase64(textPart.Body.Data);
        }

        // Try text/html
        var htmlPart = message.Payload.Parts?.FirstOrDefault(p => p.MimeType == "text/html");
        if (htmlPart?.Body?.Data != null)
        {
            return DecodeBase64(htmlPart.Body.Data);
        }

        // Check if body is directly in payload
        if (message.Payload.Body?.Data != null)
        {
            return DecodeBase64(message.Payload.Body.Data);
        }

        return string.Empty;
    }

    private static string DecodeBase64(string input)
    {
        try
        {
            var padding = 4 - input.Length % 4;
            if (padding != 4)
            {
                input += new string('=', padding);
            }
            
            input = input.Replace('-', '+').Replace('_', '/');
            var bytes = Convert.FromBase64String(input);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return input;
        }
    }
}

// Models
public class GmailMessageListResponse
{
    [JsonPropertyName("messages")]
    public List<GmailMessageReference>? Messages { get; set; }
    
    [JsonPropertyName("nextPageToken")]
    public string? NextPageToken { get; set; }
    
    [JsonPropertyName("resultSizeEstimate")]
    public int ResultSizeEstimate { get; set; }
}

public class GmailMessageReference
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;
}

public class GmailMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;
    
    [JsonPropertyName("labelIds")]
    public List<string>? LabelIds { get; set; }
    
    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
    
    [JsonPropertyName("payload")]
    public MessagePayload? Payload { get; set; }
    
    [JsonPropertyName("internalDate")]
    public string? InternalDate { get; set; }
}

public class MessagePayload
{
    [JsonPropertyName("partId")]
    public string? PartId { get; set; }
    
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
    
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
    
    [JsonPropertyName("headers")]
    public List<MessageHeader>? Headers { get; set; }
    
    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }
    
    [JsonPropertyName("parts")]
    public List<MessagePart>? Parts { get; set; }
}

public class MessageHeader
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class MessageBody
{
    [JsonPropertyName("attachmentId")]
    public string? AttachmentId { get; set; }
    
    [JsonPropertyName("size")]
    public int Size { get; set; }
    
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

public class MessagePart
{
    [JsonPropertyName("partId")]
    public string? PartId { get; set; }
    
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
    
    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
    
    [JsonPropertyName("headers")]
    public List<MessageHeader>? Headers { get; set; }
    
    [JsonPropertyName("body")]
    public MessageBody? Body { get; set; }
    
    [JsonPropertyName("parts")]
    public List<MessagePart>? Parts { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}
