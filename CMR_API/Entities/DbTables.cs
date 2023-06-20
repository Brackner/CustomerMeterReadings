using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMR_API.Entities
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        public int AccountId { get; set; }
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
    }

    [Table("MeterReadings")]
    public class MeterReading
    {
        public int Id { get; set; }
        [Required]  
        public int AccountId { get; set; }
        [Required]
        public DateTime MeterReadingDateTime { get; set; }
        [Required]
        public int MeterReadValue { get; set; } 
    }
}
