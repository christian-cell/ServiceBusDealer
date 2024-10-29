
using NetNinja.ServiceBusDealer.Tests.models;

namespace NetNinja.ServiceBusDealer.Tests.mocks
{
    public static class ServiceBusMessagesMocks
    {
        internal static List<ServiceBusMessageCommad.ServiceBusMessageCommand> GetServiceBusMessages()
        {
            return new List<ServiceBusMessageCommad.ServiceBusMessageCommand>()
            {
                new ServiceBusMessageCommad.ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "christian", Receptor = "Robin"
                },
                new ServiceBusMessageCommad.ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "fernando", Receptor = "roberto"
                },
                new ServiceBusMessageCommad.ServiceBusMessageCommand
                {
                    Message = "ultimo mensaje para probar  método messages", Emissor = "alejandro", Receptor = "martin"
                },
            };
        }
    }
};

