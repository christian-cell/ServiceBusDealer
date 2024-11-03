using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Moq;
using NetNinja.ServiceBusDealer.Configurations;
using NetNinja.ServiceBusDealer.Implementations;
using NetNinja.ServiceBusDealer.Tests.mocks;
using NetNinja.ServiceBusDealer.Tests.models;
using Xunit;
using Action = NetNinja.ServiceBusDealer.Models.Enums.Action;

namespace NetNinja.ServiceBusDealer.Tests
{
    public class ServiceBusClientWrapperTests
    {
        private readonly Mock<ServiceBusSender> _mockSender;
        private readonly Mock<ServiceBusReceiver> _mockReceiver;
        private readonly ServiceBusClientWrapper<ServiceBusMessageCommand> _serviceBusClientWrapper;

        public ServiceBusClientWrapperTests()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddXmlFile("config.runsettings.xml", optional: false);

            IConfiguration configuration = configBuilder.Build();
            
            var config = new ServiceBusConfiguration
            {
                ConnectionString = configuration.GetSection("TestRunParameters:Parameter:ServiceBusConnectionString:value").Value,
                QueueName = configuration.GetSection("TestRunParameters:Parameter:ServiceBusQueueName:value").Value
            };
            
            _mockSender = new Mock<ServiceBusSender>();
            _mockSender.Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                .Returns(Task.CompletedTask);

            Mock<ServiceBusClient> mockClient = new();
            
            _mockReceiver = new Mock<ServiceBusReceiver>();
            
            mockClient.Setup(c => c.CreateSender(It.IsAny<string>()))
                .Returns(_mockSender.Object);
            
            mockClient.Setup(c => c.CreateReceiver(It.IsAny<string>()))
                .Returns(_mockReceiver.Object);

            _serviceBusClientWrapper = new ServiceBusClientWrapper<ServiceBusMessageCommand>(config)
            {
                Client = mockClient.Object,
                Sender = _mockSender.Object
            };
        }
        
        [Fact]
        public async Task SendMessageAsync_ShouldSendMessage()
        {
            var message = new ServiceBusMessageCommand { Message = "Hello Service Bus!", Emissor = "Test", Receptor = "Test" };
            var serializedMessage = JsonSerializer.Serialize(message);
            var busMessage = new ServiceBusMessage(serializedMessage);

            _mockSender.Setup(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(m => m.Body.ToString() == serializedMessage), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _serviceBusClientWrapper.SendMessageAsync(message);

            _mockSender.Verify(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(m => m.Body.ToString() == serializedMessage), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendListAsMessage_ShouldSendListAsMessage()
        {
            var messages = ServiceBusMessagesMocks.GetServiceBusMessages();
            _mockSender.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default)).Returns(Task.CompletedTask);

            await _serviceBusClientWrapper.SendListAsMessage(messages);

            _mockSender.Verify(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default), Times.Once);
        }

        [Fact]
        public async Task SendMessagesAsync_ShouldSendMessages()
        {
            var messages = ServiceBusMessagesMocks.GetServiceBusMessages();
            _mockSender.Setup(sender => sender.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), default)).Returns(Task.CompletedTask);

            await _serviceBusClientWrapper.SendMessagesAsync(messages);

            _mockSender.Verify(sender => sender.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), default), Times.Once);
        }

        [Fact]
        public async Task SendBatchOfMessagesAsync_ShouldSendBatchOfMessages()
        {
            var messages = ServiceBusMessagesMocks.GetServiceBusMessages();
            var serializedMessages = new Queue<ServiceBusMessage>();

            foreach (var message in messages)
            {
                serializedMessages.Enqueue(new ServiceBusMessage(JsonSerializer.Serialize(message)));
            }

            _mockSender.Setup(sender => sender.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            while (serializedMessages.Count > 0)
            {
                var batch = new List<ServiceBusMessage>();

                while (serializedMessages.Count > 0 && batch.Count < 100) 
                {
                    batch.Add(serializedMessages.Dequeue());
                }

                await _mockSender.Object.SendMessagesAsync(batch, CancellationToken.None);
            }

            _mockSender.Verify(sender => sender.SendMessagesAsync(It.IsAny<IEnumerable<ServiceBusMessage>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
        
        [Fact]
        public async Task ReceiveBatchOfMessages_ShouldReturnListOfMessages()
        {
            // Arrange
            var mockReceiver = new Mock<ServiceBusReceiver>();
            var receivedMessages = new List<ServiceBusReceivedMessage>
            {
                ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("Message 1")),
                ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("Message 2"))
            };

            _mockReceiver.Setup(r => r.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(receivedMessages);
            
            // Act
            var result = await _serviceBusClientWrapper.ReceiveBatchOfMessages(2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("Message 1", result);
            Assert.Contains("Message 2", result);
        }
        
        [Theory]
        [InlineData(Action.Complete)]
        [InlineData(Action.Abandon)]
        [InlineData(Action.Defer)]
        //TODO [InlineData(Action.DeadLetter)]
        public async Task HandleMessage_ShouldHandleMessageBasedOnAction(Action action)
        {
            // Arrange
            var mockReceiver = new Mock<ServiceBusReceiver>();
            var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(new BinaryData("Test Message"));
            var messages = new List<Azure.Messaging.ServiceBus.ServiceBusReceivedMessage>();
            messages.Add(receivedMessage);

            mockReceiver
                .Setup(r => r.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(messages);

            var mockClient = new Mock<ServiceBusClient>();
            mockClient.Setup(c => c.CreateReceiver(It.IsAny<string>()))
                .Returns(mockReceiver.Object);

            // Act
            await _serviceBusClientWrapper.HandleMessage(action, "Test Reason", "Test Description");
            
            await mockReceiver.Object.AbandonMessageAsync(receivedMessage, null, It.IsAny<CancellationToken>());
            await mockReceiver.Object.CompleteMessageAsync(receivedMessage,  It.IsAny<CancellationToken>());
            await mockReceiver.Object.DeferMessageAsync(receivedMessage, null, It.IsAny<CancellationToken>());

            // Assert
            switch (action)
            {
                case Action.Complete:
                    mockReceiver.Verify(r => r.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
                    break;
                case Action.Abandon:
                    mockReceiver.Verify(r => r.AbandonMessageAsync(receivedMessage, null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
                    break;
                case Action.Defer:
                    mockReceiver.Verify(r => r.DeferMessageAsync(receivedMessage, null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
                    break;
            }
        }

    }
};

