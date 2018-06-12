using DotNetHelper_Application.Interface;
using DotNetHelper_DependencyResolver;

namespace FilePurger
{
    public static class Default
    {
        internal static IApplicationConfiguration<AppSetting> AppConfiguration { get; } = DependencyResolver.GetInstance<IApplicationConfiguration<AppSetting>>();

        public static string PurgeFolder { get; } = AppConfiguration.AddIfNotExist(new AppSetting(){Name = "PurgeFolder" ,Value = $@"C:\Temp\" }).Value;
    }
}
