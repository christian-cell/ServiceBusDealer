using System.Text.Json;
using Azure.Messaging.ServiceBus;
using NetNinja.ServiceBusDealer.Configurations;
using NetNinja.ServiceBusDealer.Contracts;
using Action = NetNinja.ServiceBusDealer.Models.Enums.Action;

namespace NetNinja.ServiceBusDealer.Implementations
{
    public class ServiceBusClientWrapper<T> : IServiceBusClientWrapper<T>
    {
        public ServiceBusClient Client { get; set; }
        public ServiceBusSender Sender { get; set; }
        private readonly ServiceBusConfiguration _serviceBusConfiguration;

        public ServiceBusClientWrapper(ServiceBusConfiguration config, ServiceBusClient? client = null, ServiceBusSender? sender = null)
        {
            Client = client ?? new ServiceBusClient(config.ConnectionString);
            Sender = sender ?? Client.CreateSender(config.QueueName);
            _serviceBusConfiguration = config;
            // InitializeClient(config);
        }

        /*private void InitializeClient(ServiceBusConfiguration config)
        {
            Client = new ServiceBusClient(config.ConnectionString);
            Sender = Client.CreateSender(config.QueueName);
        }*/

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

        public async Task<List<string>> ReceiveBatchOfMessages(int maxMessagesToReceive)
        {
            try
            {
                ServiceBusReceiver receiver = Client.CreateReceiver(_serviceBusConfiguration.QueueName);
                
                var maxWaitTime = TimeSpan.FromSeconds(15); 

                IReadOnlyList<ServiceBusReceivedMessage> receivedMessages = await receiver.ReceiveMessagesAsync(maxMessages: 2 , maxWaitTime );

                var messagesReceived = new List<string>();

                foreach (var receivedMessage in receivedMessages)
                {
                    string body = receivedMessage.Body.ToString();

                    messagesReceived.Add(body);
                }

                return messagesReceived;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task HandleMessage(Action action , string reason , string description )
        {
            try
            {
                ServiceBusReceiver receiver = Client.CreateReceiver(_serviceBusConfiguration.QueueName);

                ServiceBusReceivedMessage receivedMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(10));

                switch (action)
                {
                    case Action.Complete : await receiver.CompleteMessageAsync(receivedMessage);
                        break;
                    
                    case Action.Abandon : await receiver.AbandonMessageAsync(receivedMessage);
                        break;
                    
                    case Action.Defer : await receiver.DeferMessageAsync(receivedMessage);
                        break;
                    
                    case Action.DeadLetter :
                        if (!string.IsNullOrEmpty(reason) && !string.IsNullOrEmpty(description))
                        {
                            await DeadLetterAMessageAsync( receiver , receivedMessage , reason , description);
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region Private Methods

        private async Task DeadLetterAMessageAsync( ServiceBusReceiver receiver , ServiceBusReceivedMessage receivedMessage , string reason , string description )
        {
            await receiver.DeadLetterMessageAsync(receivedMessage, reason, description);

            ServiceBusReceiver dlqReceiver = Client.CreateReceiver(_serviceBusConfiguration.QueueName,
                new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter
                });

            ServiceBusReceivedMessage dlqMessage = await dlqReceiver.ReceiveMessageAsync();
            
            string dlqReason = dlqMessage.DeadLetterReason;
            string dlqDescription = dlqMessage.DeadLetterErrorDescription;
            
            Console.WriteLine($"dlqReason: {dlqReason}");
            Console.WriteLine($"dlqDescription: {dlqDescription}");
        }

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

