using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Consumers")]
public class Consumer
{
    [Key]
    [Column("consumer_id")]
    public int ConsumerId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; }

    [Column("middle_name")]
    public string? MiddleName { get; set; }

    [Column("last_name")]
    public string LastName { get; set; }
   [Key]
    [Column("username")]
    public string Username { get; set; }

    [Column("address")]
    public string Address { get; set; }

    [Column("phone_number")]
    public string PhoneNumber { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

[ForeignKey("UserId")]
public User User { get; set; }
}