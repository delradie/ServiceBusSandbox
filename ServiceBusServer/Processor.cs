using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

using TestbedContracts;

namespace ServiceBusServer
{
    internal class Processor
    {
        private readonly String _connectionString;
        private readonly String _requestQueueName;
        private readonly String _responseQueueName;

        private Int32 _messageCount = 0;

        private ServiceBusClient _busClient;
        private ServiceBusSessionProcessor _busProcessor;

        public Processor(String connectionString, String requestQueueName, String responseQueueName)
        {
            this._connectionString = connectionString;

            this._requestQueueName = requestQueueName;
            this._responseQueueName = responseQueueName;            

            this._busClient = new ServiceBusClient(connectionString);
        }

        private async Task EnsureSessionQueue(String queueName, Boolean requiresSession)
        {
            ServiceBusAdministrationClient Client = new ServiceBusAdministrationClient(this._connectionString);

            Boolean CreateClient;

            if (!await Client.QueueExistsAsync(queueName))
            {
                CreateClient = true;
            }
            else
            {
                var QueueDetails = await Client.GetQueueAsync(queueName);

                if(QueueDetails.Value.RequiresSession != requiresSession)
                {
                    await Client.DeleteQueueAsync(queueName);
                    CreateClient = true;
                }
                else
                {
                    CreateClient = false;
                }
            }

            if(CreateClient)
            {
                CreateQueueOptions QueueConfig = new CreateQueueOptions(queueName)
                { 
                    RequiresSession = requiresSession
                };

                await Client.CreateQueueAsync(QueueConfig);
            }
        }

        public async Task Start()
        {
            if(_busProcessor != null)
            {
                return;
            }

            await EnsureSessionQueue(this._requestQueueName, true);
            await EnsureSessionQueue(this._responseQueueName, true);

            this._messageCount = 0;

            ServiceBusSessionProcessorOptions Options = new ServiceBusSessionProcessorOptions
            {
                // By default after the message handler returns, the processor will complete the message
                // If I want more fine-grained control over settlement, I can set this to false.
                AutoCompleteMessages = false,

                // I can also allow for processing multiple sessions
                MaxConcurrentSessions = 5,

                // By default or when AutoCompleteMessages is set to true, the processor will complete the message after executing the message handler
                // Set AutoCompleteMessages to false to [settle messages](https://docs.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock) on your own.
                // In both cases, if the message handler throws an exception without settling the message, the processor will abandon the message.
                MaxConcurrentCallsPerSession = 2,
            };

            _busProcessor = _busClient.CreateSessionProcessor(this._requestQueueName, Options);
            _busProcessor.ProcessMessageAsync +=_busProcessor_ProcessMessageAsync;
            _busProcessor.ProcessErrorAsync +=_busProcessor_ProcessErrorAsync;

            await _busProcessor.StartProcessingAsync();
        }

        private async Task _busProcessor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            Console.WriteLine(arg.Exception?.ToString());
        }

        private async Task _busProcessor_ProcessMessageAsync(ProcessSessionMessageEventArgs arg)
        {
            ServiceBusReceivedMessage ReceivedMessage = arg.Message;
            BinaryData MessageBody = ReceivedMessage.Body;
            RequestMessage Request = MessageBody.ToObjectFromJson<RequestMessage>();

            String ResponseMessageText = $"Your message was {Request.MessageText.Length} characters long";

            ResponseMessage Response = new ResponseMessage()
            {
                OriginalMessageText = Request.MessageText,
                RequestTimestamp = Request.Timestamp,
                ResponseText = ResponseMessageText,
                Timestamp = DateTimeOffset.Now
            };

            BinaryData ResponseBody = new BinaryData(Response);

            ServiceBusMessage ResponseMessage = new ServiceBusMessage(ResponseBody)
            {
                CorrelationId = ReceivedMessage.MessageId,
                MessageId = ReceivedMessage.MessageId,
                SessionId = ReceivedMessage.SessionId
            };

            ServiceBusSender ResponseSender = _busClient.CreateSender(_responseQueueName);

            await arg.CompleteMessageAsync(ReceivedMessage);
            await ResponseSender.SendMessageAsync(ResponseMessage);

            this._messageCount++;
        }

        public async Task<Int32> Stop()
        {
            await _busProcessor.StopProcessingAsync();

            return this._messageCount;
        }
    }
}
