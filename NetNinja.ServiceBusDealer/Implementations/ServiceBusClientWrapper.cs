using System.Text.Json;
using Azure.Messaging.ServiceBus;
using NetNinja.ServiceBusDealer.Configurations;
using NetNinja.ServiceBusDealer.Contracts;

namespace NetNinja.ServiceBusDealer.Implementations
{
    public class ServiceBusClientWrapper<T> : IServiceBusClientWrapper<T>
    {
        public ServiceBusClient Client { get; set; }
        public ServiceBusSender Sender { get; set; }

        public ServiceBusClientWrapper(ServiceBusConfiguration config, ServiceBusClient? client = null, ServiceBusSender? sender = null)
        {
            Client = client ?? new ServiceBusClient(config.ConnectionString);
            Sender = sender ?? Client.CreateSender(config.QueueName);
            InitializeClient(config);
        }

        private void InitializeClient(ServiceBusConfiguration config)
        {
            Client = new ServiceBusClient(config.ConnectionString);
            Sender = Client.CreateSender(config.QueueName);
        }

        #region Unique Message

        public async Task SendListAsMessage(List<T> messages)
        {
            try
            {
                var messageSerialized = SerializeMessages(messages);

                var busMessage = new ServiceBusMessage(messageSerialized);
                
                await Sender.SendMessageAsync(busMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task SendMessageAsync(T message)
        {
            try
            {
                var messageSerialized = SerializeMessage(message);
                
                var busMessage = new ServiceBusMessage(messageSerialized);
                
                await Sender.SendMessageAsync(busMessage);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion

        #region Several Messages

        public async Task SendMessagesAsync( List<T> messages )
        {
            try
            {
                IList<ServiceBusMessage> packedMessages = new List<ServiceBusMessage>();

                foreach (var message in messages)
                {
                    packedMessages.Add( new ServiceBusMessage( SerializeMessage( message ) ) );
                }

                await Sender.SendMessagesAsync( packedMessages );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public async Task SendBatchOfMessagesAsync( List<T> messagesToBatch )
        {
            try
            {
                var messages = SerializeBatchMessages(messagesToBatch);

                int messageCount = messages.Count;

                while (messages.Count > 0)
                {
                    using ServiceBusMessageBatch messageBatch = await Sender.CreateMessageBatchAsync();

                    if (messageBatch.TryAddMessage(messages.Peek()))
                    {
                        messages.Dequeue();
                    }
                    else
                    {
                        throw new Exception(
                            $"Message {messageCount - messages.Count} is too large and cannot be sent.");
                    }

                    while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                    {
                        messages.Dequeue();
                    }

                    await Sender.SendMessagesAsync(messageBatch);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        #endregion
        
        

        #region Private Methods

        private Queue<ServiceBusMessage> SerializeBatchMessages(List<T> messages)
        {
            Queue<ServiceBusMessage> messagesForBatch = new();

            foreach (var message in messages)
            {
                var serializedMessage = SerializeMessage(message);
                
                var busMessage = new ServiceBusMessage(serializedMessage);
                
                messagesForBatch.Enqueue( busMessage );
            }

            return messagesForBatch;
        }
        private string SerializeMessage(T message)
        {
            var jsonString = JsonSerializer.Serialize(message);

            return jsonString;
        }
        
        private string SerializeMessages(List<T> messages)
        {
            var jsonString = JsonSerializer.Serialize(messages);

            return jsonString;
        }

        #endregion
    }
};

