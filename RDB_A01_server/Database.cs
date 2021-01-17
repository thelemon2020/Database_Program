/*
* FILE : DbEntry.cs
* PROJECT : PROG2111 - Assignment #1
* PROGRAMMER : Chris Lemon & Nick Byam
* FIRST VERSION : 2020 - 09 - 15
* REVISED ON : 2020 - 10 - 04
* DESCRIPTION : This file defines the Database Entry class. Each instance of this class makes up an entry in the database 
*/

using RDB_A01_client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RDB_A01_server
{
   /*
    * NAME : DbEntry
    * PURPOSE : This defines the Database class.  This controls all the logic and opertation of the database, including creating database entries, 
    *           reading from the database and writing to it
    */
    class Database
    {
        //class properties
       public string fileOpened { get; set; }
       public int fileLength { get; set; }
       // Dictionary for holding the database entries
       private ConcurrentDictionary<int, DbEntry> fileContents = new ConcurrentDictionary<int, DbEntry>();
       public ConcurrentDictionary<int, DbEntry> FileContents
        {
            get { return fileContents; }
            set { fileContents = value; }
        }
       public string dataToWrite { get; set; }

       /*
        * METHOD : Database()
        *
        * DESCRIPTION : The overloaded constructor method for the Database class.  
        *
        * PARAMETERS : fileToOpen - the string path of the database file
        * 
        * RETURNS : Nothing
        */
        public Database(string fileToOpen)
        {
            //check that that the file exists and create if not
            fileOpened = fileToOpen;
            checkFileExists(fileOpened);
        }

        /*
        * METHOD : ~Database()
        *
        * DESCRIPTION : The overloaded Destructor method for the Database class.  Updates the database as it's last action
        *
        * PARAMETERS : Nothing
        * 
        * RETURNS : Nothing
        */
        ~Database()
        {
            StreamWriter finalWrite = new StreamWriter(fileOpened);
            //iterate through the dictionary and print each to a line in the csv file
            foreach (KeyValuePair<int, DbEntry> entry in fileContents)
            {
                finalWrite.Write(entry.Value.FormatForWriting());
            }
            finalWrite.Close();  //close writer
        }

        /*
        * METHOD : checkFileExists()
        *
        * DESCRIPTION : This method checks to see if the csv file exists and reads it if it does.  If not, creates files.
        *
        * PARAMETERS : filePath - the path for the database file
        * 
        * RETURNS : Nothing
        */
        public bool checkFileExists(string filePath)
        {
            FileStream DatabaseFile = null;
            if (System.IO.File.Exists(filePath))  //file exists
            {
                
                StreamReader FromServer = new StreamReader(fileOpened);
                string entry = "";
                string[] fields = null;
                while ((entry = FromServer.ReadLine()) != null)  //iterate through csv file and copy each entry
                {
                    fields = entry.Split(',');
                    DbEntry newEntry = new DbEntry(Int32.Parse(fields[0]), fields[1], fields[2], fields[3]);//convert each line in the csv to a DbEntry object
                    fileContents.TryAdd(newEntry.entryID, newEntry);//add the new entry to the dict
                }
                FromServer.Close();  //close read stream
                fileLength = fileContents.Count() + 1; //set the member ID to one more than the current member ID
                return true;
            }
            else
            {
                DatabaseFile = new FileStream(filePath, FileMode.Create); //create file if it doesn't exist
                fileLength = 1;
                if (DatabaseFile == null) //if it fails to create
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
       /*
        * METHOD : parseRequest()
        *
        * DESCRIPTION : This method takes the message sent through the server by the user and determines the action to take
        *
        * PARAMETERS : databaseEntry - arguments sent by the user
        * 
        * RETURNS : operationSuccess - string telling the user if the command was successful or not
        */
        public string parseRequest(string databaseEntry)
        {
            var arguments = databaseEntry.Split(' '); //split the string into separate arguments
            string operationSuccess = "";

            if (arguments[0] == "INSERT" && fileContents.Count <= 40000) // cannot insert if over 40000 entries in db
            {
                string makeEntry = String.Format("{0},{1},{2}", arguments[1], arguments[2], arguments[3]); //recombine the string for DbEntry creation
                DbEntry insertEntry = CreateNewEntry(makeEntry);//make a new DbEntry
                if (writeNewEntry(insertEntry) == true)
                {
                    operationSuccess = "INSERT OPERATION SUCCESSFUL";
                }
                else
                {
                    operationSuccess = "INSERT OPERATION FAILED";
                }
            }
            else if (arguments[0] == "INSERT" && fileContents.Count > 40000) //if database is full
            {
                operationSuccess = "INSERT OPERATION FAILED - Database Full";
            }
            else if (arguments[0] == "UPDATE")
            {

                DbEntry toUpdate = (FindInDatabase(Int32.Parse(arguments[1]))); //find the entry to update
                if (toUpdate != null) //if it's found
                {
                    string makeEntry = String.Format("{0},{1},{2},{3}\n", arguments[1], arguments[2], arguments[3], arguments[4]);//recombine string to make a new DbEntry
                    DbEntry updateEntry = CreateNewEntry(makeEntry); //create the new entry
                    UpdateEntry(Int32.Parse(arguments[1]), updateEntry); //pass in key and new value to update entry
                    operationSuccess = "UPDATE OPERATION SUCCESSFUL";
                }
                else
                {
                    operationSuccess = "UPDATE OPERATION FAILED - Entry Not In File";
                }
            }
            else if (arguments[0] == "FIND")
            {
                if (fileContents.Count == 0)
                {
                    operationSuccess = "FIND OPERATION FAILED - Entry Not In File";
                }
                else
                {
                    DbEntry didFind = FindInDatabase(Int32.Parse(arguments[1])); //look for entry in database
                    if (didFind != null)
                    {
                        operationSuccess = didFind.FormatForWriting();
                    }
                    else
                    {
                        operationSuccess = "FIND OPERATION FAILED - Entry Not In File";
                    }
                }
            }
            return operationSuccess;
        }
        /*
        * METHOD : CreateNewEntry()
        *
        * DESCRIPTION : This method takes in a string and creates a DbEntry object out of it
        *
        * PARAMETERS : newData - Information to be entered into a database entry
        * 
        * RETURNS : newEntry - the new database object
        */
        public DbEntry CreateNewEntry(string newData)
        {
           
            string[] fields = newData.Split(',');
            if (fields.Count() == 3)
            {
                DbEntry newEntry = new DbEntry(fileLength, fields[0], fields[1], fields[2]);//for insert
                return newEntry;
            }
            else if (fields.Count()==4)
            {
                DbEntry newEntry = new DbEntry(Int32.Parse(fields[0]), fields[1], fields[2], fields[3]); //for update
                return newEntry;
            }
            else
            {
                return null;
            }
            
        }

       /*
        * METHOD : writeNewEntry()
        *
        * DESCRIPTION : This method adds a DbEntry object to the dictionary holding all entries
        *
        * PARAMETERS : toWrite - the DbEntry to be added
        * 
        * RETURNS : true - if add was successful
        *           false - if database file is past max size
        */
        public bool writeNewEntry(DbEntry toWrite)
        {
            if (fileLength >= 40000) //database is full
            {
                return false;
            }
            else
            {
                fileContents.TryAdd(toWrite.entryID ,toWrite);//adds entry to database
                fileLength += 1;
                return true;
            }
        }

        /*
        * METHOD : UpdateEntry()
        *
        * DESCRIPTION : This method allows updating to a DbEntry
        *
        * PARAMETERS : entryToUpdate - key for dictionary value to be updated
        *              entryUpdate - the DbEntry to be used for the update
        * 
        * RETURNS : Nothing
        */
        public void UpdateEntry(int entryToUpdate, DbEntry entryUpdate)
        {
            //updates all fields in dict entry
            fileContents[entryToUpdate].entryFirstName = entryUpdate.entryFirstName;
            fileContents[entryToUpdate].entryLastName = entryUpdate.entryLastName;
            fileContents[entryToUpdate].entryDOB = entryUpdate.entryDOB;
        }

        /*
       * METHOD : FindInDatabase()
       *
       * DESCRIPTION : This method takes an int and uses it as a key in the dictionary to return the value
       *
       * PARAMETERS : memberId - the dictionary key
       * 
       * RETURNS : null - if not found
       *           fileContents[memberID] - the value if found
       */
        public DbEntry FindInDatabase(int memberID)
        {
            if (memberID > fileContents.Count()) // if ID is outside of range
            {
                return null;
            }
            else
            {
                return fileContents[memberID]; //send back required entry
            }

        }
    }
}
    
