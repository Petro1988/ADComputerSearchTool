using ADComputerSearchTool.Commands;
using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ADComputerSearchTool.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IActiveDirectoryService _activeDirectoryService;
    private readonly IClipboardService _clipboardService;
    private readonly IExcelExportService _excelExportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDialogService _dialogService;

    private string _computerNameFilter =
        string.Empty;

    private string _organizationalUnitFilter =
        string.Empty;

    private string _descriptionFilter =
        string.Empty;

    private string _groupFilter =
        string.Empty;

    private string _ipAddressFilter =
        string.Empty;

    private ComputerStatusFilter _selectedStatus =
        ComputerStatusFilter.All;

    private ComputerRecord? _selectedComputer;

    private bool _isBusy;

    private string _statusMessage =
        "Bereit";

    public MainWindowViewModel(
        IActiveDirectoryService activeDirectoryService,
        IClipboardService clipboardService,
        IExcelExportService excelExportService,
        IFileDialogService fileDialogService,
        IDialogService dialogService)
    {
        _activeDirectoryService =
            activeDirectoryService ??
            throw new ArgumentNullException(
                nameof(activeDirectoryService));

        _clipboardService =
            clipboardService ??
            throw new ArgumentNullException(
                nameof(clipboardService));

        _excelExportService =
            excelExportService ??
            throw new ArgumentNullException(
                nameof(excelExportService));

        _fileDialogService =
            fileDialogService ??
            throw new ArgumentNullException(
                nameof(fileDialogService));

        _dialogService =
            dialogService ??
            throw new ArgumentNullException(
                nameof(dialogService));

        SearchCommand =
            new AsyncRelayCommand(
                SearchAsync,
                () => !IsBusy);

        ClearCommand =
            new RelayCommand(
                _ => Clear(),
                _ => !IsBusy);

        ExportCommand =
            new RelayCommand(
                _ => Export(),
                _ =>
                    Results.Count > 0 &&
                    !IsBusy);

        CopyRowCommand =
            new RelayCommand(
                parameter =>
                    CopyRow(
                        parameter as ComputerRecord),
                parameter =>
                    parameter is ComputerRecord &&
                    !IsBusy);
    }

    public ObservableCollection<ComputerRecord> Results { get; } =
        new();

    public IReadOnlyList<ComputerStatusFilter> StatusOptions { get; } =
        Enum.GetValues<ComputerStatusFilter>();

    public string ComputerNameFilter
    {
        get => _computerNameFilter;

        set => SetProperty(
            ref _computerNameFilter,
            value);
    }

    public string OrganizationalUnitFilter
    {
        get => _organizationalUnitFilter;

        set => SetProperty(
            ref _organizationalUnitFilter,
            value);
    }

    public string DescriptionFilter
    {
        get => _descriptionFilter;

        set => SetProperty(
            ref _descriptionFilter,
            value);
    }

    public string GroupFilter
    {
        get => _groupFilter;

        set => SetProperty(
            ref _groupFilter,
            value);
    }

    public string IpAddressFilter
    {
        get => _ipAddressFilter;

        set => SetProperty(
            ref _ipAddressFilter,
            value);
    }

    public ComputerStatusFilter SelectedStatus
    {
        get => _selectedStatus;

        set => SetProperty(
            ref _selectedStatus,
            value);
    }

    public ComputerRecord? SelectedComputer
    {
        get => _selectedComputer;

        set
        {
            if (!SetProperty(
                    ref _selectedComputer,
                    value))
            {
                return;
            }

            RaiseCommandStates();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;

        private set
        {
            if (!SetProperty(
                    ref _isBusy,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsNotBusy));

            RaiseCommandStates();
        }
    }

    public bool IsNotBusy =>
        !IsBusy;

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public string ResultCountText =>
        $"{Results.Count} Treffer";

    public ICommand SearchCommand { get; }

    public ICommand ClearCommand { get; }

    public ICommand ExportCommand { get; }

    public ICommand CopyRowCommand { get; }

    private async Task SearchAsync()
    {
        IsBusy = true;

        StatusMessage =
            "Active Directory wird durchsucht und " +
            "IP-Adressen werden ermittelt...";

        try
        {
            ComputerSearchCriteria criteria =
                CreateSearchCriteria();

            IReadOnlyList<ComputerRecord> computers =
                await _activeDirectoryService
                    .SearchComputersAsync(criteria);

            SetResults(computers);

            SelectedComputer =
                null;

            StatusMessage =
                "Suche abgeschlossen.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage =
                "Die Suche wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                "Fehler bei der AD-Abfrage.";

            _dialogService.ShowError(
                "Die Active-Directory-Abfrage ist fehlgeschlagen:\n\n" +
                exception.Message,
                "Fehler bei der AD-Abfrage");
        }
        finally
        {
            IsBusy = false;

            RefreshResultInformation();
        }
    }

    private ComputerSearchCriteria CreateSearchCriteria()
    {
        return new ComputerSearchCriteria
        {
            ComputerName =
                ComputerNameFilter.Trim(),

            OrganizationalUnit =
                OrganizationalUnitFilter.Trim(),

            Description =
                DescriptionFilter.Trim(),

            GroupName =
                GroupFilter.Trim(),

            IpAddress =
                IpAddressFilter.Trim(),

            Status =
                SelectedStatus
        };
    }

    private void SetResults(
        IEnumerable<ComputerRecord> computers)
    {
        Results.Clear();

        foreach (ComputerRecord computer in computers)
        {
            Results.Add(computer);
        }
    }

    private void Clear()
    {
        ComputerNameFilter =
            string.Empty;

        OrganizationalUnitFilter =
            string.Empty;

        DescriptionFilter =
            string.Empty;

        GroupFilter =
            string.Empty;

        IpAddressFilter =
            string.Empty;

        SelectedStatus =
            ComputerStatusFilter.All;

        SelectedComputer =
            null;

        Results.Clear();

        StatusMessage =
            "Bereit";

        RefreshResultInformation();
    }

    private void Export()
    {
        if (Results.Count == 0)
        {
            _dialogService.ShowInformation(
                "Es sind keine Ergebnisse zum Exportieren vorhanden.",
                "Excel-Export");

            return;
        }

        string? filePath =
            _fileDialogService
                .SelectExcelSavePath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusMessage =
                "Der Excel-Export wurde abgebrochen.";

            return;
        }

        try
        {
            _excelExportService.Export(
                filePath,
                Results);

            StatusMessage =
                "Der Excel-Export wurde erfolgreich erstellt.";

            _dialogService.ShowInformation(
                "Der Excel-Export wurde erfolgreich erstellt.",
                "Excel-Export");
        }
        catch (Exception exception)
        {
            StatusMessage =
                "Der Excel-Export ist fehlgeschlagen.";

            _dialogService.ShowError(
                "Der Excel-Export ist fehlgeschlagen:\n\n" +
                exception.Message,
                "Fehler beim Excel-Export");
        }
    }

    private void CopyRow(
        ComputerRecord? computer)
    {
        if (computer == null)
        {
            _dialogService.ShowInformation(
                "Es wurde kein Computer ausgewählt.",
                "Zeile kopieren");

            return;
        }

        try
        {
            _clipboardService.CopyComputerRow(
                computer);

            StatusMessage =
                $"Die vollständige Zeile von {computer.Name} " +
                "wurde kopiert.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                "Die Zeile konnte nicht kopiert werden.";

            _dialogService.ShowError(
                "Die Zeile konnte nicht kopiert werden:\n\n" +
                exception.Message,
                "Fehler beim Kopieren");
        }
    }

    public void CopyCellValue(
        string? value,
        string? columnHeader)
    {
        string clipboardValue =
            value ?? string.Empty;

        string displayHeader =
            string.IsNullOrWhiteSpace(columnHeader)
                ? "Zelle"
                : columnHeader;

        try
        {
            _clipboardService.CopyText(
                clipboardValue);

            StatusMessage =
                $"{displayHeader} wurde kopiert.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                "Die Zelle konnte nicht kopiert werden.";

            _dialogService.ShowError(
                "Die Zelle konnte nicht kopiert werden:\n\n" +
                exception.Message,
                "Fehler beim Kopieren");
        }
    }

    public void ShowNoRowSelectedMessage()
    {
        _dialogService.ShowInformation(
            "Bitte mit der rechten Maustaste auf eine Tabellenzelle klicken.",
            "Zeile kopieren");
    }

    public void ShowNoCellSelectedMessage()
    {
        _dialogService.ShowInformation(
            "Bitte mit der rechten Maustaste direkt auf eine Tabellenzelle klicken.",
            "Zelle kopieren");
    }

    private void RefreshResultInformation()
    {
        OnPropertyChanged(
            nameof(ResultCountText));

        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (SearchCommand is AsyncRelayCommand searchCommand)
        {
            searchCommand.RaiseCanExecuteChanged();
        }

        if (ClearCommand is RelayCommand clearCommand)
        {
            clearCommand.RaiseCanExecuteChanged();
        }

        if (ExportCommand is RelayCommand exportCommand)
        {
            exportCommand.RaiseCanExecuteChanged();
        }

        if (CopyRowCommand is RelayCommand copyRowCommand)
        {
            copyRowCommand.RaiseCanExecuteChanged();
        }
    }
}