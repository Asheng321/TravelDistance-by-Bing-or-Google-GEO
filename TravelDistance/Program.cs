using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Security;
using System.Web.Services.Protocols;
using System.Configuration;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.Device.Location;

namespace TravelDistance
{
    class Program
    {
        static void Main(string[] args)
        {
            string strcurpath = Directory.GetCurrentDirectory() + '\\' + "umass.ini";
            Inifile configfile = new Inifile(strcurpath);
            if (!configfile.ExistINIFile())
            {
                Console.WriteLine("Configure file does not exist.");
            }

            string conStr_UMS = configfile.IniReadValue("ConnectStrings", "DBconnect");

            SqlCommand command;
            SqlDataReader reader;


            //----------------------------------------------------
            //Get Postal Address which request to get GEO code
            //call SP [usp_List_PostalAddressGEOCode]
            //----------------------------------------------------
            SqlConnection Conn_UMS = new SqlConnection(conStr_UMS);
            command = new SqlCommand("[ElkhornDataAccess].[usp_List_PostalAddressGEOCode]", Conn_UMS);
            command.CommandType = CommandType.StoredProcedure;
            reader = null;

            Int32 ContactMechanismID = 0;
            string Latitude = string.Empty;
            string Longitude = string.Empty;
            string AddressLine1 = string.Empty;
            string AddressLine2 = string.Empty;
            string AddressLine3 = string.Empty;
            try
            {
                if (Conn_UMS.State == ConnectionState.Closed)
                {
                    Conn_UMS.Open();
                }

                reader = command.ExecuteReader();
                do
                {
                    while (reader.Read())
                    {
                        ContactMechanismID = Convert.ToInt32(reader["ContactMechanismID"].ToString());
                        AddressLine1 = reader["AddressLine1"].ToString();
                        AddressLine2 = reader["AddressLine2"].ToString();
                        AddressLine3 = reader["AddressLine3"].ToString();

                        //Call webservice to get two places distance.
                        DistanceCalc distanceCalc = new DistanceCalc();
                        List<GEOCodeStep> addrs = new List<GEOCodeStep>();
                        addrs = distanceCalc.GetGEOCode(AddressLine1);
                        foreach (GEOCodeStep addr in addrs)
                        {
                            Latitude = addr.lat;
                            Longitude = addr.lng;
                        }

                        //Use address2 or address3 to get GEO code the address1 returns ZERO_RESULTS
                        if (Latitude =="ZERO_RESULTS")
                        {
                            addrs = distanceCalc.GetGEOCode(AddressLine2);
                            foreach (GEOCodeStep addr in addrs)
                            {
                                Latitude = addr.lat;
                                Longitude = addr.lng;
                            }

                            if (Latitude == "ZERO_RESULTS")
                            {
                                addrs = distanceCalc.GetGEOCode(AddressLine3);
                                foreach (GEOCodeStep addr in addrs)
                                {
                                    Latitude = addr.lat;
                                    Longitude = addr.lng;
                                }
                            }
                        }

                        CallSP_usp_Update_PostalAddress
                        (
                            conStr_UMS,
                            ContactMechanismID,
                            Latitude,
                            Longitude
                        );

                        //Reset variable
                        ContactMechanismID = 0;
                        Latitude = string.Empty;
                        Longitude = string.Empty;
                        AddressLine1 = string.Empty;
                        AddressLine2 = string.Empty;
                        AddressLine3 = string.Empty;
                    }
                }
                while (reader.NextResult());

                if (reader != null)
                    reader.Close();
            }
            catch (Exception ex)
            {
                RecordLog(" Failed to call SP usp_List_PostalAddressGEOCode", ex.Message);
            }
            finally
            {
                if (Conn_UMS != null && Conn_UMS.State == ConnectionState.Open)
                    Conn_UMS.Close();
            }

        }

        public static void CallSP_usp_Update_PostalAddress(string connString, long ContactMechanismID, string Latitude, string Longitude)
        {
            SqlConnection Conn = new SqlConnection(connString);
            SqlCommand command = new SqlCommand("[ElkhornDataAccess].[usp_Update_PostalAddress_GEOCode]", Conn);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@ContactMechanismID", SqlDbType.BigInt).Value = ContactMechanismID;
            command.Parameters.Add("@Latitude", SqlDbType.VarChar, 4000).Value = Latitude;
            command.Parameters.Add("@Longitude", SqlDbType.VarChar, 4000).Value = Longitude;
            command.Parameters.Add("@ErrorMessage", SqlDbType.VarChar, 4000).Value = "";
            command.Parameters["@ErrorMessage"].Direction = ParameterDirection.Output;

            command.CommandTimeout = 300;

            try
            {
                if (Conn.State == ConnectionState.Closed)
                    Conn.Open();

                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                RecordLog("Failed to execute usp_Update_PostalAddress", ex.Message);
                throw ex;
            }
            finally
            {
                if (Conn != null && Conn.State == ConnectionState.Open)
                    Conn.Close();
            }
        }


        public static void RecordLog(string funcName, string logText)
        {
            // Create a writer and open the file:
            StreamWriter log;
            string errorLogFile = Directory.GetCurrentDirectory() + '\\' + "TravelDistanceLog.txt";


            if (!File.Exists(errorLogFile))
            {
                log = new StreamWriter(errorLogFile);
            }
            else
            {
                log = File.AppendText(errorLogFile);
            }

            // Write to the file:
            log.WriteLine(DateTime.Now);
            log.WriteLine(funcName + " - " + logText);
            log.WriteLine();

            // Close the stream:
            log.Close();
        }
    }
}
