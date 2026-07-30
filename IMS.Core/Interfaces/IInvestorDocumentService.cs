using System;
using System.Collections.Generic;
using System.Text;
using IMS.Core.Entities;

namespace IMS.Core.Interfaces;

public interface IInvestorDocumentService
{
    Task<IEnumerable<InvestorDocumentDTO>> GetAllInvestorDocs();
    Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int investorId, string? userId = null, string? email = null);
    Task<int> GetInvestorIdByUserIdOrEmailAsync(string? userId, string? email);
    Task<InvestorDocument?> GetInvestorDocByIdAsync(int id);
    Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto);
    Task<bool?> UpdateDocumentAsync(int docuetId, string status);
    Task<bool> UpdateDocumentStatusAsync(int id, string status);
    Task<bool> UpdateDocumentSignatureAsync(int id, string signature);
    Task<bool> ResetDocumentSignatureAsync(int id);
    Task<bool> DeleteDocumentAsync(int id);
}

public class InvestorDocumentDTO
{
    public int id { get; set; }
    public int investor_id { get; set; }
    public string? investor_name { get; set; }
    public string? investor_email { get; set; }
    public string title { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string size { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string uploaded_by { get; set; } = string.Empty;
    public string created_at { get; set; } = string.Empty;
    public string status { get; set; } = "PendingReview";
    public string? signature { get; set; }
    public string? signed_at { get; set; }
}

public class UploadDocumentDTO
{
    public string title { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string size { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string uploaded_by { get; set; } = string.Empty;
}



