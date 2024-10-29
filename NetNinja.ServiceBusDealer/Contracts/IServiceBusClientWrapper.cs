using Action = NetNinja.ServiceBusDealer.Models.Enums.Action;

namespace NetNinja.ServiceBusDealer.Contracts
{
    public interface IServiceBusClientWrapper<T>
    {
        Task SendListAsMessage(List<T> messages);
        Task SendMessageAsync(T message);
        Task SendMessagesAsync(List<T> messages);
        Task SendBatchOfMessagesAsync(List<T> messages);
        Task<List<string>> ReceiveBatchOfMessages(int maxMessagesToReceive);
        Task HandleMessage(Action action, string reason, string description);
    }
};

