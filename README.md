![alt text](ReadmeImages/image.png)

# ServiceBusDealer

# Usage
¿ How to inject ?

```
    //program.cs

    var serviceBusConfiguration = new ServiceBusConfiguration{
            
        ConnectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING") ?? string.Empty,
        QueueName = Environment.GetEnvironmentVariable("SERVICEBUS_QUEUENAME") ?? string.Empty
    };

    services.AddSingleton(serviceBusConfiguration);
    services.AddTransient<IServiceBusClientWrapper<ServiceBusMessageCommand>, ServiceBusClientWrapper<ServiceBusMessageCommand>>();
```

Terms:

- ConnectionString : connection string of your service bus , if your que has an access shared policy you should use connection string of that policy.

- QueueName : name of your queue in azure.

- ServiceBusMessageCommand : type of data you wish to send by ServiceBus.


# Send Messages

Do not worry abot Serialize , the nuget with deal with that.

In this nuget there are 4 ways to send messages to ServiceBus:

1. **SendMessageAsync**:
Send a Single message

```
    await _serviceBusClientWrapper.SendMessageAsync("basic nuget");
```

2. **SendListAsMessage**
Send a message of type List<>

```
    var message1 = new ServiceBusMessageCommand { Message = "great day let's code", Emissor = "Robert", Receptor = "Claris" };
    var message2 = new ServiceBusMessageCommand { Message = "second message for queue", Emissor = "Ramiro", Receptor = "Roberto" };
    
    listOfMessages.Add(message1);
    listOfMessages.Add(message2);
    
    await _serviceBusClientWrapper.SendListAsMessage(listOfMessages);
```

3. **SendMessagesAsync**
Send several messages at the same time

```
    var listOfMessages = new List<ServiceBusMessageCommand>();

    var message1 = new ServiceBusMessageCommand { Message = "great day let's code", Emissor = "Robert", Receptor = "Claris" };
    var message2 = new ServiceBusMessageCommand { Message = "second message for queue", Emissor = "Ramiro", Receptor = "Roberto" };
    
    listOfMessages.Add(message1);
    listOfMessages.Add(message2);
    
    await _serviceBusClientWrapper.SendMessagesAsync(listOfMessages);
```

4. **SendBatchOfMessagesAsync**
send several messages wrapped in a Batch

```
    var listOfMessages = new List<ServiceBusMessageCommand>();

    var message1 = new ServiceBusMessageCommand { Message = "great day let's code", Emissor = "Robert", Receptor = "Claris" };
    var message2 = new ServiceBusMessageCommand { Message = "second message for queue", Emissor = "Ramiro", Receptor = "Roberto" };
    
    listOfMessages.Add(message1);
    listOfMessages.Add(message2);
    
    await _serviceBusClientWrapper.SendBatchOfMessagesAsync(listOfMessages);
```

5**ReceiveBatchOfMessages**
Receive a batch of messages

```
   /*
   * @params : int maxMessagesToReceive 
   */
    await _serviceBusClientWrapper.ReceiveBatchOfMessages( maxMessagesToReceive )
```

6**HandleMessage**
After send the message you can: complete , abandone , defer or deadLetter it.

```
    /*
    * @params :Action action , string reason , string description
    */
    await _serviceBusClientWrapper.HandleMessage(action , reason , description )
    
    public enum Action
    {
        Complete,
        Abandon,
        Defer,
        DeadLetter
    }
```
