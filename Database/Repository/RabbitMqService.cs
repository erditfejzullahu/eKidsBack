using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database.DTOs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Database.Repository
{
    public class RabbitMqService
    {
        private readonly string _hostName = "http://192.168.1.20";
        private readonly string _queueName = "aiBlogContentGeneration";
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(ILogger<RabbitMqService> logger)
        {
            _logger = logger;
        }
        public async void SendMessage(AIMessageGenerationDto generationDto)
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

                    var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(generationDto));

                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _queueName,
                        body: body);

                    _logger.LogInformation("Message sent to RabbitMQ queue.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while sending message to RabbitMQ: {ex.Message}");
            }

        }
    }
}
