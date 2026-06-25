using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Core.Interfaces;

public interface IInvestorDocumentService
{
    Task<IEnumerable<InvestorDocumentDTO>> GetAllInvestorDocs();
    Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int investorId);
    Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto);
    Task<bool?> UpdateDocumentAsync(int docuetId, string status);
    Task<bool> DeleteDocumentAsync(int id);
}

public class InvestorDocumentDTO
{
    public int id { get; set; }
    public string title { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string size { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string uploaded_by { get; set; } = string.Empty;
    public string created_at { get; set; } = string.Empty;
}

public class UploadDocumentDTO
{
    public string title { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string size { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public string uploaded_by { get; set; } = string.Empty;
}



