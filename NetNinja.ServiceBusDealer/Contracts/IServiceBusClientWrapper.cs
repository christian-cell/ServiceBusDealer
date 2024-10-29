namespace NetNinja.ServiceBusDealer.Contracts
{
    public interface IServiceBusClientWrapper<T>
    {
        Task SendListAsMessage(List<T> messages);
        Task SendMessageAsync(T message);
        Task SendMessagesAsync(List<T> messages);
        Task SendBatchOfMessagesAsync(List<T> messages);
    }
};

