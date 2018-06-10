using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetHelper_Application.App;
using DotNetHelper_Application.Interface;
using DotNetHelper_Application.Services;
using DotNetHelper_Contracts.Enum;
using DotNetHelper_DependencyResolver;
using DotNetHelper_IO;
using DotNetHelper_IO.Interface;
using DotNetHelper_Logger;
using DotNetHelper_Logger.Interface;
using DotNetHelper_Serializer.DataSource;
using FilePurger.CustomEnum;
using FilePurger.Extension;
using Newtonsoft.Json;
using SimpleInjector;

namespace FilePurger
{
    class Program
    {
        private static FolderObject PurgeFolder { get;  set; }
        private static List<PurgeFolders> FolderTypes { get; } = Enum.GetValues(typeof(PurgeFolders)).Cast<PurgeFolders>().ToList(); 

        private static Dictionary<PurgeFolders, TimeSpan> ExpirationLookup = new Dictionary<PurgeFolders, TimeSpan>()
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

            DependencyResolver.GetInstance<IFileLogger>().ConsoleAndLog("Checking for any file(s) & folder(s) to purge");



            FolderTypes.ForEach(delegate(PurgeFolders folderType)
            {
             
                var folder = new FolderObject($"{PurgeFolder.FullFolderPath}{folderType}",false,true);
                
               
            });


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
            var logger = new FileLogger();

            // Register all of the services
            container.RegisterSingleton<IApplication>(() => app);
            container.RegisterSingleton<ISerializer>(() => appSerializer);
            container.RegisterSingleton<IApplicationConfiguration<AppSetting>>(() => configuration);
            container.RegisterSingleton<ILogger>(() => logger);

            DependencyResolver.SwapContainer(container);
        }


        #region  Application Specific Initialization 
        private static void InitApp()
        {
            DependencyResolver.GetInstance<ILogger>().ConsoleAndLog("Initializing application....");
            PurgeFolder = new FolderObject(Default.PurgeFolder,true,true);
            FolderTypes.ForEach(t => new FolderObject(PurgeFolder.FullFolderPath + t).Create() );

            var files = GetAllFilesRecursive();
        }
        #endregion




        /// <summary>
        /// https://stackoverflow.com/questions/929276/how-to-recursively-list-all-the-files-in-a-directory-in-c
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static  IEnumerable<FileObject> GetAllFilesRecursive()
        {
            var FullFolderPath = PurgeFolder.FullFolderPath;
            var queue = new Queue<string>() { };
            queue.Enqueue(FullFolderPath);
            while (queue.Count > 0)
            {
                FullFolderPath = queue.Dequeue();
                try
                {
                    foreach (var subDir in Directory.GetDirectories(FullFolderPath))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                var files = new List<string>() { };
                try
                {
                    files = Directory.GetFiles(FullFolderPath).ToList();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                foreach (var t in files)
                {
                    yield return new FileObject(t);
                }

            }
        }


    }


}
