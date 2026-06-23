using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Entities;

public class InvestorDocument
{
    public int Id { get; set; }

    // The anchor foreign key tying this document to a specific investor
    public int InvestorId { get; set; }
    public Investor? InvestorNav { get; set; }
    public string? Title { get; set; }  // e.g., "Passport_Verification.pdf"
    public string? DocumentType { get; set; }  // e.g., "KYC", "TaxForm", "Agreement"
    public decimal? Size { get; set; }
        
    // The secure cloud storage path/URL (AWS S3, Azure Blob, etc.)
    public string? StorageUrl { get; set; } 
    public DateTime? UploadedAt { get; set; } 
    public string? UploadedById { get; set; }
    public string? Status { get; set; } = "PendingReview"; // PendingReview, Approved, Rejected
}
