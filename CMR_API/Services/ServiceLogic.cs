using Microsoft.VisualBasic.FileIO;
using CsvHelper;
using CsvHelper.Configuration;
using CMR_API.DataConnections;
using CMR_API.Entities;
using System.Globalization;
using YourProject.Controllers;

public class MeterReadingService
{
    private readonly ENSEK_DbContext _dbContext;
    private readonly ILogger<MeterReadingController> _logger;

    public MeterReadingService(ENSEK_DbContext dbContext, ILogger<MeterReadingController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(int successCount, int failureCount)> ProcessMeterReadings(IFormFile csvFile)
    {
        var accountReadings = new List<MeterReading>();
        var successCount = 0;
        var failureCount = 0;

        using (var reader = new StreamReader(csvFile.OpenReadStream()))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        }))
        {
            csv.Read();

            while (csv.Read())
            {
                // Validation: data types for reader 
                var accountIdValid = csv.TryGetField<int>(0, out var accountId);
                var meterReadingDateTimeValid = csv.TryGetField<DateTime>(1, out var meterReadingDateTime);
                var meterReadValueValid = csv.TryGetField<int>(2, out var meterReadValue);

                if (accountIdValid && meterReadingDateTimeValid && meterReadValueValid)
                {
                    var accountReading = new MeterReading
                    {
                        AccountId = accountId,
                        MeterReadingDateTime = meterReadingDateTime,
                        MeterReadValue = meterReadValue
                    };

                    accountReadings.Add(accountReading);
                }
                else
                {
                    failureCount++;
                }
            }
        }

        foreach (var accountReading in accountReadings)
        {
            // Validation: check account ID exists
            var account = await _dbContext.Accounts.FindAsync(accountReading.AccountId);
            if (account == null)
            {
                failureCount++;
                continue;
            }

            // Validation: check duplicate entry
            var existingEntry = _dbContext.meterReadings
                .FirstOrDefault(mr => mr.AccountId == accountReading.AccountId && mr.MeterReadingDateTime == accountReading.MeterReadingDateTime);
            if (existingEntry != null)
            {
                failureCount++;
                continue;
            }

            var meterReading = new MeterReading
            {
                AccountId = accountReading.AccountId,
                MeterReadingDateTime = accountReading.MeterReadingDateTime,
                MeterReadValue = accountReading.MeterReadValue
            };

            _dbContext.meterReadings.Add(meterReading);
            successCount++;
        }

        await _dbContext.SaveChangesAsync();

        return (successCount, failureCount);
    }
}
