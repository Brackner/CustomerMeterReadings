using Microsoft.VisualBasic.FileIO;
using CsvHelper;
using CsvHelper.Configuration;
using CMR_API.DataConnections;
using CMR_API.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
                // This had some issues reading the CSV dates (formatting, probably needs refactoring to account.. could play with data types maybe?)
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
            var validationResult = await ValidateMeterReading(accountReading);
            if (validationResult)
            {
                _dbContext.MeterReadings.Add(accountReading);
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        await _dbContext.SaveChangesAsync();

        return (successCount, failureCount);
    }

    public async Task<bool> ValidateMeterReading(MeterReading meterReading)
    {
        if (!await AccountExists(meterReading.AccountId))
        {
            return false;
        }

        if (IsDuplicateEntry(meterReading))
        {
            return false;
        }

        return true;
    }

    public async Task<bool> AccountExists(int accountId)
    {
        var account = await _dbContext.Accounts.FindAsync(accountId);
        return account != null;
    }

    public bool IsDuplicateEntry(MeterReading meterReading)
    {
        return _dbContext.MeterReadings.Any(mr =>
            mr.AccountId == meterReading.AccountId &&
            mr.MeterReadingDateTime == meterReading.MeterReadingDateTime);
    }
}
