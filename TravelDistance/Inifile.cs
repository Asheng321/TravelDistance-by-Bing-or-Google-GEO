using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace TravelDistance
{
    class Inifile
    {
        string inipath;
        //declare read and write API functions of ini file
        [DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public Inifile(string INIPath)
        {
            inipath = INIPath;
        }


        public void IniWriteValue(string Section, string Key, string Value)//read ini file function
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }

        public string IniReadValue(string Section, string Key)//write ini file function
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "can not read value", temp, 500, this.inipath);
            return temp.ToString();
        }

        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }

    }
}
