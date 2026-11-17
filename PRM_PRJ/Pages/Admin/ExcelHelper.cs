using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace PRM_PRJ.Pages.Admin
{
    public static class ExcelHelper
    {
        public static List<List<string>> ReadRows(Stream stream, string fileName)
        {
            if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ReadCsvRows(stream);
            }

            if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Vui lòng chọn file Excel (.xlsx) hoặc (.csv).");
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = ResolveFirstWorksheetEntry(archive)
                ?? throw new InvalidOperationException("Không tìm thấy sheet dữ liệu trong file Excel.");

            using var sheetStream = sheetEntry.Open();
            var doc = XDocument.Load(sheetStream);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var rows = new List<List<string>>();
            foreach (var rowEl in doc.Descendants(ns + "row"))
            {
                var rowValues = new List<string>();
                foreach (var cellEl in rowEl.Elements(ns + "c"))
                {
                    var cellRef = cellEl.Attribute("r")?.Value;
                    var colIndex = string.IsNullOrWhiteSpace(cellRef)
                        ? rowValues.Count
                        : GetColumnIndex(cellRef);

                    while (rowValues.Count <= colIndex)
                    {
                        rowValues.Add(string.Empty);
                    }

                    rowValues[colIndex] = ReadCellValue(cellEl, ns, sharedStrings).Trim();
                }

                if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    rows.Add(rowValues);
                }
            }

            return rows;
        }

        public static bool TryParseExcelTime(string raw, out TimeSpan time)
        {
            time = default;
            var s = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return false;

            if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out time))
                return true;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                time = dt.TimeOfDay;
                return true;
            }

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
            {
                var fraction = serial - Math.Floor(serial);
                if (fraction >= 0 && fraction < 1)
                {
                    var totalMinutes = (int)Math.Round(fraction * 24 * 60);
                    time = TimeSpan.FromMinutes(totalMinutes);
                    return true;
                }
            }

            return false;
        }

        public static bool TryParseExcelDate(string raw, out DateTime date)
        {
            date = default;
            var s = raw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var acceptedFormats = new[]
            {
                "yyyy-MM-dd",
                "M/d/yyyy",
                "MM/dd/yyyy",
                "d/M/yyyy",
                "dd/MM/yyyy",
                "yyyy/MM/dd"
            };

            if (DateTime.TryParseExact(s, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                date = date.Date;
                return true;
            }

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaDate) &&
                oaDate > 1000 && oaDate < 2958465)
            {
                date = DateTime.FromOADate(oaDate).Date;
                return true;
            }

            return false;
        }

        public static byte[] BuildXlsx(string sheetName, IReadOnlyList<string[]> rows)
        {
            var safeSheetName = SecurityElement.Escape(sheetName) ?? "Sheet1";

            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(archive, "[Content_Types].xml",
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                    </Types>
                    """);

                WriteEntry(archive, "_rels/.rels",
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                    </Relationships>
                    """);

                WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                    """
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                    </Relationships>
                    """);

                WriteEntry(archive, "xl/workbook.xml",
                    $"""
                    <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                              xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                      <sheets>
                        <sheet name="{safeSheetName}" sheetId="1" r:id="rId1"/>
                      </sheets>
                    </workbook>
                    """);

                var sheetSb = new StringBuilder();
                sheetSb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
                sheetSb.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");

                for (var r = 0; r < rows.Count; r++)
                {
                    var rowNumber = r + 1;
                    sheetSb.Append($"""<row r="{rowNumber}">""");
                    var cols = rows[r];
                    for (var c = 0; c < cols.Length; c++)
                    {
                        var cellRef = $"{GetColumnName(c)}{rowNumber}";
                        var text = SecurityElement.Escape(cols[c] ?? string.Empty) ?? string.Empty;
                        sheetSb.Append($"""<c r="{cellRef}" t="inlineStr"><is><t>{text}</t></is></c>""");
                    }
                    sheetSb.Append("</row>");
                }

                sheetSb.Append("</sheetData></worksheet>");
                WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetSb.ToString());
            }

            return ms.ToArray();
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return new List<string>();

            using var s = entry.Open();
            var doc = XDocument.Load(s);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            return doc.Descendants(ns + "si")
                .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                .ToList();
        }

        private static ZipArchiveEntry? ResolveFirstWorksheetEntry(ZipArchive archive)
        {
            var direct = archive.GetEntry("xl/worksheets/sheet1.xml");
            if (direct != null) return direct;

            return archive.Entries.FirstOrDefault(e =>
                e.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) &&
                e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        }

        private static string ReadCellValue(XElement cellEl, XNamespace ns, IReadOnlyList<string> sharedStrings)
        {
            var type = cellEl.Attribute("t")?.Value;
            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat(cellEl.Descendants(ns + "t").Select(t => t.Value));
            }

            var rawValue = cellEl.Element(ns + "v")?.Value ?? string.Empty;
            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(rawValue, out var sstIndex) &&
                sstIndex >= 0 && sstIndex < sharedStrings.Count)
            {
                return sharedStrings[sstIndex];
            }

            return rawValue;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var index = 0;
            foreach (var ch in cellReference)
            {
                if (!char.IsLetter(ch)) break;
                index = (index * 26) + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            return Math.Max(0, index - 1);
        }

        private static string GetColumnName(int colIndex)
        {
            var name = new StringBuilder();
            var n = colIndex + 1;
            while (n > 0)
            {
                n--;
                name.Insert(0, (char)('A' + (n % 26)));
                n /= 26;
            }
            return name.ToString();
        }

        private static List<List<string>> ReadCsvRows(Stream stream)
        {
            var rows = new List<List<string>>();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = ParseCsvLine(line);
                if (cols.Any(c => !string.IsNullOrWhiteSpace(c)))
                {
                    rows.Add(cols);
                }
            }
            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }
    }
}
