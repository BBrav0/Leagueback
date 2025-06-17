using System.Windows;
using DotNetEnv; // Add this using statement

namespace backend
{
    public partial class App : Application
    {
        // This method runs once when your application first starts.
        protected override void OnStartup(StartupEventArgs e)
        {
            // Load environment variables from the .env file
            Env.Load();
            
            base.OnStartup(e);
        }
    }
}