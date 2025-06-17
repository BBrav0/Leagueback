using System.Windows;

namespace backend // Or whatever your project's namespace is
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        // In MainWindow.xaml.cs
        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.AddHostObjectToScript("backendBridge", new BackendApiBridge());
            

            // Point to your Next.js dev server
            webView.CoreWebView2.Navigate("http://localhost:3000");
        }
    }
}