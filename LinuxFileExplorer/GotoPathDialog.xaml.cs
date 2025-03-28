using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LinuxFileExplorer
{
    /// <summary>
    /// Interaction logic for GotoPathDialog.xaml
    /// </summary>
    public partial class GotoPathDialog : Window
    {
        public string EnteredPath { get; private set; }
        public GotoPathDialog(string currentPath = "")
        {
            InitializeComponent();
            PathTextBox.Text = currentPath;
            PathTextBox.Focus();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            EnteredPath = PathTextBox.Text.Trim();
            DialogResult = true;  // Closes the dialog and returns true
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;  // Closes the dialog and returns false
        }
    }
}
