using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;   // For .xls
using NPOI.XSSF.UserModel;  // For .xlsx
using NPOI.SS.UserModel;
using System.Data;
using System.Text;
using System.IO.Compression;

namespace PF_ESI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        [HttpPost("upload-final")]
        public async Task<IActionResult> UploadExcelFinal(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid Excel file.");

            // Save the uploaded file temporarily
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
            using (var stream = new FileStream(tempPath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Read Excel to DataTable
            DataTable dt = ReadExcelToDataTable(tempPath);

            // Delete temp file
            System.IO.File.Delete(tempPath);

            // Build final content with #~# separator
            var builder = new StringBuilder();
            foreach (DataRow row in dt.Rows)
            {
                var fields = row.ItemArray.Select(f => f?.ToString()?.Replace("\"", "\"\"") ?? "");
                builder.AppendLine(string.Join("#~#", fields));
            }

            byte[] fileBytes = Encoding.UTF8.GetBytes(builder.ToString());

            // Return final text file
            return File(fileBytes, "text/plain", "final_output.txt");
        }

        private DataTable ReadExcelToDataTable(string filePath)
        {
            DataTable dt = new DataTable();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook;

            if (Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                workbook = new HSSFWorkbook(fs); // Excel 97-2003
            else
                workbook = new XSSFWorkbook(fs); // Excel 2007+

            ISheet sheet = workbook.GetSheetAt(0); // First sheet
            IRow headerRow = sheet.GetRow(0);
            int cellCount = headerRow.LastCellNum;

            // Add columns
            for (int i = 0; i < cellCount; i++)
                dt.Columns.Add(headerRow.GetCell(i)?.ToString() ?? $"Column{i + 1}");

            IFormulaEvaluator evaluator = WorkbookFactory.CreateFormulaEvaluator(workbook);

            // Add rows
            for (int i = 1; i <= sheet.LastRowNum; i++) // Start from row 1 to skip header
            {
                IRow row = sheet.GetRow(i);
                if (row == null) continue;

                var rowValues = new object[cellCount];
                for (int j = 0; j < cellCount; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell == null)
                    {
                        rowValues[j] = "";
                        continue;
                    }

                    switch (cell.CellType)
                    {
                        case CellType.Formula:
                            var evaluated = evaluator.Evaluate(cell);
                            if (evaluated.CellType == CellType.Numeric)
                                rowValues[j] = evaluated.NumberValue;
                            else if (evaluated.CellType == CellType.String)
                                rowValues[j] = evaluated.StringValue;
                            else
                                rowValues[j] = "";
                            break;
                        case CellType.Numeric:
                            rowValues[j] = cell.NumericCellValue;
                            break;
                        case CellType.String:
                            rowValues[j] = cell.StringCellValue;
                            break;
                        default:
                            rowValues[j] = cell.ToString();
                            break;
                    }
                }
                dt.Rows.Add(rowValues);
            }

            return dt;
        }



        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadExcelMultiple(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Please upload at least one Excel file.");

            // Create a memory stream for the ZIP
            using var zipStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    // Save temporarily
                    var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(file.FileName));
                    using (var fs = new FileStream(tempPath, FileMode.Create))
                        await file.CopyToAsync(fs);

                    // Read to DataTable
                    DataTable dt = ReadExcelToDataTable(tempPath);

                    // Delete temp
                    System.IO.File.Delete(tempPath);

                    // Build text content
                    var builder = new StringBuilder();
                    foreach (DataRow row in dt.Rows)
                    {
                        var fields = row.ItemArray.Select(f => f?.ToString()?.Replace("\"", "\"\"") ?? "");
                        builder.AppendLine(string.Join("#~#", fields));
                    }

                    // Add as .txt with same file name
                    var txtFileName = Path.GetFileNameWithoutExtension(file.FileName) + ".txt";
                    var entry = zipArchive.CreateEntry(txtFileName, CompressionLevel.Optimal);
                    using (var entryStream = entry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        writer.Write(builder.ToString());
                    }
                }
            }

            zipStream.Position = 0;
            return File(zipStream.ToArray(), "application/zip", "ConvertedFiles.zip");
        }

        private DataTable ReadExcelToDataTableMultiple(string filePath)
        {
            DataTable dt = new DataTable();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            IWorkbook workbook;

            if (Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                workbook = new HSSFWorkbook(fs);
            else
                workbook = new XSSFWorkbook(fs);

            ISheet sheet = workbook.GetSheetAt(0);
            IRow headerRow = sheet.GetRow(0);
            int cellCount = headerRow.LastCellNum;

            for (int i = 0; i < cellCount; i++)
                dt.Columns.Add(headerRow.GetCell(i)?.ToString() ?? $"Column{i + 1}");

            IFormulaEvaluator evaluator = WorkbookFactory.CreateFormulaEvaluator(workbook);

            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row == null) continue;

                var rowValues = new object[cellCount];
                for (int j = 0; j < cellCount; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell == null)
                    {
                        rowValues[j] = "";
                        continue;
                    }

                    switch (cell.CellType)
                    {
                        case CellType.Formula:
                            var evaluated = evaluator.Evaluate(cell);
                            if (evaluated.CellType == CellType.Numeric)
                                rowValues[j] = evaluated.NumberValue;
                            else if (evaluated.CellType == CellType.String)
                                rowValues[j] = evaluated.StringValue;
                            else
                                rowValues[j] = "";
                            break;
                        case CellType.Numeric:
                            rowValues[j] = cell.NumericCellValue;
                            break;
                        case CellType.String:
                            rowValues[j] = cell.StringCellValue;
                            break;
                        default:
                            rowValues[j] = cell.ToString();
                            break;
                    }
                }
                dt.Rows.Add(rowValues);
            }

            return dt;
        }


    }
}
