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

    [Column("user_type")]
    public string UserType { get; set; } = "Consumer";

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}