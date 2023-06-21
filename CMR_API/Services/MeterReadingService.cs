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
using Microsoft.EntityFrameworkCore;

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

        if (!await IsDuplicateEntry(meterReading))
        {
            return false;
        }

        if (!await IsValidMeterReadValueAsync(meterReading.MeterReadValue))
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

    public async Task<bool> IsDuplicateEntry(MeterReading meterReading)
    {
        //didnt include value here as didnt consider it duplicate (values can be different assuming)
        return await _dbContext.MeterReadings.AnyAsync(mr =>
            mr.AccountId == meterReading.AccountId &&
            mr.MeterReadingDateTime == meterReading.MeterReadingDateTime);
    }

    public async Task<bool> IsValidMeterReadValueAsync(int meterReadValue)
    {
        //additional validation for the meter reading values 
        return await Task.FromResult(meterReadValue >= 0 && meterReadValue <= 99999);
    }
}
