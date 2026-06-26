using InvestmentTracker.Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvestmentTracker.Server.Services
{
    public class MoexService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MoexService> _logger;

        public MoexService(HttpClient httpClient, ILogger<MoexService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal?> GetCurrentPriceAsync(string ticker)
        {
            try
            {
                var url = $"https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/{ticker.ToUpper()}.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                if (!root.TryGetProperty("marketdata", out var marketdata))
                {
                    _logger.LogWarning("No marketdata block in response for {Ticker}", ticker);
                    return null;
                }

                if (!marketdata.TryGetProperty("columns", out var columns) ||
                    !marketdata.TryGetProperty("data", out var data) ||
                    data.GetArrayLength() == 0)
                {
                    _logger.LogWarning("Missing columns/data in marketdata for {Ticker}", ticker);
                    return null;
                }

                // Ищем индекс колонки LAST
                int lastIdx = -1;
                for (int i = 0; i < columns.GetArrayLength(); i++)
                {
                    if (columns[i].GetString() == "LAST")
                    {
                        lastIdx = i;
                        break;
                    }
                }

                if (lastIdx == -1)
                {
                    _logger.LogWarning("Column LAST not found for {Ticker}", ticker);
                    return null;
                }

                var row = data[0];
                if (row.GetArrayLength() <= lastIdx)
                {
                    _logger.LogWarning("Row has insufficient elements for LAST column in {Ticker}", ticker);
                    return null;
                }

                var lastElement = row[lastIdx];
                if (lastElement.ValueKind == JsonValueKind.Number && lastElement.TryGetDecimal(out var price))
                {
                    return price;
                }
                else if (lastElement.ValueKind == JsonValueKind.String && decimal.TryParse(lastElement.GetString(), out var parsedPrice))
                {
                    return parsedPrice;
                }

                _logger.LogWarning("LAST value is null or not a number for {Ticker}", ticker);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching price for {Ticker}", ticker);
                return null;
            }
        }

        public async Task<SecurityInfo?> GetSecurityInfoAsync(string tickerOrIsin)
        {
            try
            {
                // Если передали ISIN, нужно сначала найти тикер. Пока предполагаем, что ищем по тикеру.
                // Можно сделать универсальный метод, но для начала примем, что вход - тикер.
                var url = $"https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/{tickerOrIsin.ToUpper()}.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                if (!root.TryGetProperty("securities", out var securitiesBlock))
                {
                    _logger.LogWarning("No securities block for {Ticker}", tickerOrIsin);
                    return null;
                }

                if (!securitiesBlock.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                    return null;

                var columns = securitiesBlock.GetProperty("columns");
                // Находим индексы нужных полей
                int secidIdx = FindColumnIndex(columns, "SECID");
                int isinIdx = FindColumnIndex(columns, "ISIN");
                int nameIdx = FindColumnIndex(columns, "SHORTNAME");
                int secTypeIdx = FindColumnIndex(columns, "SECTYPE");

                var row = data[0];
                string? secid = secidIdx >= 0 ? row[secidIdx].GetString() : null;
                string? isin = isinIdx >= 0 ? row[isinIdx].GetString() : null;
                string? name = nameIdx >= 0 ? row[nameIdx].GetString() : null;
                string? secType = secTypeIdx >= 0 ? row[secTypeIdx].GetString() : null;

                if (secid == null || name == null)
                    return null;

                int? assetTypeId = MapSecTypeToAssetTypeId(secType);

                return new SecurityInfo
                {
                    Ticker = secid,
                    Isin = isin,
                    Name = name,
                    AssetTypeId = assetTypeId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security info for {Ticker}", tickerOrIsin);
                return null;
            }
        }

        private int FindColumnIndex(JsonElement columns, string name)
        {
            for (int i = 0; i < columns.GetArrayLength(); i++)
            {
                if (columns[i].GetString() == name)
                    return i;
            }
            return -1;
        }

        private int? MapSecTypeToAssetTypeId(string? secType)
        {
            // Маппинг: MOEX SECTYPE -> наш AssetType Id.
            // В сидах у нас: 1 - Акция, 2 - Облигация, 3 - ПИФ, 4 - ETF.
            // На MOEX: "1" - акция, "2" - облигация, "3" - депозитарная расписка, "9" - пай, "E" - ETF и т.д.
            // Можно хранить в конфиге, но для простоты захардкодим.
            return secType switch
            {
                "1" => 1, // Акция
                "2" => 2, // Облигация
                "9" => 3, // ПИФ
                "E" => 4, // ETF
                _ => null
            };
        }

        public class SecurityInfo
        {
            public string Ticker { get; set; } = string.Empty;
            public string? Isin { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? AssetTypeId { get; set; }
        }

        public async Task<decimal?> GetChangePercentSafeAsync(string ticker)
        {
            try
            {
                var url = $"https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/{ticker.ToUpper()}.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                if (!root.TryGetProperty("marketdata", out var md) ||
                    !md.TryGetProperty("columns", out var columns) ||
                    !md.TryGetProperty("data", out var data) ||
                    data.GetArrayLength() == 0)
                    return null;

                var cols = columns.EnumerateArray().Select(c => c.GetString()).ToArray();
                int idx = -1;
                for (int i = 0; i < cols.Length; i++)
                {
                    if (string.Equals(cols[i], "LASTCHANGEPRCNT", StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx == -1) return null;

                var value = data[0][idx];
                if (value.ValueKind == JsonValueKind.Number)
                    return value.GetDecimal();
                if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                    return parsed;
                return null;
            }
            catch
            {
                return null; // любая ошибка – просто прочерк
            }
        }

        public async Task<IndexInfo?> GetIndexInfoAsync(string ticker)
        {
            try
            {
                var url = $"https://iss.moex.com/iss/engines/stock/markets/index/securities/{ticker.ToUpper()}.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                decimal? value = null;
                decimal? changePct = null;

                // 1. Значение индекса ищем в marketdata (LAST, LCLOSEPRICE, CURRENTVALUE) или securities (CURRENTVALUE, LAST, CLOSE)
                if (root.TryGetProperty("marketdata", out var md) &&
                    md.TryGetProperty("columns", out var mdCols) &&
                    md.TryGetProperty("data", out var mdData) &&
                    mdData.GetArrayLength() > 0)
                {
                    var mdColumnsList = mdCols.EnumerateArray().Select(c => c.GetString()).ToArray();
                    int? valueIdx = null;
                    foreach (var name in new[] { "LAST", "LCLOSEPRICE", "CURRENTVALUE" })
                    {
                        valueIdx = FindColumnIndexCaseInsensitive(mdColumnsList, name);
                        if (valueIdx >= 0) break;
                    }

                    if (valueIdx >= 0 && mdData[0].GetArrayLength() > valueIdx)
                    {
                        var val = mdData[0][valueIdx.Value];
                        value = ParseDecimal(val);
                    }

                    // Изменение в marketdata: LASTCHANGEPRCNT, CHANGE, LASTCHANGE
                    int? chgIdx = null;
                    foreach (var name in new[] { "LASTCHANGEPRCNT", "CHANGE", "LASTCHANGE" })
                    {
                        chgIdx = FindColumnIndexCaseInsensitive(mdColumnsList, name);
                        if (chgIdx >= 0) break;
                    }

                    if (chgIdx >= 0 && mdData[0].GetArrayLength() > chgIdx)
                    {
                        var chgVal = mdData[0][chgIdx.Value];
                        changePct = ParseDecimal(chgVal);
                    }
                }

                // 2. Если значение не найдено в marketdata, ищем в securities (CURRENTVALUE, LAST, CLOSE)
                if (!value.HasValue && root.TryGetProperty("securities", out var sec) &&
                    sec.TryGetProperty("columns", out var secCols) &&
                    sec.TryGetProperty("data", out var secData) &&
                    secData.GetArrayLength() > 0)
                {
                    var secColumnsList = secCols.EnumerateArray().Select(c => c.GetString()).ToArray();
                    int? valueIdx = null;
                    foreach (var name in new[] { "CURRENTVALUE", "LAST", "CLOSE" })
                    {
                        valueIdx = FindColumnIndexCaseInsensitive(secColumnsList, name);
                        if (valueIdx >= 0) break;
                    }

                    if (valueIdx >= 0 && secData[0].GetArrayLength() > valueIdx)
                    {
                        var val = secData[0][valueIdx.Value];
                        value = ParseDecimal(val);
                    }
                }

                if (value.HasValue)
                {
                    return new IndexInfo { Value = value, ChangePct = changePct };
                }

                _logger.LogWarning("Could not find index value for {Ticker}", ticker);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting index info for {Ticker}", ticker);
                return null;
            }
        }

        // Вспомогательные методы 
        private int? FindColumnIndexCaseInsensitive(string[] columns, string name)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (string.Equals(columns[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private decimal? ParseDecimal(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number)
                return element.GetDecimal();
            if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out var parsed))
                return parsed;
            return null;
        }

        public async Task<MarketStatus?> GetMarketStatusAsync()
        {
            try
            {
                var url = "https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/SBER.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                if (!root.TryGetProperty("marketdata", out var md) ||
                    !md.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                    return new MarketStatus { Status = "Нет данных", CloseTime = "—" };

                var columns = md.GetProperty("columns");
                int statusIdx = FindColumnIndex(columns, "TRADINGSTATUS");

                var row = data[0];
                string? status = statusIdx >= 0 ? row[statusIdx].GetString() : null;

                string closeTime = "—";
                if (status == "T")
                {
                    // Определяем, основная или вечерняя сессия по московскому времени
                    var moscowTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Russian Standard Time");
                    if (moscowTime.Hour >= 10 && moscowTime.Hour < 18 || (moscowTime.Hour == 18 && moscowTime.Minute < 50))
                        closeTime = "18:50 МСК (осн. сессия)";
                    else if (moscowTime.Hour >= 19 && moscowTime.Hour < 23 || (moscowTime.Hour == 23 && moscowTime.Minute < 50))
                        closeTime = "23:50 МСК (вечерняя)";
                    else if (moscowTime.Hour >= 18 && moscowTime.Hour < 19)
                        closeTime = "18:50 МСК (осн.) / 23:50 МСК (веч.)";
                    else
                        closeTime = "23:50 МСК (вечерняя)";
                }

                return new MarketStatus
                {
                    Status = status switch
                    {
                        "T" => "Торги открыты",
                        "C" => "Торги закрыты",
                        _ => status ?? "—"
                    },
                    CloseTime = closeTime
                };
            }
            catch { return new MarketStatus { Status = "—", CloseTime = "—" }; }
        }

        public async Task<MoverDto?> GetStockMoverAsync(string ticker)
        {
            try
            {
                var url = $"https://iss.moex.com/iss/engines/stock/markets/shares/boards/TQBR/securities/{ticker.ToUpper()}.json";
                using var stream = await _httpClient.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                if (!root.TryGetProperty("marketdata", out var md) ||
                    !md.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                    return null;

                var columns = md.GetProperty("columns");
                int lastIdx = FindColumnIndex(columns, "LAST");
                int chgIdx = FindColumnIndex(columns, "LASTCHANGEPRCNT");

                var row = data[0];
                decimal? price = lastIdx >= 0 && row[lastIdx].ValueKind == JsonValueKind.Number
                    ? row[lastIdx].GetDecimal() : null;
                decimal? change = chgIdx >= 0 && row[chgIdx].ValueKind == JsonValueKind.Number
                    ? row[chgIdx].GetDecimal() : null;

                return new MoverDto
                {
                    Ticker = ticker,
                    Price = price,
                    ChangePct = change
                };
            }
            catch { return null; }
        }

    
        // Вспомогательные классы
        public class IndexInfo
        {
            public decimal? Value { get; set; }
            public decimal? ChangePct { get; set; }
        }

        public class MarketStatus
        {
            public string Status { get; set; } = string.Empty;
            public string CloseTime { get; set; } = string.Empty;
        }
    }
}