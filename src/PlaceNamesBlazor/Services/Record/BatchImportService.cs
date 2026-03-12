using System.Globalization;
using System.IO.Compression;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Dropdowns;
using PlaceNamesBlazor.Contracts.Record;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Services.ImageStorage;

namespace PlaceNamesBlazor.Services.Record;

public class BatchImportService : IBatchImportService
{
    private readonly IRecordService _recordService;
    private readonly IDbContextFactory<PlaceNamesDbContext> _dbFactory;
    private readonly IImageStorageService _imageStorage;

    public BatchImportService(IRecordService recordService, IDbContextFactory<PlaceNamesDbContext> dbFactory, IImageStorageService imageStorage)
    {
        _recordService = recordService;
        _dbFactory = dbFactory;
        _imageStorage = imageStorage;
    }

    public async Task<BatchImportResultDto> ProcessExcelAsync(Stream excelStream, CancellationToken cancellationToken = default)
    {
        // Use a dedicated context for dropdown data so we never share the scoped DbContext with the UI (avoids "second operation" errors).
        List<FylkeItemDto> fylker;
        List<StempeltypeItemDto> stempeltyper;
        await using (var db = await _dbFactory.CreateDbContextAsync(cancellationToken))
        {
            fylker = await db.Fylker
                .OrderBy(f => f.FylkeNavn)
                .Select(f => new FylkeItemDto { FylkeId = f.FylkeId, FylkeNavn = f.FylkeNavn })
                .ToListAsync(cancellationToken);
            stempeltyper = await db.Stempeltyper
                .OrderBy(s => s.Hovedstempeltype)
                .Select(s => new StempeltypeItemDto
                {
                    StempeltypeId = s.StempeltypeId,
                    Hovedstempeltype = s.Hovedstempeltype,
                    StempeltypeFullTekst = s.StempeltypeFullTekst
                })
                .ToListAsync(cancellationToken);
        }
        var fylkeByName = fylker.ToDictionary(f => f.FylkeNavn, StringComparer.OrdinalIgnoreCase);
        var stempeltypeByCode = stempeltyper.ToDictionary(s => s.Hovedstempeltype, StringComparer.OrdinalIgnoreCase);

        // Copy to MemoryStream (BrowserFileStream is async-only; ClosedXML needs sync). Then detect ZIP bundle (Excel + images) vs plain Excel.
        await using var memStream = new MemoryStream();
        await excelStream.CopyToAsync(memStream, cancellationToken);
        memStream.Position = 0;

        string? imageBasePath = null;
        string? extractDir = null;
        Stream? excelStreamToUse = null;
        string? excelFilePath = null;

        try
        {
            if (TryOpenAsZipBundle(memStream, out var zipExcelPath, out var zipExtractDir))
            {
                imageBasePath = zipExtractDir;
                extractDir = zipExtractDir;
                excelFilePath = zipExcelPath;
            }
            else
            {
                memStream.Position = 0;
                excelStreamToUse = memStream;
            }

            using var book = excelFilePath != null
                ? new XLWorkbook(excelFilePath)
                : new XLWorkbook(excelStreamToUse!);
            var sheet = book.Worksheet("Fields");
            if (sheet == null)
                return new BatchImportResultDto { Errors = ["Sheet 'Fields' not found."] };

            var used = sheet.RangeUsed();
            if (used == null)
                return new BatchImportResultDto { Total = 0, Errors = ["Sheet 'Fields' is empty."] };

            var header = used.FirstRow();
            var rows = used.Rows().Skip(1);
            int colFylke = GetColumn(header, "Fylke");
            int colStempletype = GetColumn(header, "Stempletype");
            int colPoststed = GetColumn(header, "Poststed");
            int colStempeltekst = GetColumn(header, "Stempeltekst");
            int colKommune = GetColumn(header, "Kommune");
            int colForste = GetColumn(header, "Første kjente");
            int colSiste = GetColumn(header, "Siste kjente");
            int colKommentar = GetColumn(header, "Kommentar");
            int colBilde = GetColumn(header, "Bilde");
            int colStempeldiameter = GetColumn(header, "Stempeldiameter");
            int colBokstavhoeyde = GetColumn(header, "Bokstavhøyde");
            int colAndreMaal = GetColumn(header, "Andre mål");
            int colStempelfarge = GetColumn(header, "Stempelfarge");
            int colTapsmelding = GetColumn(header, "Tapsmelding");
            int colReparasjoner = GetColumn(header, "Reparasjoner");
            int colDatoAvtrykkIPm = GetColumn(header, "Dato avtrykk i PM");
            int colDatoFraGravoer = GetColumn(header, "Dato fra gravør");
            int colDatoFraIntendantur = GetColumn(header, "Dato fra intendantur");
            int colDatoFraOverordnet = GetColumn(header, "Dato fra overordnet");
            int colDatoForInnlevering = GetColumn(header, "Dato for innlevering");
            int colDatoInnlevertIntendantur = GetColumn(header, "Dato innlevert intendantur");

            if (colKommune <= 0 || colPoststed <= 0)
                return new BatchImportResultDto { Errors = ["Required columns missing: Poststed, Kommune."] };

            var errors = new List<string>();
            var batchItems = new List<(int RowIndex, CreateRecordRequest Request)>();
            int rowIndex = 1;

            foreach (var row in rows)
            {
                rowIndex++;
                try
                {
                    var fylkeName = GetCellString(row, colFylke);
                    if (string.IsNullOrWhiteSpace(fylkeName) || !fylkeByName.TryGetValue(fylkeName.Trim(), out var fylkeItem))
                    {
                        errors.Add($"Row {rowIndex}: Fylke '{fylkeName}' not found.");
                        continue;
                    }

                    var code = GetCellString(row, colStempletype);
                    if (string.IsNullOrWhiteSpace(code) || !stempeltypeByCode.TryGetValue(code.Trim(), out var stempelItem))
                    {
                        errors.Add($"Row {rowIndex}: Stempletype '{code}' not found.");
                        continue;
                    }

                    var poststed = GetCellString(row, colPoststed);
                    var kommune = GetCellString(row, colKommune);
                    if (string.IsNullOrWhiteSpace(poststed) || string.IsNullOrWhiteSpace(kommune))
                    {
                        errors.Add($"Row {rowIndex}: Missing Poststed or Kommune.");
                        continue;
                    }

                    var stempeltekst = GetCellString(row, colStempeltekst);
                    if (string.IsNullOrWhiteSpace(stempeltekst))
                        stempeltekst = poststed;

                    var forste = FormatDate(GetCellValue(row, colForste));
                    var siste = FormatDate(GetCellValue(row, colSiste));
                    var kommentar = GetCellString(row, colKommentar);

                    string? bildePath = null;
                    var bildeCellValue = GetCellString(row, colBilde);
                    if (!string.IsNullOrWhiteSpace(bildeCellValue))
                    {
                        var path = ResolveImagePath(bildeCellValue.Trim(), imageBasePath);
                        if (path != null && File.Exists(path))
                        {
                            try
                            {
                                await using var fs = File.OpenRead(path);
                                var fileName = Path.GetFileName(path);
                                if (!string.IsNullOrEmpty(fileName))
                                    bildePath = await _imageStorage.UploadAsync(fs, fileName, "stamp", cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Row {rowIndex}: Bilde copy failed ({path}): {ex.Message}");
                            }
                        }
                    }

                var request = new CreateRecordRequest
                {
                    Poststed = poststed.Trim(),
                    Kommune = kommune.Trim(),
                    FylkeId = fylkeItem.FylkeId,
                    StempeltypeId = stempelItem.StempeltypeId,
                    StempeltekstOppe = stempeltekst.Trim(),
                    ForsteKjente = forste,
                    SisteKjente = siste,
                    Kommentar = string.IsNullOrWhiteSpace(kommentar) ? null : kommentar.Trim(),
                    BildePath = bildePath,
                    Stempeldiameter = GetCellDecimal(row, colStempeldiameter),
                    Bokstavhoeyde = GetCellDecimal(row, colBokstavhoeyde),
                    AndreMaal = NullIfWhiteSpace(GetCellString(row, colAndreMaal)),
                    Stempelfarge = NullIfWhiteSpace(GetCellString(row, colStempelfarge)),
                    Tapsmelding = NullIfWhiteSpace(GetCellString(row, colTapsmelding)),
                    Reparasjoner = NullIfWhiteSpace(GetCellString(row, colReparasjoner)),
                    DatoAvtrykkIPm = NullIfWhiteSpace(GetCellString(row, colDatoAvtrykkIPm)),
                    DatoFraGravoer = FormatDate(GetCellValue(row, colDatoFraGravoer)),
                    DatoFraIntendanturTilOverordnetPostkontor = FormatDate(GetCellValue(row, colDatoFraIntendantur)),
                    DatoFraOverordnetPostkontor = FormatDate(GetCellValue(row, colDatoFraOverordnet)),
                    DatoForInnleveringTilOverordnetPostkontor = FormatDate(GetCellValue(row, colDatoForInnlevering)),
                    DatoInnlevertIntendantur = FormatDate(GetCellValue(row, colDatoInnlevertIntendantur))
                };
                batchItems.Add((rowIndex, request));
            }
            catch (Exception ex)
            {
                errors.Add($"Row {rowIndex}: {ex.Message}");
            }
        }

            int totalRows = rowIndex - 1;
            int imported = 0;
            if (batchItems.Count > 0)
            {
                var (batchImported, batchErrors) = await _recordService.CreateBatchAsync(batchItems, cancellationToken);
                imported = batchImported;
                errors.AddRange(batchErrors);
            }
            int skipped = totalRows - imported;

            return new BatchImportResultDto
            {
                Total = totalRows,
                Imported = imported,
                Skipped = skipped,
                Errors = errors.Take(100).ToList()
            };
        }
        finally
        {
            if (!string.IsNullOrEmpty(extractDir) && Directory.Exists(extractDir))
            {
                try { Directory.Delete(extractDir, recursive: true); } catch { /* ignore */ }
            }
        }
    }

    /// <summary>If the stream is a ZIP containing a root-level .xlsx and image files, extract and return (excelPath, extractDir). Otherwise false.</summary>
    private static bool TryOpenAsZipBundle(Stream stream, out string? excelPath, out string? extractDir)
    {
        excelPath = null;
        extractDir = null;
        try
        {
            stream.Position = 0;
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var entries = zip.Entries;
            ZipArchiveEntry? xlsxEntry = null;
            var hasImage = false;
            foreach (var e in entries)
            {
                var name = e.FullName.Replace('\\', '/').TrimEnd('/');
                if (string.IsNullOrEmpty(name)) continue;
                if (name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && !name.Contains('/'))
                {
                    xlsxEntry = e;
                    continue;
                }
                var ext = Path.GetExtension(name);
                if (ext.Length > 0 && (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".png", StringComparison.OrdinalIgnoreCase) || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase) || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)))
                    hasImage = true;
            }
            if (xlsxEntry == null || !hasImage)
            {
                stream.Position = 0;
                return false;
            }
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            zip.ExtractToDirectory(dir);
            var fullXlsxPath = Path.Combine(dir, xlsxEntry.FullName.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullXlsxPath))
            {
                Directory.Delete(dir, recursive: true);
                stream.Position = 0;
                return false;
            }
            excelPath = fullXlsxPath;
            extractDir = dir;
            return true;
        }
        catch
        {
            stream.Position = 0;
            return false;
        }
    }

    /// <summary>Resolve Bilde column: if imageBasePath is set, treat value as relative path; otherwise absolute. Returns null if path would escape base (security).</summary>
    private static string? ResolveImagePath(string bildeCellValue, string? imageBasePath)
    {
        if (string.IsNullOrWhiteSpace(bildeCellValue)) return null;
        if (string.IsNullOrEmpty(imageBasePath))
            return bildeCellValue;
        var combined = Path.Combine(imageBasePath, bildeCellValue.Replace('/', Path.DirectorySeparatorChar));
        var full = Path.GetFullPath(combined);
        var baseFull = Path.GetFullPath(imageBasePath).TrimEnd(Path.DirectorySeparatorChar);
        if (!full.StartsWith(baseFull + Path.DirectorySeparatorChar, StringComparison.Ordinal) && full != baseFull)
            return null;
        return full;
    }

    private static int GetColumn(IXLRangeRow header, string name)
    {
        for (int c = 1; c <= 50; c++)
        {
            var cellVal = header.Cell(c).GetString()?.Trim();
            if (string.Equals(cellVal, name, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return 0;
    }

    private static string GetCellString(IXLRangeRow row, int col)
    {
        if (col <= 0) return "";
        var v = row.Cell(col).GetString();
        return v?.Trim() ?? "";
    }

    private static object? GetCellValue(IXLRangeRow row, int col)
    {
        if (col <= 0) return null;
        return row.Cell(col).Value;
    }

    private static string? FormatDate(object? value)
    {
        if (value == null) return null;
        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return s;
    }

    private static string? NullIfWhiteSpace(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim();
    }

    private static decimal? GetCellDecimal(IXLRangeRow row, int col)
    {
        if (col <= 0) return null;
        var v = row.Cell(col).Value;
        var s = v.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        return null;
    }
}
