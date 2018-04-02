using Costa.Services;
using Costa.Services.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CostaService.SyncShipJob
{
    public partial class ShipSyncService : ServiceBase
    {
        System.Timers.Timer timeDelay;
      
        static string LOG_FILE = ConfigurationManager.AppSettings["LogFilePath"] + "Logfile.txt";
        static string ONSHORE_SERVER_BASE_URI = ConfigurationManager.AppSettings["OnshoreServerUri"];
        static string ONSHORE_SERVER_RESOURCE_URI = ONSHORE_SERVER_BASE_URI + "Resources/";
        static string ONSHORE_SERVER_RESOURCE_FILES = ONSHORE_SERVER_BASE_URI + "files/";


        static string SHIP_RESOURCE_FILE_PATH = ConfigurationManager.AppSettings["ResourcesDirectory"];

        static string ERROR_DOWNLOADING_NEW_FILE = "Failed to download file {0} from {1}.";
        static string ERROR_DELETING_SHIP_FILE = "Failed to delete file {0}.";
        static string ERROR_UPDATING_SHIP_FILE = "Failed to updating file {0} from {1}.";
        static string SYNC_TIME_INTERVAL_MINUTE = ConfigurationManager.AppSettings["SyncIntervalInMinutes"];
        private static readonly Http Http = new Http();

        public ShipSyncService()
        {
            InitializeComponent();

            timeDelay = new System.Timers.Timer();
            timeDelay.Interval= Convert.ToInt32(SYNC_TIME_INTERVAL_MINUTE);
            timeDelay.Elapsed += new System.Timers.ElapsedEventHandler(WorkProcess);
        }

        public void WorkProcess(object sender, System.Timers.ElapsedEventArgs e)
        {
            ProcessSync();
        }




        public static void ProcessSync()
        {


            //get all ship files
            List<FileModel> shipFiles = Directory.GetFiles(SHIP_RESOURCE_FILE_PATH, "*")
                                                  .Select(x => new FileInfo(x))
                                                  .Select(x => new FileModel
                                                  {
                                                      Name = Path.GetFileName(x.Name),
                                                      Size = x.Length,
                                                      ModifiedOn = x.LastWriteTimeUtc,
                                                      CreatedOn = x.CreationTimeUtc
                                                  })
                                                  .ToList();

            List<FileModel> shoreFiles = new List<FileModel>();
            try
            {
                //get all the shore files
                WriteLogToFile(string.Format("Please wait, Getting files from {0}.....", ONSHORE_SERVER_BASE_URI));
                shoreFiles = GetFiles(ONSHORE_SERVER_RESOURCE_FILES);
                WriteLogToFile(string.Format("Total Files on SHORE {0}.....", shoreFiles.Count));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed to get files from {0}. Please check log files for more information.", ONSHORE_SERVER_BASE_URI));

                //write error to log file
                string errorLog = string.Format("Failed to get files from {0}", ONSHORE_SERVER_BASE_URI);
                errorLog += string.Format("\n {0} \n {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
                WriteLogToFile(errorLog);

            }



            //get file names which are available on shore but not in ship
            var newFiles = shoreFiles.Where(x => !shipFiles.Select(l => l.Name).Contains(x.Name)).ToList();

            //get deleted files from shore

            var deletedFiles = shipFiles.Where(x => !shoreFiles.Select(s => s.Name).Contains(x.Name)).ToList();

            //get common files which are updated
            var modifiedFiles = (from s in shoreFiles
                                 join l in shipFiles on s.Name equals l.Name
                                 where s.ModifiedOn > l.ModifiedOn
                                 select s).ToList();


            //download all the new files
            if (newFiles.Count > 0)
            {
                WriteLogToFile(string.Format("Downloading {0} new file(s) from {1}", newFiles.Count, ONSHORE_SERVER_BASE_URI));
                int newFilesDownloaded = 0;

                foreach (var item in newFiles)
                {
                    try
                    {

                        WriteLogToFile("\tDownloading new file " + item.Name);
                        Http.Download(ONSHORE_SERVER_RESOURCE_URI + item.Name, Path.Combine(SHIP_RESOURCE_FILE_PATH, item.Name));
                        //keep track how many file(s) downloaded successfully.
                        newFilesDownloaded++;
                    }
                    catch (Exception ex)
                    {
                        //write error to console
                        WriteLogToFile(string.Format(ERROR_DOWNLOADING_NEW_FILE + " Please check log files for more information.", item.Name, ONSHORE_SERVER_BASE_URI));

                        //write error to log file
                        string errorLog = string.Format(ERROR_DOWNLOADING_NEW_FILE, item.Name, ONSHORE_SERVER_BASE_URI);
                        errorLog += string.Format("\n {0} \n {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
                        WriteLogToFile(errorLog);
                    }
                }

               // WriteLogToFile(string.Format("{0} new file(s) downloaded from {1}.", newFilesDownloaded, ONSHORE_SERVER_BASE_URI));
                WriteLogToFile(string.Format("{0} new file(s) downloaded from {1}.", newFilesDownloaded, ONSHORE_SERVER_BASE_URI));

            }

            //now delete those files which are present on ship but not on shore.
            if (deletedFiles.Count > 0)
            {
                int filesDeleted = 0;

                WriteLogToFile((string.Format("Deleting {0} file(s) from ship", deletedFiles.Count)));
                foreach (var item in deletedFiles)
                {
                    try
                    {
                        string filePath = Path.Combine(SHIP_RESOURCE_FILE_PATH, item.Name);
                        File.Delete(filePath);
                        //keep track how many file(s) deleted successfully.
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        //write error to console
                        WriteLogToFile(string.Format(ERROR_DELETING_SHIP_FILE + " Please check log files for more information.", item.Name));

                        //write error to log file
                        string errorLog = string.Format(ERROR_DELETING_SHIP_FILE, item.Name);
                        errorLog += string.Format("\n {0} \n {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
                        WriteLogToFile(errorLog);
                    }
                }

                //Console.WriteLine(string.Format("{0} file(s) deleted from ship.", filesDeleted));
                WriteLogToFile(string.Format("{0} file(s) deleted from ship.", filesDeleted));

            }

            //now check for modified files
            if (modifiedFiles.Count > 0)
            {
                int filesUpdated = 0;
                WriteLogToFile(string.Format("Downloading {0} updated file(s) from {1}", modifiedFiles.Count, ONSHORE_SERVER_BASE_URI));

                foreach (var item in modifiedFiles)
                {
                    WriteLogToFile("\t Downloading updated file " + item.Name);

                    try
                    {
                        Http.Download(ONSHORE_SERVER_RESOURCE_URI + item.Name, Path.Combine(SHIP_RESOURCE_FILE_PATH, item.Name));
                        //keep track of how many files updated successfully.
                        filesUpdated++;
                    }
                    catch (Exception ex)
                    {
                        //write error to console
                       WriteLogToFile(string.Format(ERROR_UPDATING_SHIP_FILE + " Please check log files for more information.", item.Name, ONSHORE_SERVER_BASE_URI));

                        //write error to log file
                        string errorLog = string.Format(ERROR_UPDATING_SHIP_FILE, item.Name, ONSHORE_SERVER_BASE_URI);
                        errorLog += string.Format("\n {0} \n {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
                        WriteLogToFile(errorLog);
                    }
                }

               // Console.WriteLine(string.Format("{0} file(s) updated from {1}.", filesUpdated, ONSHORE_SERVER_BASE_URI));
                WriteLogToFile(string.Format("{0} file(s) updated from {1}.", filesUpdated, ONSHORE_SERVER_BASE_URI));

            }


          //  Console.WriteLine("SYNCED. Files are upto date.\n\n");
            WriteLogToFile("SYNCED");
            
        }


        /// <summary>
        /// Get file names in json format
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static List<FileModel> GetFiles(string uri)
        {

            string json = Http.Get(uri);

            return JsonConvert.DeserializeObject<ResourceResponseModel>(json)
                              .Files
                              .Select(x => new FileModel { CreatedOn = x.CreatedOn, ModifiedOn = x.ModifiedOn, Size = x.Size, Name = Path.GetFileName(x.Name) })
                              .ToList();


        }

        /// <summary>
        /// Writes any error into log file
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="moreInfo">more detail info about error</param>
        static void WriteLogToFile(string message, string moreInfo = "")
        {
            StreamWriter log;

            if (!File.Exists(LOG_FILE))
            {
                log = new StreamWriter(LOG_FILE);
            }
            else
            {
                log = File.AppendText(LOG_FILE);
            }

            // Write to the file:
            log.WriteLine(DateTime.Now);
            log.WriteLine(message);
            log.WriteLine();
            if (!string.IsNullOrEmpty(moreInfo))
            {
                log.WriteLine(moreInfo);
                log.WriteLine();
            }

            // Close the stream:
            log.Close();


        }

        protected override void OnStart(string[] args)
        {
            WriteLogToFile("Service started");
            timeDelay.Enabled = true;
        }

        protected override void OnStop()
        {
            timeDelay.Enabled = false;

            WriteLogToFile("Service stopped");
        }
    }
}
