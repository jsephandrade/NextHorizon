using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAspNetApp.Models
{
    [Table("Consumers")]
    public class DbConsumer
    {
        [Key][Column("consumer_id")] public int ConsumerId { get; set; }
        [Column("user_id")]          public int UserId { get; set; }
        [Column("first_name")]       public string FirstName { get; set; } = string.Empty;
        [Column("middle_name")]      public string? MiddleName { get; set; }
        [Column("last_name")]        public string LastName { get; set; } = string.Empty;
        [Column("address")]          public string Address { get; set; } = string.Empty;
        [Column("phone_number")]     public string? PhoneNumber { get; set; }
        [Column("username")]         public string? Username { get; set; }
    }

    [Table("Users")]
    public class DbUser
    {
        [Key][Column("user_id")] public int UserId { get; set; }
        [Column("email")]        public string Email { get; set; } = string.Empty;
    }
}
