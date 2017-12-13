using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Transactions;
using System.Web;

namespace DeleteInactiveUsers
{
    public static class UsersFetcher
    {
        static int cmdfetchmode;
        public static List<Customer> getInactivecustomers()
        {
            try
            {
                string daysstring = System.Configuration.ConfigurationManager.AppSettings["InactivityDays"].ToString();

                int days = int.Parse(daysstring);
                
                DateTime minussevendays = DateTime.Now.AddDays(-days);
               GCL_LOTTODBCONTEXTDataContext db = new GCL_LOTTODBCONTEXTDataContext();
               List<Customer> customers = db.Customers.Where(a => a.Customer_Category1.code == "OC" && a.Customer_Login.is_active == false && a.Customer_Login.is_verified == false && a.dateRegistered < minussevendays).ToList<Customer>();
               return customers;
            }
            catch (Exception ex)
            {
                WriteToFile(ex.Message);
                return null;
            }
        }


        public static List<Customer> getVerifiedInactivecustomers()
        {
            try
            {
                string daysstring = System.Configuration.ConfigurationManager.AppSettings["VerifiedButInactivitydays"].ToString();

                int days = int.Parse(daysstring);

                DateTime minusdays = DateTime.Now.AddDays(-days);
                GCL_LOTTODBCONTEXTDataContext db = new GCL_LOTTODBCONTEXTDataContext();
                List<Customer> customers = db.Customers.Where(a => a.Customer_Category1.code == "OC" && a.Customer_Login.is_active == true && a.dateRegistered < minusdays && a.Customer_Transactions.Count <= 2 && a.Customer_Login.Customer_Login_Logs.Last().timein < minusdays  && a.Customer_Login.Customer_Login_Logs.Count <3).ToList<Customer>();
                return customers;
            }
            catch (Exception ex)
            {
                WriteToFile(ex.Message);
                return null;
            }
        }

        public static void DeleteInACtiveCustomerCascade()
        {
            GCL_LOTTODBCONTEXTDataContext db = new GCL_LOTTODBCONTEXTDataContext();
            //List<Daily_Game> DailyGAmes = new List<Daily_Game>();
            string daysstring = System.Configuration.ConfigurationManager.AppSettings["VerifiedButInactivitydays"].ToString();

            int days = int.Parse(daysstring);

            DateTime minusdays = DateTime.Now.AddDays(-days);
           // List<Customer> customers = db.Customers.Where(a => a.Customer_Category1.code == "OC" && a.Customer_Wallet.wallet_balance <= 200 && a.Customer_Login.Customer_Login_Logs.Count < 3 && a.Customer_Login.is_active == false && a.Customer_Login.is_verified == false && a.dateRegistered < minussevendays).ToList<Customer>();
            List<Customer> customers = db.Customers.Where(a => a.Customer_Category1.code == "OC" && a.Customer_Wallet.wallet_balance <= 200 && a.Customer_Login.is_active == true && a.dateRegistered < minusdays && a.Customer_Transactions.Count <= 2 && a.Customer_Login.Customer_Login_Logs.OrderByDescending(b => b.id).FirstOrDefault().timein < minusdays && a.Customer_Login.Customer_Login_Logs.Count < 3).ToList<Customer>();
            WriteToFile("Customer Count within " + days + " days => " + customers.Count());
            for (int i = 0; i < customers.Count; i++)
            {
                Customer cust = customers[i];
                try
                {
                    WriteToFile(cust.id + ": " + cust.Customer_Login.username + " begin delete");
                    if (cust.Customer_Transactions.Count() < 3  && cust.Tickets.Count() == 0 && cust.Customer_Deposits.Count() < 5)
                    {
                        Customer_Login_Log lastlogin = cust.Customer_Login.Customer_Login_Logs.OrderByDescending(a => a.id).FirstOrDefault();
                        if (lastlogin == null || (lastlogin.status == true && lastlogin.timein <= minusdays))
                        {
                            db.deleteCustomer(cust.id);
                            WriteToFile(cust.id + ": " + cust.Customer_Login.username + " deleted Sucesfully");
                            updateDeletedCustomersTable(cust, "Active But Not Operating since " + ((lastlogin != null) ? lastlogin.timein.ToShortDateString() : cust.dateRegistered.ToShortDateString()));
                        }
                        else
                        {
                            WriteToFile(cust.id + ": " + cust.Customer_Login.username + " lastlogin = " + lastlogin.timein);
                        }
                    }
                    else
                    {
                        WriteToFile(cust.id + ": " + cust.Customer_Login.username + "  has transactions or deposit or tickets");
                    }






                }
                catch (Exception ex)
                {
                    WriteToFile("Error :" + ex.Message + " on internal");
                }







            }


        }




        public static void DeleteCustomerCascade()
        {
            GCL_LOTTODBCONTEXTDataContext db = new GCL_LOTTODBCONTEXTDataContext();
            //List<Daily_Game> DailyGAmes = new List<Daily_Game>();
            string daysstring = System.Configuration.ConfigurationManager.AppSettings["InactivityDays"].ToString();

            int days = int.Parse(daysstring);

            DateTime minussevendays = DateTime.Now.AddDays(-days);
            List<Customer> customers = db.Customers.Where(a => a.Customer_Category1.code == "OC" && a.Customer_Wallet.wallet_balance <= 200 && a.Customer_Login.Customer_Login_Logs.Count < 3 && a.Customer_Login.is_active == false && a.Customer_Login.is_verified == false && a.dateRegistered < minussevendays).ToList<Customer>();
            WriteToFile("Customer Count within "+ days +" days => "+ customers.Count());
            for (int i = 0; i<  customers.Count ;  i++)
            {
                Customer cust = customers[i];
                try
                {
                    WriteToFile(cust.id + ": " + cust.Customer_Login.username + " begin delete");
                    if (cust.Customer_Transactions.Count() < 10 && cust.Tickets.Count() == 0 && cust.Customer_Deposits.Count() < 5)
                    {
                        Customer_Login_Log lastlogin = cust.Customer_Login.Customer_Login_Logs.OrderByDescending(a => a.id).FirstOrDefault();
                        if (lastlogin == null || (lastlogin.status == true && lastlogin.timein <= minussevendays))
                        {
                            db.deleteCustomer(cust.id);
                            WriteToFile(cust.id + ": " + cust.Customer_Login.username + " deleted Sucesfully");
                            updateDeletedCustomersTable(cust, "Inactive and not validated since " + ((lastlogin != null)? lastlogin.timein.ToShortDateString() : cust.dateRegistered.ToShortDateString()) );
                        }
                        else
                        {
                            WriteToFile(cust.id + ": " + cust.Customer_Login.username + " lastlogin = " + lastlogin.timein);
                        }
                    }
                    else
                    {
                        WriteToFile(cust.id + ": " + cust.Customer_Login.username + "  has transactions or deposit or tickets");
                    }

               




                }
                catch (Exception ex)
                {
                    WriteToFile("Error :" + ex.Message + " on internal");
                }

                



                        

            }


        }

        private static void updateDeletedCustomersTable(Customer cust, string detail)
        {
            Deleted_Customer customers = new Deleted_Customer
            {
                email = cust.Customer_Login.email,
                fullname = cust.lastname + " " + cust.firstname + " " + cust.middlename,
                username = cust.Customer_Login.email,
                reasonfordeletion = detail
            };

            GCL_LOTTODBCONTEXTDataContext db = new GCL_LOTTODBCONTEXTDataContext();
            db.Deleted_Customers.InsertOnSubmit(customers);
            db.SubmitChanges();
        }

        private static void WriteToFile(string text)
        {

            try
            {
                string path = System.Configuration.ConfigurationManager.AppSettings["LogFile"].ToString();
                //Directory.GetDirectories(path);
                FileInfo file = new FileInfo(path);
                file.Directory.Create();
                if (file.Exists && file.Length > 40000)
                    file.Delete();

                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.Close();
                }

            }
            catch (Exception)
            {

            }
        }
    }
}
