using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace analysis.Controllers
{
    public class SurveyController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Please upload a valid file (Excel or CSV).";
                return View();
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            var analysisResults = new Dictionary<string, Dictionary<string, int>>();

            try
            {
                if (extension == ".xlsx")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var stream = file.OpenReadStream())
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.First();
                        var totalRows = worksheet.Dimension.Rows;
                        var totalCols = worksheet.Dimension.Columns;

                        for (int col = 1; col <= totalCols; col++)
                        {
                            var columnTitle = worksheet.Cells[1, col].Text.Trim();
                            if (string.IsNullOrWhiteSpace(columnTitle)) continue;

                            var frequencies = new Dictionary<string, int>();

                            for (int row = 2; row <= totalRows; row++)
                            {
                                var cellValue = worksheet.Cells[row, col].Text.Trim();
                                if (string.IsNullOrWhiteSpace(cellValue)) continue;

                                if (frequencies.ContainsKey(cellValue))
                                    frequencies[cellValue]++;
                                else
                                    frequencies[cellValue] = 1;
                            }

                            if (frequencies.Count > 0)
                                analysisResults[columnTitle] = frequencies;
                        }
                    }
                }
                else if (extension == ".csv" || extension == ".txt")
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        var lines = new List<string[]>();
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            var values = line.Split(',');
                            lines.Add(values);
                        }

                        if (lines.Count > 1)
                        {
                            var headers = lines[0];
                            for (int col = 0; col < headers.Length; col++)
                            {
                                var columnTitle = headers[col].Trim();
                                if (string.IsNullOrWhiteSpace(columnTitle)) continue;

                                var frequencies = new Dictionary<string, int>();

                                for (int row = 1; row < lines.Count; row++)
                                {
                                    if (col >= lines[row].Length) continue; // حماية من الأعمدة الفارغة
                                    var cellValue = lines[row][col].Trim();
                                    if (string.IsNullOrWhiteSpace(cellValue)) continue;

                                    if (frequencies.ContainsKey(cellValue))
                                        frequencies[cellValue]++;
                                    else
                                        frequencies[cellValue] = 1;
                                }

                                if (frequencies.Count > 0)
                                    analysisResults[columnTitle] = frequencies;
                            }
                        }
                    }
                }
                else
                {
                    ViewBag.Error = "Unsupported file type. Please upload an Excel (.xlsx) or CSV (.csv/.txt) file.";
                    return View();
                }

                return View(analysisResults);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred while analyzing the file: " + ex.Message;
                return View();
            }
        }
    }
}
