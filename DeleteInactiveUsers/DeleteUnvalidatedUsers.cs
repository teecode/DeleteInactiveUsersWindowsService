using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace DeleteInactiveUsers
{
    public partial class DeleteUnvalidatedUsers : ServiceBase
    {
        public DeleteUnvalidatedUsers()
        {
            InitializeComponent();
        }

        
        

        protected override void OnStart(string[] args)
        {
            
            this.WriteToFile("Simple Service started {0}");
            this.ScheduleService();
        }

        protected override void OnStop()
        {
            this.WriteToFile("Simple Service stopped {0}");
            this.Schedular.Dispose();
        }

        private Timer Schedular;

        public void ScheduleService()
        {
            try
            {
                Schedular = new Timer(new TimerCallback(SchedularCallback));
                string mode = ConfigurationManager.AppSettings["Mode"].ToUpper();
                this.WriteToFile("Simple Service Mode: " + mode + " {0}");

                //Set the Default Time.
                DateTime scheduledTime = DateTime.MinValue;

                if (mode == "DAILY")
                {
                    //Get the Scheduled Time from AppSettings.
                    scheduledTime = DateTime.Parse(System.Configuration.ConfigurationManager.AppSettings["ScheduledTime"]);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next day.
                        scheduledTime = scheduledTime.AddDays(1);
                    }
                }

                if (mode.ToUpper() == "INTERVAL")
                {
                    //Get the Interval in Minutes from AppSettings.
                    int intervalMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalMinutes"]);

                    //Set the Scheduled Time by adding the Interval to Current Time.
                    scheduledTime = DateTime.Now.AddMinutes(intervalMinutes);
                    if (DateTime.Now > scheduledTime)
                    {
                        //If Scheduled Time is passed set Schedule for the next Interval.
                        scheduledTime = scheduledTime.AddMinutes(intervalMinutes);
                    }
                }

                TimeSpan timeSpan = scheduledTime.Subtract(DateTime.Now);
                string schedule = string.Format("{0} day(s) {1} hour(s) {2} minute(s) {3} seconds(s)", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                this.WriteToFile("Simple Service scheduled to run after: " + schedule + " {0}");

                DeleteUsers();

                //Get the difference in Minutes between the Scheduled and Current Time.
                int dueTime = Convert.ToInt32(timeSpan.TotalMilliseconds);

                //Change the Timer's Due Time.
                Schedular.Change(dueTime, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                WriteToFile("Simple Service Error on: {0} " + ex.Message + ex.StackTrace);

                //Stop the Windows Service.
                using (System.ServiceProcess.ServiceController serviceController = new System.ServiceProcess.ServiceController("SimpleService"))
                {
                    serviceController.Stop();
                }
            }
        }

        private void DeleteUsers()
        {
            try
            {
                UsersFetcher.DeleteCustomerCascade();
                UsersFetcher.DeleteInACtiveCustomerCascade();
            }
            catch (Exception ex)
            {

                this.WriteToFile(ex.Message);
            }
        }

        private void SchedularCallback(object e)
        {
            this.WriteToFile("Simple Service Log: {0}");
            this.ScheduleService();
        }

        private void WriteToFile(string text)
        {

            try
            {
               //string xxx = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ILOTTO" + Path.DirectorySeparatorChar + "GAMEOFDAYSERVICE" + Path.DirectorySeparatorChar + "GamesCreated.txt");
               //xxx = Path.GetFullPath(xxx).Replace(@"\", @"\\");

                


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
            catch (Exception )
            {
                
            }
        }
    }
}
