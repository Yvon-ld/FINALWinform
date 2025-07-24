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
        private const string ApiKey = "96e79e1e-413f-4f7c-a572-023742c3801c"; // �滻Ϊ��Ļ�ɽ����API Key

        public DeepSeekService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        }

        public async Task<string> GetTrainingRecommendationsAsync(int playerId, string playerStats, string postureAnalysis)
        {
            var prompt = $@"����һ��רҵ��������������������Ա���ݣ�������ϸ��רҵ�����Ի���ѵ�����飺
            ��ԱID: {playerId}
            ͳ������:
            {playerStats}
            ���Ʒ���:
            {postureAnalysis}
            �������
            1. Ͷ�������Ľ�����
            2. ��������ר��ѵ������
            3. ƣ����ָ�����
            4. ѵ���ƻ�����
            �����������������������";

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

            return responseObj?.choices?[0]?.message?.content ?? "�޷���ȡ����";
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