//*********************************************
// File			 : client.cs
// Project		 : PROG2111 - Assignment 1
// Programmer	 : Chris Lemon, Nick Byam
// Last Change   : 2020-10-04 
//*********************************************


using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml.Schema;
using System.Reflection.Emit;

namespace RDB_A01
{
    //*****************************************
    // Class        : client
    // Description  : The client class serves as the client model that will connect to the server to access the database.
    //              : The main features of the client are the IP entry to connect to a server running on somebody else's network
    //              : as well as input validation for the command and input the user wishes to send to the server's database.
    //*****************************************
    class client
    {
        /////////////////////////////////////////
        // Method       : Main
        // Description  : The code that initializes the client and starts the connection to the server
        // Parameters   : string[] : args : Command line arguments given up running, they are unused as of now
        // Returns      : N/A
        /////////////////////////////////////////
        static void Main(string[] args)
        {
            bool validIP = false;
            string ipAddress = "";
            while (validIP == false) // only move on once the user has entered a valid IP
            {
                Console.WriteLine("Please enter the IP of the Database Server: ");
                ipAddress = Console.ReadLine();
                validIP = validateIP(ipAddress);
            }
            connectToServer(ipAddress); 


            Console.WriteLine("Press Any Key To Continue...");
            Console.ReadKey();

        }


        /////////////////////////////////////////
        // Method       : validateIP
        // Description  : A function used solely to test the IP address provided by the user against a regex for the IP
        // Parameters   : string : IpAddress : The string given by user input that contains the IP Address to test.
        // Returns      : true : The address is valid
        //              : false : the address is invalid
        /////////////////////////////////////////
        static bool validateIP(string ipAddress)
        {
            Regex checkIP = new Regex("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            if (checkIP.IsMatch(ipAddress))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /////////////////////////////////////////
        // Method       : connectToServer
        // Description  : A method which when called from main acts to create a new tcp client and connect to the ip address
        //              : of the server. When it connects to the server it opens a network stream which is used to transmit
        //              : data back and forth through the port. Once a connection is made the user is able to enter data to
        //              : write to the server's db or query it for information.
        // Parameters   : string : serverIP : the ip address of the serve to connect to
        // Returns      : N/A
        /////////////////////////////////////////
        static void connectToServer(string serverIP)
        {
            string data = null; // user input data will populate this string
            string dataRec = null; // this string will contain responses from the server
            int port = 23000; // defualt port we're using is 23000
            Console.WriteLine("Connecting to server. . .");

            TcpClient client = new TcpClient();
            client.Connect(serverIP, port);// establish a connection with the server and get the data stream
            NetworkStream clientStream = client.GetStream();
            Console.WriteLine("Connected to server!");
            string userInput = "";
            while (true) // The data itself is entered in the sendDataToServer method, however this is the main
            {                   // loop to send and then receive data
                userInput = GetUserInput();
                if (userInput == "DEMONSTRATE")
                {
                    string randomInput = "";
                    for (int i = 0; i < 1000; i++)
                    {
                        randomInput = makeRandomString();
                        data = sendDataToServer(clientStream, randomInput);
                        dataRec = recieveDataFromServer(clientStream);
                        Console.WriteLine("Response: {0:S}", dataRec);
                    }
                }
                else
                {
                    data = sendDataToServer(clientStream, userInput);
                    if (userInput == ".")
                    {
                        break;
                    }
                    dataRec = recieveDataFromServer(clientStream);
                    Console.WriteLine("Response: {0:S}", dataRec);
                }
            }
            clientStream.Close(); // after finishing with sending and receiving, close the stream and client to disconnect safely
            client.Close();

        }


        /////////////////////////////////////////
        // Method       : GetUserInput
        // Description  : A method soley used to gather user input for validation and then to send to the server
        // Parameters   : N/A
        // Returns      : sendData : the data that will be sent to the server in the form of a string
        /////////////////////////////////////////
        static string GetUserInput()
        {
            string sendData = null;
            bool okToSend = false;
            while (!okToSend)
            {
                Console.WriteLine("Please enter the data to send, starting with the command. Enter . to exit");
                sendData = Console.ReadLine();
                okToSend = clientSideValidation(sendData);
            }
            return sendData;
        }

        /////////////////////////////////////////
        // Method       : sendDataToServer
        // Description  : the method where the user enters data, and if it is valid it is converted to bytes and sent across
        //              : the stream to the server.
        // Parameters   : NetworkStream : stream : the stream should only be opened once and closed at the end, so it's passed as
        //              :                        : a parameter so it isn't opened and closed multiple times
        // Returns      : sendData : the data that was entered by the user
        /////////////////////////////////////////
        static string sendDataToServer(NetworkStream stream, string sendData)
        {                
            byte[] dataSent = Encoding.ASCII.GetBytes(sendData); // package the data to send into bytes for the stream
            stream.Write(dataSent, 0, dataSent.Length); // write the bytes to the stream
            stream.Flush(); // flush the stream out after the write so old data doesn't remain.
            return sendData;

        }


        /////////////////////////////////////////
        // Method       : makeRandomString
        // Description  : A method that makes a random string to be sent to the server for the DEMONSTRATE command
        // Parameters   : N/A
        // Returns      : a formatted string with the insert command, the first name, last name, and date of birth
        /////////////////////////////////////////
        static string makeRandomString()
        {
            string[] fNames = { "Chris", "Nick", "Carlo", "Noman", "Sean" };
            string[] lNames = { "Smith", "Doe", "Wilson", "Vong", "Cooper" };
            string[] dob = { "06-14-1989", "05-15-1989", "11-23-1963", "12-05-1976", "01-01-1999" };
            Random rng = new Random();
            int rngName = rng.Next(0, 4);
            int rngLName = rng.Next(0, 4);
            int rngDOB = rng.Next(0, 4);
            return String.Format("{0} {1} {2} {3}", "INSERT", fNames[rngName], lNames[rngLName], dob[rngDOB]);
        }

        /////////////////////////////////////////
        // Method       : receieveDataFromServer
        // Description  : a method that reads data from the stream sent by the server
        // Parameters   : NetworkStream : stream : The network stream that carries data between the client and server
        // Returns      : receivedData : the data that is sent to client from the server
        /////////////////////////////////////////
        static string recieveDataFromServer(NetworkStream stream)
        {
            string recievedData = null;
            byte[] rawData = null;

            rawData = new byte[1024]; // buffer that the bytes of response data are stored into from the stream read
            int bytesRec = stream.Read(rawData, 0, rawData.Length); // need to know how many bytes were received to decode to ascii
            stream.Flush(); // flush the stream after reading the data
            recievedData += Encoding.ASCII.GetString(rawData, 0, bytesRec);// decode the response to a string
            return recievedData; // return the response
        }


        /////////////////////////////////////////
        // Method       : clientSideValidation
        // Description  : A method of the client class that is used to test if the data entered by the user is correct
        //              : as well as if the command the user is trying to use is correct with the allowable number of arguments
        // Parameters   : string : allData : the entire data string entered by the user including the command
        // Returns      : true : the data entered is valid
        //              : false : the data entered is not valid
        /////////////////////////////////////////
        static bool clientSideValidation(string allData) // make sure to put this in a try block always
        {
            string argument; // the command that precedes the other user input
            string memberId, fName, lName; // input fields
            DateTime dob; // another input field specifically for a date
            string dobBuf; // a buffer to hold the dob in
            bool errorFlag = true;
            string errMessage = null; // a common variable used to display situation specific errors to the console

            string[] splitData = allData.Split(' '); // split the data on spaces
            argument = splitData[0];

            if(argument == ".") // if the first argument is . that means the user wants to exit, return true
            {
                return true;
            }

            if(splitData.Length > 5) // there should never be more than 5 separate arguments given by the user
            {
                errMessage = "There are too many data elements";
                Console.WriteLine(errMessage);
                return false;
            }
            else
            {
                if (argument == "UPDATE") // update requires 5 arguments
                {
                    if (splitData.Length != 5) // if it's not 5 arguments, display an error and return false
                    {
                        errMessage = "Incorrect arguments for UPDATE. Must include ID, First Name, Last Name, and Date of Birth";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    // Get the member ID as a string, keep it this way because we test against a regex pattern
                    memberId = splitData[1];
                    Regex idRegex = new Regex("[0-9]+"); // only allow 1 or more digits
                    if (idRegex.IsMatch(memberId) != true)
                    {
                        errMessage = "There was a problem with the member ID";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    fName = splitData[2];
                    Regex fNameRegex = new Regex("^[A-Za-z]+[-]?[A-Za-z]+?$"); // at the moment no spaces allowed, capitals optional
                    if (fNameRegex.IsMatch(fName) != true)       // but at least one letter must be present
                    {
                        errMessage = "There was a problem with the first name";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    lName = splitData[3];
                    Regex lNameRegex = new Regex("^[A-Za-z]+[-]?[A-Za-z]+?$"); // again no spaces, capitals optional, must have at least one letter
                    if (lNameRegex.IsMatch(lName) != true)
                    {
                        errMessage = "There was a problem with the last name";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    dobBuf = splitData[4]; // Only allows a date of birth in mm-dd-yyyy format
                    if (!DateTime.TryParseExact(dobBuf, "mm-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
                    {
                        errMessage = "There was a problem with the date of birth, make sure to use mm-dd-yyyy";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    return true; // if it gets down here then all data is valid, return true.
                }
                else if (argument == "INSERT")
                {
                    if (splitData.Length != 4) // insert should have 4 arguments total
                    {
                        errMessage = "Incorrect arguments for INSERT. Must include First Name, Last Name, and Date of Birth.";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    fName = splitData[1];
                    Regex fNameRegex = new Regex("^[A-Za-z]+[-]?[A-Za-z]+?$"); // same as above, at least one letter must be here
                    if (fNameRegex.IsMatch(fName) != true)
                    {
                        errMessage = "There was a problem with the first name";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    lName = splitData[2];
                    Regex lNameRegex = new Regex("^[A-Za-z]+[-]?[A-Za-z]+?$"); // at least one letter for last name must be entered
                    if (lNameRegex.IsMatch(lName) != true)
                    {
                        errMessage = "There was a problem with the last name";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    dobBuf = splitData[3]; // same as before, only accepts mm-dd-yyyy
                    if (!DateTime.TryParseExact(dobBuf, "mm-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
                    {
                        errMessage = "There was a problem with the date of birth, make sure to use mm-dd-yyyy";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    return errorFlag; // If it gets all the way here, then all data is valid
                }
                else if (argument == "FIND") // find only has 2 arguments total
                {
                    if (splitData.Length != 2)
                    {
                        errMessage = "Incorrect arguments for FIND. Only include the Member ID.";
                        Console.WriteLine(errMessage);
                        return false;
                    }

                    memberId = splitData[1];
                    Regex idRegex = new Regex("[0-9]+"); // only allow 1 or more digits
                    if (idRegex.IsMatch(memberId) == true)
                    {
                        return true; // only one field to validate, if its valid, return true
                    }
                    else
                    {
                        errMessage = "There was a problem with the member ID";
                        Console.WriteLine(errMessage);
                        return false;
                    }
                }
                else if (argument == "DEMONSTRATE")
                {
                    if (splitData.Length != 1)
                    {
                        errMessage = "Incorrect arguments for DEMONSTRATE. No arguments required.";
                        Console.WriteLine(errMessage);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else // if a command was entered that isn't one of the three above, display an error and return false.
                {
                    errMessage = "That command was not recognized";
                    Console.WriteLine(errMessage);
                    return false;
                }
            }
        }
    }
}
