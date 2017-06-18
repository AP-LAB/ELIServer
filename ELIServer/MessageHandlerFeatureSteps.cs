using System;
using System.Net.Sockets;
using TechTalk.SpecFlow;
using NSubstitute;
using System.Dynamic;
using Newtonsoft.Json;
using ELIServer;
using ELIServer.Messaging;
using Microsoft.CSharp;
using System.Text;

namespace ELIServerTest
{
    [Binding]
    public class MessageHandlerFeatureSteps
    {
  
        dynamic message;
        MessageSocketManager messageSocketManager;
        TcpClient tcpClient;

        [Given(@"A new client is connected")]
        public void GivenANewClientIsConnected()
        {
            var mainwindowSub = Substitute.ForPartsOf<MainWindow>();
            mainwindowSub.When<MainWindow>(x => x.SetNumberOfConnectedCalls(Arg.Any<int>())).DoNotCallBase();
            mainwindowSub.When<MainWindow>(x => x.SetNumberOfConnectedClients(Arg.Any<int>())).DoNotCallBase();
            mainwindowSub.When<MainWindow>(x => x.SetNumberOfPendingCalls(Arg.Any<int>())).DoNotCallBase();

            messageSocketManager = new MessageSocketManager(Substitute.For<MainWindow>());

            tcpClient = new TcpClient();
            tcpClient.Connect("localhost", 30000);

            dynamic connectMessage = new ExpandoObject();
            connectMessage.ClientID = "1";
            connectMessage.ID = "1";
            
            WriteToTcpClient(JsonConvert.SerializeObject(connectMessage));            
        }

        [Given(@"The incoming message has arrived")]
        public void GivenTheIncomingMessageHasArrived()
        {
            message = new ExpandoObject();
        }
        
        [When(@"The type of the message is ""(.*)""")]
        public void WhenTheTypeOfTheMessageIs(string p0)
        {
            message.message_type = "LogIn";
        }
        
        [When(@"the correct credentials used")]
        public void WhenTheCorrectCredentialsUsed()
        {
            message.ClientID = '2';
            message.UserName = "JohnDoe";
            message.PassWord = "John Doe";
            message.LogInTime = "2017-06-17 17:08:33";
        }
        
        [Then(@"a message should be returned to the user with the user's information")]
        public void ThenAMessageShouldBeReturnedToTheUserWithTheUserSInformation()
        {
            var stringMessage = JsonConvert.SerializeObject(message);
            WriteToTcpClient(stringMessage);
        }

        private void WriteToTcpClient(String stringMessage)
        {
            //Write the sting to the tcpClient
            byte[] messageBytes = Encoding.UTF8.GetBytes(stringMessage);
            byte[] sizeBytesArray = BitConverter.GetBytes((uint)messageBytes.Length);
            byte[] finalArray = new byte[sizeBytesArray.Length + messageBytes.Length];
            System.Buffer.BlockCopy(sizeBytesArray, 0, finalArray, 0, sizeBytesArray.Length);
            System.Buffer.BlockCopy(messageBytes, 0, finalArray, 4, messageBytes.Length);
            tcpClient.GetStream().WriteAsync(messageBytes, 0, finalArray.Length);

        }


    }
}
