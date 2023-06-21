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
        private readonly MeterReadingService _meterReadingService;


        //could change this due to business logic being in seperate class but leaving it in here as i haven't adjusted the post/get methods just below
        public MeterReadingController(ENSEK_DbContext dbContext, ILogger<MeterReadingController> logger, MeterReadingService meterReadingService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _meterReadingService = meterReadingService;
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

                var existingEntry = _dbContext.MeterReadings
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

                _dbContext.MeterReadings.Add(meterReading);
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
                var (successCount, failureCount) = await _meterReadingService.ProcessMeterReadings(csvFile);

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
