// See https://aka.ms/new-console-template for more information
using ServiceBusServer;

using TestbedContracts;

Console.WriteLine("Starting");

Processor BusProcessor = new Processor(Constants.ServiceBusConnectionString, Constants.RequestQueueName, Constants.ResponseQueueName);

await BusProcessor.Start();

Console.WriteLine("Press any key to kill the server");

Console.ReadKey();

Int32 ReceivedMessageCount = await BusProcessor.Stop();

Console.WriteLine($"{ReceivedMessageCount} messages handled while processor was active");

Console.ReadKey();