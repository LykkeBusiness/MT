// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MarginTrading.Backend.RestoreTool;

internal static class CsvReaders
{
    public static List<PositionCsvModel> ReadPositionsFromCsv()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using (var reader = new StreamReader("PositionsHistory.csv"))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<PositionCsvClassMap>();
            var records = csv.GetRecords<PositionCsvModel>().ToList();
            return records;
        }
    }

    public static List<AccountHistoryCsvModel> ReadAccountHistoryFromCsv()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ";"
        };

        using (var reader = new StreamReader("AccountHistory.csv"))
        using (var csv = new CsvReader(reader, config))
        {
            csv.Context.RegisterClassMap<AccountHistoryCsvClassMap>();
            var records = csv.GetRecords<AccountHistoryCsvModel>().ToList();
            return records;
        }
    }
}