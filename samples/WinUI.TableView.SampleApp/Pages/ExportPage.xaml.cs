using ClosedXML.Excel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ExportPage : Page
{
    public ExportPage()
    {
        InitializeComponent();
    }

    private async void OnExportAllContent(object sender, TableViewExportContentEventArgs e)
    {
        if (exportToExcel.IsOn is true)
        {
            e.Handled = true;

            await ExportToExcel(true);
        }
    }

    private async void OnExportSelectedContent(object sender, TableViewExportContentEventArgs e)
    {
        if (exportToExcel.IsOn is true)
        {
            e.Handled = true;

            await ExportToExcel(false);
        }
    }

    private async Task ExportToExcel(bool all)
    {
        var separator = '|';
        var content = all ? tableView.GetAllContent(true, separator) : tableView.GetSelectedContent(true, separator);
        var lines = content.Split('\n');
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("WinUI.TableView");

        for (var rowIndex = 0; rowIndex < lines.Length; rowIndex++)
        {
            var cells = lines[rowIndex].Split(separator);

            for (var colIndex = 0; colIndex < cells.Length; colIndex++)
            {
                worksheet.Cell(rowIndex + 1, colIndex + 1).Value = cells[colIndex];
            }
        }

        var savePicker = new FileSavePicker();
        savePicker.FileTypeChoices.Add("Excel Workbook", [".xlsx"]);
#if WINDOWS
        var hWnd = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        InitializeWithWindow.Initialize(savePicker, hWnd);
#endif

        var file = await savePicker.PickSaveFileAsync();
        using var stream = await file.OpenStreamForWriteAsync();
        stream.SetLength(0);
        workbook.SaveAs(stream);
    }
}
