using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BasketballAnalyzer.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://ark.cn-beijing.volces.com/api/v3/chat/completions";
        private const string ApiKey = "96e79e1e-413f-4f7c-a572-023742c3801c"; // 替换为你的火山引擎API Key

        public DeepSeekService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public async Task<string> GetTrainingRecommendationsAsync(int playerId, string playerStats, string postureAnalysis)
        {
            var prompt = $@"你是一名专业篮球教练，请根据以下球员数据，生成详细、专业、个性化的训练建议：
            球员ID: {playerId}
            统计数据:
            {playerStats}
            姿势分析:
            {postureAnalysis}
            请给出：
            1. 投篮动作改进建议
            2. 针对弱项的专项训练建议
            3. 疲劳与恢复建议
            4. 训练计划建议
            请用条理清晰的中文输出。";

            var request = new
            {
                model = "deepseek-r1-250528",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson);

            return responseObj?.choices?[0]?.message?.content ?? "无法获取建议";
        }

        private class DeepSeekResponse
        {
            public Choice[] choices { get; set; }
        }
        
        private class Choice
        {
            public Message message { get; set; }
        }

        private class Message
        {
            public string content { get; set; }
        }
    }
}