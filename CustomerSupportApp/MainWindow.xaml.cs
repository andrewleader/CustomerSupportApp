using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CustomerSupportApp.ViewModels;

namespace CustomerSupportApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedQuestion != null && !string.IsNullOrWhiteSpace(_viewModel.ResponseText))
            {
                MessageBox.Show("Response sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _viewModel.ResponseText = string.Empty;
            }
            else
            {
                MessageBox.Show("Please enter a response before sending.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}