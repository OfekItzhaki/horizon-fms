using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.DTOs;
using FileManagementSystem.Presentation.ViewModels;

namespace FileManagementSystem.Presentation;

public partial class MainWindow : Window
{
    private readonly IMediator _mediator;
    private readonly ILogger<MainWindow> _logger;
    private ObservableCollection<FolderViewModel> _folders;
    private ObservableCollection<FileItemDto> _files;
    private ProgressReportDto? _currentProgress;
    
    public MainWindow(IMediator mediator, ILogger<MainWindow> logger)
    {
        try
        {
            _mediator = mediator;
            _logger = logger;
            _folders = new ObservableCollection<FolderViewModel>();
            _files = new ObservableCollection<FileItemDto>();
            
            _logger.LogInformation("Starting MainWindow initialization...");
            
            InitializeComponent();
            
            _logger.LogInformation("InitializeComponent completed");
            
            Loaded += MainWindow_Loaded;
            AllowDrop = true;
            Drop += MainWindow_Drop;
            DragOver += MainWindow_DragOver;
            
            // Force window to be visible
            Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
            
            _logger.LogInformation("MainWindow initialized successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error initializing window: {ex.Message}\n\n{ex.StackTrace}";
            _logger?.LogError(ex, "Error in MainWindow constructor");
            
            // Show error in a simple way
            try
            {
                MessageBox.Show(errorMsg, 
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // If MessageBox fails, at least log it
                System.Diagnostics.Debug.WriteLine(errorMsg);
            }
            throw;
        }
    }
    
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadFoldersAsync();
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial data");
            var errorMsg = $"Error loading application data: {ex.Message}\n\n{ex.StackTrace}";
            MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task LoadFoldersAsync()
    {
        try
        {
            _logger.LogInformation("Loading folders...");
            var query = new GetFoldersQuery();
            var result = await _mediator.Send(query);
            
            _folders.Clear();
            foreach (var folder in result.Folders)
            {
                _folders.Add(new FolderViewModel(folder));
            }
            
            FoldersTreeView.ItemsSource = _folders;
            FoldersStatusText.Visibility = _folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _logger.LogInformation("Loaded {Count} folders", _folders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading folders");
            MessageBox.Show($"Error loading folders: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task LoadFilesAsync(Guid? folderId = null)
    {
        try
        {
            _logger.LogInformation("Loading files...");
            var query = new SearchFilesQuery(FolderId: folderId);
            var result = await _mediator.Send(query);
            
            _files.Clear();
            foreach (var file in result.Files)
            {
                _files.Add(file);
            }
            
            FilesListView.ItemsSource = _files;
            FilesStatusText.Visibility = _files.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            _logger.LogInformation("Loaded {Count} files", _files.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading files");
            MessageBox.Show($"Error loading files: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ScanDirectory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderPickerDialog();
        if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.SelectedPath))
        {
            return;
        }
        
        ProgressGrid.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressTextBlock.Text = "Starting scan...";
        
        try
        {
            var progress = new Progress<ProgressReportDto>(report =>
            {
                _currentProgress = report;
                ProgressBar.Value = report.Percentage;
                ProgressTextBlock.Text = report.CurrentItem;
                
                if (report.IsCompleted)
                {
                    ProgressGrid.Visibility = Visibility.Collapsed;
                }
            });
            
            var command = new ScanDirectoryCommand(dialog.SelectedPath, progress);
            var result = await _mediator.Send(command);
            
            MessageBox.Show(
                $"Scan complete!\nFiles processed: {result.FilesProcessed}\nFiles skipped: {result.FilesSkipped}\nFolders created: {result.FoldersCreated}",
                "Scan Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            await LoadFoldersAsync();
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning directory");
            MessageBox.Show($"Error scanning directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProgressGrid.Visibility = Visibility.Collapsed;
        }
    }
    
    private async void UploadFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        
        try
        {
            var command = new UploadFileCommand(dialog.FileName);
            var result = await _mediator.Send(command);
            
            MessageBox.Show($"File uploaded successfully: {result.FilePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            MessageBox.Show($"Error uploading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSearchAsync();
    }
    
    private async void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await PerformSearchAsync();
        }
    }
    
    private async void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Debounce search could be implemented here
        // For now, we don't auto-search on text change to avoid performance issues
        try
        {
            // Could implement debounced search here if needed
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in search text changed handler");
        }
    }
    
    private async Task PerformSearchAsync()
    {
        try
        {
            var searchTerm = SearchTextBox.Text;
            var isPhoto = PhotosOnlyCheckBox.IsChecked ?? false;
            
            var query = new SearchFilesQuery(
                SearchTerm: string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm,
                IsPhoto: isPhoto ? true : null);
            
            var result = await _mediator.Send(query);
            
            _files.Clear();
            foreach (var file in result.Files)
            {
                _files.Add(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            MessageBox.Show($"Error performing search: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SearchTextBox.Text = string.Empty;
            PhotosOnlyCheckBox.IsChecked = false;
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search");
            MessageBox.Show($"Error clearing search: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void FoldersTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        try
        {
            if (e.NewValue is FolderViewModel folder)
            {
                await LoadFilesAsync(folder.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading files for selected folder");
            MessageBox.Show($"Error loading folder contents: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void FilesListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Handle file selection if needed
    }
    
    private async void DeleteFile_Click(object sender, RoutedEventArgs e)
    {
        if (FilesListView.SelectedItem is not FileItemDto file)
        {
            return;
        }
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{file.Path}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var command = new DeleteFileCommand(file.Id, MoveToRecycleBin: true);
                await _mediator.Send(command);
                
                MessageBox.Show("File deleted successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadFilesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                MessageBox.Show($"Error deleting file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void RenameFile_Click(object sender, RoutedEventArgs e)
    {
        if (FilesListView.SelectedItem is not FileItemDto file)
        {
            return;
        }
        
        var dialog = new RenameDialog(file.Path);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var command = new RenameFileCommand(file.Id, dialog.NewName);
                var result = await _mediator.Send(command);
                
                MessageBox.Show($"File renamed to: {result.NewPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadFilesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file");
                MessageBox.Show($"Error renaming file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement undo functionality
        MessageBox.Show("Undo functionality not yet implemented", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await LoadFoldersAsync();
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing data");
            MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }
    
    private async void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        var command = new UploadFileCommand(file);
                        await _mediator.Send(command);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading file via drag-drop: {FilePath}", file);
                    }
                }
            }
            
            await LoadFilesAsync();
        }
    }
}
