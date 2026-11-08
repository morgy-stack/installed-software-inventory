using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using InstalledSoftwareInventory.Models;
using InstalledSoftwareInventory.Services;

namespace InstalledSoftwareInventory.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly RegistryInventoryService _inventory = new();
    private readonly CsvExportService _csv = new();

    private string _filter = string.Empty;
    private bool _isBusy;
    private string _status = "Ready.";

    public MainViewModel()
    {
        RefreshCommand = new RelayCommand(Refresh, () => !IsBusy);
        ExportCommand = new RelayCommand(Export, () => !IsBusy && Apps.Count > 0);
        Refresh();
    }

    public ObservableCollection<InstalledApp> Apps { get; } = new();

    public string Filter
    {
        get => _filter;
        set { if (Set(ref _filter, value)) ApplyFilter(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                ExportCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ExportCommand { get; }

    private IReadOnlyList<InstalledApp> _allApps = Array.Empty<InstalledApp>();

    private async void Refresh()
    {
        IsBusy = true;
        Status = "Scanning installed software…";
        try
        {
            _allApps = await Task.Run(() => _inventory.GetInstalledApps());
            ApplyFilter();
            Status = $"{_allApps.Count} program(s) found.";
        }
        catch (Exception ex)
        {
            Status = "Scan failed: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
            ExportCommand.RaiseCanExecuteChanged();
        }
    }

    private void ApplyFilter()
    {
        var q = _filter.Trim();
        var filtered = string.IsNullOrEmpty(q)
            ? _allApps
            : _allApps.Where(a =>
                a.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                || (a.Publisher?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false));

        Apps.Clear();
        foreach (var a in filtered)
            Apps.Add(a);

        ExportCommand.RaiseCanExecuteChanged();
    }

    private void Export()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export inventory",
            FileName = $"software-inventory-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            Filter = "CSV file (*.csv)|*.csv",
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _csv.Export(Apps, dialog.FileName);
            Status = $"Exported {Apps.Count} row(s) to {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
