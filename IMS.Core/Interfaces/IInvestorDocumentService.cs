using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Interfaces;

public interface IInvestorDocumentService
{
    Task<IEnumerable<InvestorDocumentDTO>> GetAllInvestorDocs();
    Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int  investorId);
    Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto);
    Task<bool?> UpdateDocumentAsync(int docuetId, string status);
}

public class InvestorDocumentDTO
{
    public int Id { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string? DocumentType { get; set; } = string.Empty;
    public string? StorageUrl { get; set; } = string.Empty;
    public DateTime? UploadedAt { get; set; }
    public string? UploadedBy { get; set; } = string.Empty;
    public string? Status { get; set; } = string.Empty;
}

public class UploadDocumentDTO
{
    public string? Title { get; set; } = string.Empty;
    public string? Type { get; set; } = string.Empty;
    public decimal? Size { get; set; }
    public string? Url { get; set; } = string.Empty; // This comes back from your S3/Blob storage upload routine
    public string? Uploaded_By { get; set; } = string.Empty;
}



