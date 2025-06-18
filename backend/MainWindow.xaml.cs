using System.Windows;

namespace backend // Or whatever your project's namespace is
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set the minimum width and height to the current actual width and height.
            MinWidth = ActualWidth;
            MinHeight = ActualHeight;
        }

        // In MainWindow.xaml.cs
        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.AddHostObjectToScript("backendBridge", new BackendApiBridge());
            

            // Point to the self-hosted backend which also serves the frontend assets
            // PUBLISH COMMAND
             webView.CoreWebView2.Navigate("http://localhost:5000");

            // TESTING COMMAND
            //webView.CoreWebView2.Navigate("http://localhost:3000");
        }
    }
}