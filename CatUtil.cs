using System;
using System.Collections.Generic;
using System.Text;

namespace Cat
{
    public class Util
    {
        public static string FileToString(string sFileName)
        {
            // Read the file 
            System.IO.StreamReader file = new System.IO.StreamReader(sFileName);
            try
            {
                return file.ReadToEnd();
            }
            finally
            {
                file.Close();
            }
        }
    }
}
