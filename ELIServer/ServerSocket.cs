using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ELIServer
{
    class ServerSocket
    {
        TcpListener listener = null;
        int port = 4567;
        string IPString = "127.0.0.1";
        bool running = false;
        
        public ServerSocket()
        {
            //Parse IP address
            IPAddress localAddr = IPAddress.Parse(IPString);

            //Create a new instance of TcpListener
            listener = new TcpListener(localAddr, port);

            listener.Start();

            running = true;

            StartListening();

        }

        public async void StartListening()
        {
            ClientSocket a = new ClientSocket();

                //TODO match te right clients
            while (running)
            {

                
                //Only accept if there is a pending request
                if (listener.Pending())
                {
                    Debug.WriteLine("Hello client!");
                    a.CreateNewClientByType(listener.AcceptTcpClient());
                    

                }




            }
            

        }


    }
}
