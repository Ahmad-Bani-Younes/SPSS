using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Data;
using System.Linq;
using System.Collections.Generic;

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
            if (file == null || file.Length == 0 || !file.FileName.EndsWith(".xlsx"))
            {
                ViewBag.Error = "Please upload a valid Excel (.xlsx) file.";
                return View();
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var analysisResults = new Dictionary<string, Dictionary<string, int>>();

            try
            {
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
