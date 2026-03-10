using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{

    [Key]
    [Column("user_id")]
    public int UserId { get; set; }


    [Column("email")]
    public string Email { get; set; }

    [Column("password_hash")]
    public string PasswordHash { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation property to Consumer
    public virtual Consumer? Consumer { get; set; }
    
}

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

    [Column("address")]
    public string? Address { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("username")]
    public string? Username { get; set; }

    [NotMapped]
    public string FullName
    {
        get
        {
            var names = new[] { FirstName, MiddleName, LastName }
                .Where(n => !string.IsNullOrWhiteSpace(n));
            return string.Join(" ", names);
        }
    }

    // Navigation property
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}