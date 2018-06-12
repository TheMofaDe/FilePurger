using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetHelper_Application.App;
using DotNetHelper_Application.Interface;
using DotNetHelper_Application.Services;
using DotNetHelper_Contracts.Enum;
using DotNetHelper_Contracts.Extension;
using DotNetHelper_DependencyResolver;
using DotNetHelper_IO;
using DotNetHelper_IO.Interface;
using DotNetHelper_Logger;
using DotNetHelper_Logger.Interface;
using DotNetHelper_Serializer.DataSource;
using FilePurger.CustomEnum;
using Newtonsoft.Json;
using SimpleInjector;

namespace FilePurger
{
    class Program
    {
        private static FolderObject PurgeFolder { get;  set; }
        private static List<PurgeFolders> FolderTypes { get; } = Enum.GetValues(typeof(PurgeFolders)).Cast<PurgeFolders>().ToList();
        private static int PurgeFolderCount { get; set; } = 0;
        private static int PurgeFileCount { get; set; } = 0;
        private static int PurgeFileErrorCount { get; set; } = 0;
        private static long DiskSpaceRecovered { get; set; } = 0;

        private static readonly Dictionary<PurgeFolders, TimeSpan> ExpirationLookup = new Dictionary<PurgeFolders, TimeSpan>()
        {
            {PurgeFolders.DailyPurge,TimeSpan.FromDays(1)}
           ,{PurgeFolders.MonthlyPurge,TimeSpan.FromDays(31)}
           ,{PurgeFolders.WeeklyPurge,TimeSpan.FromDays(7)}
           ,{PurgeFolders.YearlyPurge,TimeSpan.FromDays(365)}
           ,{PurgeFolders.ManualPurge,TimeSpan.FromDays(50000)}
        };

    
        static void Main(string[] args)
        {
            
            RegisterDependencies();
            InitApp();

            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog("Checking for any file(s) & folder(s) to purge");

            FolderTypes.Where(f => f != PurgeFolders.ManualPurge).ToList().ForEach(delegate(PurgeFolders folderType)
            {
             
                var folder = PurgeFolder.Subfolders.First(subfolder => subfolder.FolderNameOnly == folderType.ToString());
               
                // Grab all files
                folder.RefreshObject(false,true,true);
                folder.Files.ForEach(delegate(FileObject o)
                {
                    var result = ExpirationLookup.TryGetValue(folderType, out var value);
                    if (o.CreationTimeUtc < DateTimeOffset.UtcNow.Subtract(value))
                    {
                        try
                        {
                    
                            PurgeFileCount++;
                            DiskSpaceRecovered += o.GetFileSize(FileObject.SizeUnits.Byte).GetValueOrDefault(0);
                            o.DeleteFile(true);
                        }
                        catch (Exception error)
                        {
                            DependencyResolver.GetInstance<ILogger>().LogError(error);
                            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Couldn't Delete File {o.FullFilePath}");
                            PurgeFileCount--;
                            DiskSpaceRecovered -= o.GetFileSize(FileObject.SizeUnits.Byte).GetValueOrDefault(0);
                            PurgeFileErrorCount++;
                        }

                    }
                    else // DO NOTHING
                    {

                    }
                });

            });


            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Performance Results :");
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Disk Space Recovered : {DiskSpaceRecovered} Bytes");
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Files Deleted : {PurgeFileCount}");
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Files not deleted due to errors : {PurgeFileErrorCount}");



           // DependencyResolver.GetInstance<IEmailSender>().SendToRecipients(new List<string>() { "josephmcnealjr@gmail.com" }, new List<string>() { }, "Test", "Joseph Testing", true);

       
            CloseApp();
        }


        private static void RegisterDependencies()
        {
            // Create a new container
            var container = new Container();

            // Initialize all of the services 
            var app = new Application();
            var appSerializer = new DataSourceJson(new JsonSerializerSettings(){Formatting = Formatting.Indented});
            var configFile = $"{app.GetApplicationFolder(FileRepo.Data)}{app.ApplicationName}.config";
            var configuration = new FileConfiguration<AppSetting>(new FileObject(configFile), appSerializer);
            var logger = new FileLogger {TruncateOnAppStart = true};

#if DEBUG
#endif


            // Register all of the services
            container.RegisterSingleton<IApplication>(() => app);
            container.RegisterSingleton<ISerializer>(() => appSerializer);
            container.RegisterSingleton<IApplicationConfiguration<AppSetting>>(() => configuration);
            container.RegisterSingleton<ILogger>(() => logger);
          //  container.RegisterSingleton<IEmailSender>(() => emailSender);
            DependencyResolver.SwapContainer(container);
        }


        #region  Application Specific Initialization 
        private static void InitApp()
        {
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog("Initializing application....");
            PurgeFolder = new FolderObject(Default.PurgeFolder,true,false,false);

            // DELETE ALL SUBFOLDERS THATS NOT SUPPOSE TO BE THEIR 
            PurgeFolder.Subfolders
                .Where(sf => !FolderTypes.Select(f => f.ToString()).Contains(sf.FolderNameOnly)).ToList()
                .ForEach(delegate(FolderObject o)
                {
                    o.DeleteFolder();
                    DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Deleting Folder {o.FullFolderPath} because it doesn't belong in this directory");
                });


            foreach (var type in FolderTypes)
            {
                if (PurgeFolder.Subfolders.Where(subfolder => subfolder.FolderNameOnly == type.ToString()).IsNullOrEmpty())
                {
                    using (var newFolder = new FolderObject(PurgeFolder.FullFolderPath + type + Path.DirectorySeparatorChar))
                    {
                        DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Creating Folder {newFolder.FullFolderPath}");
                        newFolder.Create();
                       
                    }   
                }
            }

            PurgeFolder.RefreshObject(true, false, false);







        }
        #endregion


        private static void CloseApp()
        {
            if (DependencyResolver.GetInstance<IApplicationConfiguration<AppSetting>>() is FileConfiguration<AppSetting> fileConfiguration && fileConfiguration.ConfigurationFile.Exist != true)
            {
                // SYNC Default configuration if file doens't already 
                fileConfiguration.Sync();
                
            }
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog("Closing application....");
            Environment.Exit(Environment.ExitCode);
        }


 


    }


}
