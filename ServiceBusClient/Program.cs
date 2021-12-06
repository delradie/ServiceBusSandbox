// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus;

using TestbedContracts;

String? Input;
ServiceBusClient Client = new ServiceBusClient(Constants.ServiceBusConnectionString);
var Sender = Client.CreateSender(Constants.RequestQueueName);

while(true)
{
    Console.WriteLine("Enter a line to send to the server or \"exit\" to quit:");

    Input = Console.ReadLine();

    if(String.IsNullOrWhiteSpace(Input))
    {
        continue;
    }

    if(String.Equals(Input.Trim(), "exit", StringComparison.InvariantCultureIgnoreCase))
    {
        break;
    }    

    RequestMessage MessageToSend = new RequestMessage()
    {
        MessageText = Input,
        Timestamp = DateTimeOffset.Now
    };

    BinaryData DataToSend = new BinaryData(MessageToSend);
    String SessionId = Guid.NewGuid().ToString();

    ServiceBusMessage SBMessage = new ServiceBusMessage(DataToSend)
    {
        ReplyTo = Constants.ResponseQueueName,
        ReplyToSessionId = SessionId,
        SessionId = SessionId
    };

    await Sender.SendMessageAsync(SBMessage);

    ServiceBusSessionReceiver SessionReceiver = await Client.AcceptSessionAsync(Constants.ResponseQueueName, SessionId);

    ServiceBusReceivedMessage SBResponse = await SessionReceiver.ReceiveMessageAsync();

    ResponseMessage Response = SBResponse.Body.ToObjectFromJson<ResponseMessage>();

    await SessionReceiver.CompleteMessageAsync(SBResponse);
    await SessionReceiver.CloseAsync();

    Console.WriteLine($"Response to [{Response.OriginalMessageText}] sent from server at {Response.Timestamp}: {Response.ResponseText}");
}

Console.ReadKey();
