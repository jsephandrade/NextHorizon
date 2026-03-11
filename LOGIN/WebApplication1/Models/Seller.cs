using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Sellers")]
public class Seller
{
    [Key]
    [Column("seller_id")]
    public int SellerId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; } // FK to Users

    [Column("business_type")]
    public string BusinessType { get; set; }

    [Column("business_name")]
    public string BusinessName { get; set; }

    [Column("business_email")]
    public string BusinessEmail { get; set; }

    [Column("business_phone")]
    public string BusinessPhone { get; set; }

    [Column("tax_id")]
    public string TaxId { get; set; }

    [Column("business_address")]
    public string BusinessAddress { get; set; }

    [Column("logo_path")]
    public string LogoPath { get; set; }

    [Column("document_path")]
    public string DocumentPath { get; set; } // you can store multiple docs as JSON or semi-colon separated if needed

    [Column("seller_status")]
    public string SellerStatus { get; set; } = "Pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}