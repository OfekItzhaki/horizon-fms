using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace FileManagementSystem.Presentation;

public partial class FolderPickerDialog : Window
{
    public string? SelectedPath { get; private set; }
    
    public FolderPickerDialog()
    {
        InitializeComponent();
    }
    
    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Folder Selection."
        };
        
        if (openFileDialog.ShowDialog() == true)
        {
            var folderPath = Path.GetDirectoryName(openFileDialog.FileName);
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                FolderPathTextBox.Text = folderPath;
            }
        }
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var path = FolderPathTextBox.Text;
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            MessageBox.Show("Please select a valid folder.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        SelectedPath = path;
        DialogResult = true;
    }
}
