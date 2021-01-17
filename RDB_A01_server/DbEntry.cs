/*
* FILE : DbEntry.cs
* PROJECT : PROG2111 - Assignment #1
* PROGRAMMER : Chris Lemon & Nick Byam
* FIRST VERSION : 2020 - 09 - 15
* REVISED ON : 2020 - 10 - 04
* DESCRIPTION : This file defines the Database Entry class. Each instance of this class makes up an entry in the database 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDB_A01_client
{
   /*
    * NAME : DbEntry
    * PURPOSE : This defines the DbEntry class.  It acts as an individual entry into the database.  It holds a member ID, a first name, last name and a DOB.
    */
    class DbEntry
    {
        public int entryID { get; set; }
        public string entryFirstName { get; set; }
        public string entryLastName { get; set; }
        public DateTime entryDOB { get; set; }
       /*
        * METHOD : DbEntry()
        *
        * DESCRIPTION : The overloaded constructor method for the DbEntry class.  
        *
        * PARAMETERS : lineCount - the number to be assigned to the Member ID
        *              firstName - the first name of the database entry
        *              lastName - the last name of the database entry
        *              DOB - the date of birth of the database entry
        *
        * RETURNS : Nothing
        */
        public DbEntry(int lineCount, string firstName, string lastName, string DOB)
        {
            entryID = lineCount;
            entryFirstName = firstName;
            entryLastName = lastName;
            entryDOB = DateTime.Parse(DOB);
        }
       /*
        * METHOD : FormatForWriting()
        *
        * DESCRIPTION : This method formats and outputs the data base entry as it should appear in the csv file
        *
        * PARAMETERS : None
        *
        * RETURNS : Nothing
        */
        public string FormatForWriting()
        {
            return String.Format("{0:D},{1:S},{2:S},{3:S}\n", entryID, entryFirstName, entryLastName, entryDOB.ToString("yyyy-MM-dd")); //for writing to the database file
        }
    }
}
