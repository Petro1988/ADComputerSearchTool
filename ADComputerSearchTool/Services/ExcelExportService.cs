using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using ClosedXML.Excel;

namespace ADComputerSearchTool.Services;

public sealed class ExcelExportService : IExcelExportService
{
    private static readonly string[] Headers =
    [
        "Computername",
        "IPv4-Adresse",
        "Status",
        "Beschreibung",
        "Organisationseinheit",
        "Mitglied von",
        "Letzte Anmeldung",
        "Betriebssystem",
        "DNS-Hostname",
        "Distinguished Name"
    ];

    public void Export(
        string filePath,
        IEnumerable<ComputerRecord> computers)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Der Speicherpfad darf nicht leer sein.",
                nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(computers);

        List<ComputerRecord> computerList =
            computers.ToList();

        if (computerList.Count == 0)
        {
            throw new InvalidOperationException(
                "Es sind keine Computer zum Exportieren vorhanden.");
        }

        using XLWorkbook workbook =
            new XLWorkbook();

        IXLWorksheet worksheet =
            workbook.Worksheets.Add(
                "AD-Computer");

        WriteHeaders(worksheet);
        WriteComputerRows(
            worksheet,
            computerList);

        FormatWorksheet(worksheet);

        workbook.SaveAs(filePath);
    }

    private static void WriteHeaders(
        IXLWorksheet worksheet)
    {
        for (int column = 0;
             column < Headers.Length;
             column++)
        {
            worksheet
                .Cell(1, column + 1)
                .Value = Headers[column];
        }
    }

    private static void WriteComputerRows(
        IXLWorksheet worksheet,
        IReadOnlyList<ComputerRecord> computers)
    {
        int row = 2;

        foreach (ComputerRecord computer in computers)
        {
            worksheet.Cell(row, 1).Value =
                computer.Name;

            worksheet.Cell(row, 2).Value =
                computer.IpAddress;

            worksheet.Cell(row, 3).Value =
                computer.Status;

            worksheet.Cell(row, 4).Value =
                computer.Description;

            worksheet.Cell(row, 5).Value =
                computer.OrganizationalUnit;

            worksheet.Cell(row, 6).Value =
                computer.MemberOf;

            if (computer.LastLogon.HasValue)
            {
                worksheet.Cell(row, 7).Value =
                    computer.LastLogon.Value;

                worksheet
                    .Cell(row, 7)
                    .Style
                    .DateFormat
                    .Format = "dd.MM.yyyy HH:mm";
            }

            worksheet.Cell(row, 8).Value =
                computer.OperatingSystem;

            worksheet.Cell(row, 9).Value =
                computer.DnsHostName;

            worksheet.Cell(row, 10).Value =
                computer.DistinguishedName;

            row++;
        }
    }

    private static void FormatWorksheet(
        IXLWorksheet worksheet)
    {
        IXLRange? usedRange =
            worksheet.RangeUsed();

        if (usedRange != null)
        {
            usedRange.CreateTable(
                "ADComputerTabelle");
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        worksheet.Column(1).Width = 22;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 14;
        worksheet.Column(4).Width = 35;
        worksheet.Column(5).Width = 45;
        worksheet.Column(6).Width = 60;
        worksheet.Column(7).Width = 22;
        worksheet.Column(8).Width = 30;
        worksheet.Column(9).Width = 40;
        worksheet.Column(10).Width = 55;

        worksheet
            .Column(4)
            .Style
            .Alignment
            .WrapText = true;

        worksheet
            .Column(5)
            .Style
            .Alignment
            .WrapText = true;

        worksheet
            .Column(6)
            .Style
            .Alignment
            .WrapText = true;

        worksheet
            .Column(10)
            .Style
            .Alignment
            .WrapText = true;

        worksheet
            .Rows()
            .Style
            .Alignment
            .Vertical =
            XLAlignmentVerticalValues.Top;
    }
}