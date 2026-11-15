using LocalAiLibrary.AiLibrary;
using LocalAiLibrary.AiLibrary.AiSessionService;
using LocalAiLibrary.AiLibrary.AITools.Sentiment;
using LocalAiLibrary.AiLibrary.ChatService.Models;
using Microsoft.Extensions.Options;
using Utilities.FileSystem;

namespace SentimentService
{
    public class AiSentimentService(IAiManager aiManager, IFileSystemManager fileSystemManager, IOptions<AiConfig> config,
        IChatSessionService chatSesionService) : IAiSentimentService
    {
        private readonly AiConfig _config = config.Value;

        public async Task ProcessCustomerReviewList()
        {
            chatSesionService.SetServiceName("Sentiment");
            string sentimentCleanup = await GetPrompt(_config.Sentiment.SentimentCleanupPromptLocation);
            string sentimentPrompt = await GetPrompt(_config.Sentiment.SentimentJsonPromptLocation);
            string customerReviewsPath = _config.Sentiment.ReadFilePath;
            List<string> fileList = fileSystemManager.GetFiles(customerReviewsPath);

            foreach (var item in fileList)
            {
                await ProcessReview(item, sentimentPrompt);
            }

            await FinishSentimentProcessing(sentimentCleanup);
        }

        private async Task ProcessReview(string reviewItem, string prompt)
        {
            string fileName = Path.GetFileName(reviewItem);
            string sentimentPrompt = prompt.Replace("#CUSTOMERREVIEW#", fileName);
            List<ChatRequestMessage> conversationHistory = new List<ChatRequestMessage>();
            await aiManager.MakeAiCallAsync(conversationHistory, sentimentPrompt);
        }

        private async Task FinishSentimentProcessing(string sentimentCleanup)
        {
            string processedPath = _config.Sentiment.DeleteFilePath;
            fileSystemManager.DeleteAllFiles(processedPath);
            List<ChatRequestMessage> conversationHistory = new List<ChatRequestMessage>();
            await aiManager.MakeAiCallAsync(conversationHistory, sentimentCleanup);
        }

        private async Task<string> GetPrompt(string path)
        {
            return await fileSystemManager.ReadTextFile(path);
        }
    }
}