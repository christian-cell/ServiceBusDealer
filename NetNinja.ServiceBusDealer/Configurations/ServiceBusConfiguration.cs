namespace NetNinja.ServiceBusDealer.Configurations
{
    public class ServiceBusConfiguration
    {
        public string ConnectionString { get; set; } 
        public string QueueName { get; set; }
        
        public ServiceBusConfiguration() 
        {
        }
    }
};

