
using NetNinja.ServiceBusDealer.Tests.models;

namespace NetNinja.ServiceBusDealer.Tests.mocks
{
    public static class ServiceBusMessagesMocks
    {
        internal static List<ServiceBusMessageCommand> GetServiceBusMessages()
        {
            return new List<ServiceBusMessageCommand>()
            {
                new ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "christian", Receptor = "Robin"
                },
                new ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "fernando", Receptor = "roberto"
                },
                new ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "alejandro", Receptor = "martin"
                },
            };
        }
    }
};

