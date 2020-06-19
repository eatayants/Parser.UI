using MahApps.Metro.Controls;
using Parser.DataModel;
using Parser.UI;
using System;
using System.Deployment.Application;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Parser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly MainViewModel vm;

        public MainWindow()
        {
            InitializeComponent();
            vm = new MainViewModel();
            DataContext = vm;
            Title = string.Format("{0} {1}", Title, getRunningVersion());
        }

        private Version getRunningVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch (Exception)
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        async void UrlChanged(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AsyncGo();
            }
        }

        async void GoClicked(Object sender, RoutedEventArgs e)
        {
            await AsyncGo();
        }

        private Task AsyncGo()
        {
            return Task.Run(() =>
            {
                vm.Go();
            });
        }

        void SaveClicked(object sender, RoutedEventArgs e)
        {
	        var dlg = new Microsoft.Win32.SaveFileDialog
	        {
		        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
		        FileName = $"{vm.Address.GetCatalogName()}-{DateTime.Now:dd-MM-yyyy HH-mm-ss}".VaildFileName(),
		        Filter = "Excel file (*.xlsx)|*.xlsx"
	        };
	        if (dlg.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    vm.SaveToExcel(dlg.FileName);
                });
            }

        }
    }
}
