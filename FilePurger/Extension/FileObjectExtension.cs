using System;
using DotNetHelper_DependencyResolver;
using DotNetHelper_IO.Interface;
using DotNetHelper_Logger.Interface;

namespace FilePurger.Extension
{
  public static class FileObjectExtension
    {

        public static void PurgeFile(this IFileObject file, bool disposeObject,TimeSpan span)
        {
            if (file.CreationTimeUtc != null &&   file.CreationTimeUtc.Value.AddDays(span.TotalDays) > DateTime.UtcNow)
            {
                file.DeleteFile(disposeObject);
                DependencyResolver.GetInstance<ILogger>().ConsoleAndLog($"Purging File : {file.FullFilePath}  -> {file.GetFileSizeDisplay()} b");
            }
            
        }

    }
}
