using ADComputerSearchTool.Models;
using ADComputerSearchTool.Services.Interfaces;
using ClosedXML.Excel;

namespace ADComputerSearchTool.Services;

public sealed class ExcelExportService : IExcelExportService
{
    public void Export(
        string filePath,
        IEnumerable<ComputerRecord> computers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(computers);

        using XLWorkbook workbook = new();

        IXLWorksheet worksheet =
            workbook.Worksheets.Add("AD-Computer");

        WriteHeaders(worksheet);
        WriteRows(worksheet, computers);
        FormatWorksheet(worksheet);

        workbook.SaveAs(filePath);
    }

    private static void WriteHeaders(
        IXLWorksheet worksheet)
    {
        string[] headers =
        {
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
        };

        for (int column = 0;
             column < headers.Length;
             column++)
        {
            worksheet
                .Cell(1, column + 1)
                .Value = headers[column];
        }
    }

    private static void WriteRows(
        IXLWorksheet worksheet,
        IEnumerable<ComputerRecord> computers)
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

        SetColumnWidths(worksheet);
        SetTextWrapping(worksheet);

        worksheet
            .Rows()
            .Style
            .Alignment
            .Vertical =
            XLAlignmentVerticalValues.Top;
    }

    private static void SetColumnWidths(
        IXLWorksheet worksheet)
    {
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
    }

    private static void SetTextWrapping(
        IXLWorksheet worksheet)
    {
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
    }
}