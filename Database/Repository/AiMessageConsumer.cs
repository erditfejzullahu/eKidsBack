using Database.Context;
using Database.DTOs;
using Database.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class AiMessageConsumer
    {
        private readonly string _hostName = "http://192.168.1.20";
        private readonly string _queueName = "aiBlogContentGeneration";
        private readonly ILogger<AiMessageConsumer> _logger;
        private readonly ApplicationDbContext _context;

        public AiMessageConsumer(ILogger<AiMessageConsumer> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task StartConsuming()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = _hostName };

                using (var connection = await factory.CreateConnectionAsync())
                using (var channel = await connection.CreateChannelAsync())
                {
                    await channel.QueueDeclareAsync(
                        queue: _queueName,
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var messageJson = Encoding.UTF8.GetString(body);
                            var aiMessageRequest = JsonConvert.DeserializeObject<AIMessageGenerationDto>(messageJson);

                            if (aiMessageRequest == null)
                            {
                                _logger.LogWarning("Received an invalid AIMessageGenerationDto JSON");
                                return; // Do not process
                            }

                            var aiGeneratedMessage = await GenerateAiMessage(aiMessageRequest.Message);
                            var updatedBlog = await SaveGeneratedMessage(aiMessageRequest.Id, aiGeneratedMessage);

                            if (updatedBlog == null)
                            {
                                _logger.LogWarning($"Failed to update blog with ID {aiMessageRequest.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing RabbitMQ message");
                        }
                    };

                    await channel.BasicConsumeAsync(
                        queue: _queueName,
                        autoAck: true,
                        consumer: consumer);

                    _logger.LogInformation("Waiting for messages in AI Queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error in StartConsuming");
            }
        }

        private async Task<string> GenerateAiMessage(string blogContent)
        {
            //here ai api call
            await Task.Delay(2000);
            return "Ok";
        }

        private async Task<Blogs> SaveGeneratedMessage(int blogId, string generatedMessage)
        {
            try
            {
                var blog = await _context.Blogs.FindAsync(blogId);
                if(blog == null)
                {
                    throw new ApplicationException("No blog found with that id");
                }
                blog.GeneratedContent = generatedMessage;
                blog.LastModified = DateTime.UtcNow;
                _context.Blogs.Update(blog);
                await _context.SaveChangesAsync();
                return blog;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error in updating blog generated ai message");
                throw;
            }
        }
    }
}
