using System.Windows;

namespace FileManagementSystem.Presentation;

public partial class RenameDialog : Window
{
    public string NewName { get; private set; } = string.Empty;
    
    public RenameDialog(string currentPath)
    {
        InitializeComponent();
        var currentName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
        NameTextBox.Text = currentName;
        NameTextBox.SelectAll();
    }
    
    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        NewName = NameTextBox.Text;
        DialogResult = true;
    }
}
