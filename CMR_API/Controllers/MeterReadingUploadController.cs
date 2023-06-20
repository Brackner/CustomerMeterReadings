using CMR_API.DataConnections;
using CMR_API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic.FileIO;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace YourProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MeterReadingController : ControllerBase
    {
        private readonly ENSEK_DbContext _dbContext;
        private readonly ILogger<MeterReadingController> _logger;

        public MeterReadingController(ENSEK_DbContext dbContext, ILogger<MeterReadingController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        //TESTING IGNORE ME//
        [HttpGet("get-single-account")]
        public async Task<IActionResult> GetAccount(int id)
        {
            try
            {
                var account = await _dbContext.Accounts.FindAsync(id);

                if (account == null)
                {
                    return NotFound();
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving the account.");
                return StatusCode(500, "An error occurred while processing the request." + ex.ToString()); //added ex.ToString() here to debug, REFORMAT **
            }
        }

        //TESTING IGNORE ME//
        [HttpPost("meter-reading-single")]
        public async Task<IActionResult> UploadMeterReadingSingle(int acId, DateTime dateTime, int value)
        {

            try
            {
                var account = await _dbContext.Accounts.FindAsync(acId);
                if (account == null)
                {
                    return BadRequest("Invalid account ID");
                }

                var existingEntry = _dbContext.meterReadings
                    .FirstOrDefault(mr => mr.AccountId == acId && mr.MeterReadingDateTime == dateTime);
                if (existingEntry != null)
                {
                    return BadRequest("Duplicate entry");
                }

                var meterReading = new MeterReading
                {
                    AccountId = acId,
                    MeterReadingDateTime = dateTime,
                    MeterReadValue = value
                };

                _dbContext.meterReadings.Add(meterReading);
                await _dbContext.SaveChangesAsync();

                return Ok("Meter reading created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating the meter reading.");
                return StatusCode(500, "An error occurred while processing the request. " + ex.ToString());
            }
        }







        [HttpPost("meter-reading-uploads")]
        public async Task<IActionResult> UploadMeterReadingCSV(IFormFile csvFile)
        {
            try
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
                        var meterReadingDateTimeValid = csv.TryGetField<DateTime>(1, out var meterReadingDateTime); //validation is weird here, its strangely not accepting some of the dates from the datetime column, need to add something
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

                var response = new
                {
                    SuccessCount = successCount,
                    FailureCount = failureCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing the meter readings.");
                return StatusCode(500, "An error occurred while processing the request. Full stack trace: " + ex.ToString());
            }
        }
    }
}
