using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using OPCLibrary.BaseNodeClass;
using OPCLibrary.NodeManager;

namespace OPCLibrary.BaseNodeClass
{
    public class RealTimeNodeService
    {
        private readonly IMqttClient _mqttClient;
        private readonly INodeManager _nodeManager;

        public RealTimeNodeService(INodeManager nodeManager)
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            _nodeManager = nodeManager;
        }

        public async Task StartAsync(string brokerAddress, string topic)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerAddress)
                .Build();

            _mqttClient.UseConnectedHandler(async e =>
            {
                Console.WriteLine("Підключено до MQTT брокера.");
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                Console.WriteLine($"Підписано на топік: {topic}");
            });

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine($"Отримано повідомлення: {payload}");

                try
                {
                    var nodeData = JsonSerializer.Deserialize<NodeValueReader>(payload);
                    if (nodeData != null)
                    {
                        Console.WriteLine($"NodeId: {nodeData.NodeId}, Значення: {nodeData.Value}");
                        _nodeManager.UpdateNode(nodeData.NodeId, nodeData.Value); // Збереження даних у NodeManager
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка десеріалізації повідомлення: {ex.Message}");
                }
            });

            await _mqttClient.ConnectAsync(options);
        }

        public async Task StopAsync()
        {
            await _mqttClient.DisconnectAsync();
            Console.WriteLine("Відключено від MQTT брокера.");
        }
    }
}