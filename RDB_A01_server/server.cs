//*********************************************
// File			 : server.cs
// Project		 : PROG2111 - Assignment 1
// Programmer	 : Nick Byam, Chris Lemon
// Last Change   : 2020-10-04
//*********************************************


using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.IO;
using RDB_A01_client;
using System.CodeDom;
using System.Threading;
using System.Runtime.Remoting.Lifetime;

namespace RDB_A01_server
{

    //****************************************
    // Class        : server
    // Description  : The server class is used to start up a server service that accepts multiple clients at once.
    //              : The server spins off a new thread for each client that connects and has a handful of methods for
    //              : starting the server, handling new clients and creating separate threads for them, as well as communicating
    //              : with the client over data streams.
    //****************************************
    class server
    {

        // Server properties
        private int _clientCount;
        public int ClientCount
        {
            get { return _clientCount; }
            set { _clientCount = value; }
        }
        

        /////////////////////////////////////////
        // Method       : Main
        // Description  : The main method that is run when the program is run. This starts up the server in its own delegate thread
        // Parameters   : string[] : args : arguments from the command line, unused in this application
        // Returns      : N/A
        /////////////////////////////////////////
        static void Main(string[] args)
        { 
            
            Thread t = new Thread(delegate()
            {
                server myServer = new server(); // create a new instance of the server
            });
            t.Start();
        }


        /////////////////////////////////////////
        // Method       : server (ctor)
        // Description  : The constructor for the server class, starts the listener, creates a database class, sets the client count
        //              : and then starts the server functions of the class.
        // Parameters   : N/A
        // Returns      : N/A
        /////////////////////////////////////////
        public server() // Created a ctor to initialize the IP address, also creates the listener/ server
        {
            string dbFile = ".\\MyDatabase.csv";
            Database csv = new Database(dbFile); // Open database in constructor so that all clients access only one db class
            TcpListener listener = new TcpListener(IPAddress.Any, 23000); // create a listener for incoming clients
            ClientCount = -1; // set to -1 so that the server doesn't exit thinking all the clients have left before they join
            listener.Start(); // start listeneing
            StartServer(listener, csv); // start the server processes
        }


        /////////////////////////////////////////
        // Method       : StartServer
        // Description  : This method is used to listen for incoming clients and create new threads for them when they connect
        // Parameters   : TcpListener : listener : The listener passed from the constructor that waits for incoming clients
        //              : Database : MyDatabase : The database class that the clients will access when finding or inserting data
        // Returns      : N/A
        /////////////////////////////////////////
        private void StartServer(TcpListener listener, Database MyDatabase)
        {
            
            try
            {
                Console.WriteLine("Listening for a Connection. . .");
                while (true)
                {
                    if (!listener.Pending()) // used to keep the server from blocking functionality while waiting for a new client
                    {
                        if (ClientCount == 0) // when client count drops to 0, the server shuts down
                        {
                            break;
                        }
                        else
                        {
                            Thread.Sleep(1000); // if no clients are pending, sleep for a second and then begin a new iteration
                            continue;
                        }
                    }
                    TcpClient client = listener.AcceptTcpClient(); // if a client is pending, create and accept a new client
                    if (ClientCount == -1) // if this is the first client, the client count becomes one
                    {
                        ClientCount = 1;
                    }
                    else if (ClientCount >= 1) // all subsequent clients just raise the count by 1
                    {
                        ClientCount++;
                    }
                    Console.WriteLine("Connected to Client");
                    Thread t = new Thread(() => handleDevice(client, MyDatabase)); // spin a new thread for the client and pass it
                    t.Start();                                                     // the client class and the database class
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine("A problem with the socket occured: {0}", e);
                listener.Stop();
            }
            listener.Stop(); // once client count hits 0, the server stops
        }


        /////////////////////////////////////////
        // Method       : handleDevice
        // Description  : The method responsible for handling client threads, getting data from them, and sending replies
        // Parameters   : object : obj : the object passed in for the device, in this case it is a TcpClient class
        //              : Database : csv : The database class that the client will access
        // Returns      : N/A
        /////////////////////////////////////////
        private void handleDevice(object obj, Database csv)
        {
            TcpClient client = (TcpClient)obj; // accepts an object, which is the TcpClient, cast it to TcpClient
            var clientStream = client.GetStream(); // Open the stream to the client
            string data = null;
            try
            {
                while(true)
                {
                    data = receiveDataFromClient(clientStream); // get data from the client stream, only valid data is received
                    if (data == ".")  // user enters . to exit, dont send it to the database, it wont recognize it
                    {
                        break;
                    }
                    string databaseEntry = null;
                    string sendBackMessage = null;
                    databaseEntry = data;
                    sendBackMessage = csv.parseRequest(databaseEntry); // sends the data to the database to be parsed
                    sendDataToClient(clientStream, sendBackMessage); // send a reply back to the client
                }
                ClientCount--; // the client has indicated they are exiting, decrease client count and close stream.
                clientStream.Close(); // after you finish with the client, close both it and the stream
                client.Close();
            }
            catch(SocketException e)
            {
                Console.WriteLine("A problem occured with the socket; {0}", e);
                client.Close();
            }
        }

        
        /////////////////////////////////////////
        // Method       : receiveDataFromClient
        // Description  : A method that receives data from the network stream sent by the client
        // Parameters   : NetworkStream : stream : the stream opened between the client and server
        // Returns      : receivedData : the data sent by the client
        /////////////////////////////////////////
        private string receiveDataFromClient(NetworkStream stream)
        {
            string recievedData = null;
            byte[] rawData = null;
            int bytesRec;

            rawData = new byte[1024]; // buffer to hold the data as its sent in byte form through the stream
            bytesRec = stream.Read(rawData, 0, rawData.Length); // read from the stream and store in the buffer, keep track of bytes
            stream.Flush(); // flush the stream after the read to get rid of old data
            recievedData += Encoding.ASCII.GetString(rawData, 0, bytesRec); // decode the bytes into an ascii string

            return recievedData;
        }

        
        /////////////////////////////////////////
        // Method       : sendDataToClient
        // Description  : a method to send a response back to the client based on the data received from the client
        // Parameters   : NetworkStream : stream : The stream between the client and server
        //              : string : dataToSend : The response to send back to the client
        // Returns      : 0 : To say the operation went ok
        /////////////////////////////////////////
        private int sendDataToClient(NetworkStream stream, string dataToSend)
        {
            byte[] dataSent = Encoding.ASCII.GetBytes(dataToSend); // encode the message to send into bytes
            stream.Write(dataSent, 0, dataSent.Length); // write the bytes to the stream to be received by the client
            stream.Flush(); // flush the stream to get rid of the old data
            return 0;
        }
    }
}
  
