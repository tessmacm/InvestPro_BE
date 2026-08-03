using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using IMS.Core.Entities;
using System.Security.Claims;


namespace IMS.Persistance.Repositories;

public class InvestorDocumentService : IInvestorDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public InvestorDocumentService(ApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InvestorDocumentDTO>> GetAllInvestorDocs()
    {
        var allDocs = await _context.InvestorDocuments
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        // Deduplicate agreement documents per investor: keep only the latest single agreement document per InvestorId
        var agreementDocIds = allDocs
            .Where(d => d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement")))
            .GroupBy(d => d.InvestorId)
            .Select(g => g.First().Id) // int primitive ID
            .ToHashSet();

        var docs = allDocs.Where(d => 
            !(d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement"))) ||
            agreementDocIds.Contains(d.Id)
        ).ToList();

        var list = new List<InvestorDocumentDTO>();
        foreach (var d in docs)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == d.UploadedById);
            var userName = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";

            string investorName = "Investor Profile";
            string investorEmail = "";
            var inv = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorId == d.InvestorId);
            if (inv != null)
            {
                var invUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == inv.OwnerUserId);
                if (invUser != null)
                {
                    investorName = invUser.LastName == "User" || string.IsNullOrWhiteSpace(invUser.LastName) ? invUser.FirstName : $"{invUser.FirstName} {invUser.LastName}".Trim();
                    investorEmail = invUser.Email ?? "";
                }
                else if (!string.IsNullOrEmpty(inv.LegalBusinessName))
                {
                    investorName = inv.LegalBusinessName;
                }
            }

            list.Add(new InvestorDocumentDTO
            {
                id = d.Id,
                investor_id = d.InvestorId,
                investor_name = string.IsNullOrWhiteSpace(investorName) ? "Investor Profile" : investorName,
                investor_email = investorEmail,
                title = d.Title ?? string.Empty,
                type = d.DocumentType ?? "PDF",
                size = d.Size.HasValue ? $"{d.Size:F1} MB" : "0.2 MB",
                url = d.StorageUrl ?? "#",
                uploaded_by = userName,
                created_at = d.UploadedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                status = d.Status ?? "PendingReview",
                signature = d.SignatureData,
                signed_at = d.SignedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return list;
    }

    public async Task<int> GetInvestorIdByUserIdOrEmailAsync(string? userId, string? email)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var inv = await _context.Investors.FirstOrDefaultAsync(i => i.OwnerUserId == userId);
            if (inv != null && inv.InvestorId.HasValue) return inv.InvestorId.Value;
        }

        if (!string.IsNullOrEmpty(email))
        {
            var userAcc = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (userAcc != null)
            {
                var inv = await _context.Investors.FirstOrDefaultAsync(i => i.OwnerUserId == userAcc.Id);
                if (inv != null && inv.InvestorId.HasValue) return inv.InvestorId.Value;
            }
        }

        return 0;
    }

    public async Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int investorId, string? userId = null, string? email = null)
    {
        if (investorId == 0)
        {
            investorId = await GetInvestorIdByUserIdOrEmailAsync(userId, email);
        }

        var allDocs = await _context.InvestorDocuments
            .Where(d => d.InvestorId == investorId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var latestAgreementId = allDocs
            .Where(d => d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement")))
            .Select(d => d.Id)
            .FirstOrDefault();

        var docs = allDocs.Where(d => 
            !(d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement"))) ||
            (latestAgreementId != 0 && d.Id == latestAgreementId)
        ).ToList();

        var list = new List<InvestorDocumentDTO>();
        foreach (var d in docs)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == d.UploadedById);
            var userName = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";

            string investorName = "Investor Profile";
            string investorEmail = "";
            var inv = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorId == d.InvestorId);
            if (inv != null)
            {
                var invUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == inv.OwnerUserId);
                if (invUser != null)
                {
                    investorName = invUser.LastName == "User" || string.IsNullOrWhiteSpace(invUser.LastName) ? invUser.FirstName : $"{invUser.FirstName} {invUser.LastName}".Trim();
                    investorEmail = invUser.Email ?? "";
                }
                else if (!string.IsNullOrEmpty(inv.LegalBusinessName))
                {
                    investorName = inv.LegalBusinessName;
                }
            }

            list.Add(new InvestorDocumentDTO
            {
                id = d.Id,
                investor_id = d.InvestorId,
                investor_name = string.IsNullOrWhiteSpace(investorName) ? "Investor Profile" : investorName,
                investor_email = investorEmail,
                title = d.Title ?? string.Empty,
                type = d.DocumentType ?? "PDF",
                size = d.Size.HasValue ? $"{d.Size:F1} MB" : "0.2 MB",
                url = d.StorageUrl ?? "#",
                uploaded_by = userName,
                created_at = d.UploadedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                status = d.Status ?? "PendingReview",
                signature = d.SignatureData,
                signed_at = d.SignedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return list;
    }

    public async Task<InvestorDocument?> GetInvestorDocByIdAsync(int id)
    {
        return await _context.InvestorDocuments.FindAsync(id);
    }

    public async Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto)
    {
        if (investorId == 0)
        {
            var firstInvestor = await _context.Investors.FirstOrDefaultAsync();
            if (firstInvestor != null)
            {
                investorId = firstInvestor.InvestorId ?? 0;
            }
        }

        decimal sizeDec = 0.2m;
        if (!string.IsNullOrEmpty(dto.size))
        {
            var clean = new string(dto.size.Where(c => char.IsDigit(c) || c == '.').ToArray());
            decimal.TryParse(clean, out sizeDec);
        }

        var document = new InvestorDocument
        {
            InvestorId = investorId,
            Title = dto.title,
            DocumentType = dto.type,
            Size = sizeDec,
            StorageUrl = dto.url,
            UploadedById = dto.uploaded_by,
            Status = "PendingReview",
            UploadedAt = DateTime.UtcNow
        };

        await _context.InvestorDocuments.AddAsync(document);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool?> UpdateDocumentAsync(int docuetId, string status)
    {
        var document = await _context.InvestorDocuments.FindAsync(docuetId);
        if (document == null) return false;

        document.Status = status;
        return await _unitOfWork.CompleteAsync() >= 0;
    }

    public async Task<bool> UpdateDocumentStatusAsync(int id, string status)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;

        document.Status = status;
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> UpdateDocumentSignatureAsync(int id, string signature)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;

        document.Status = "Signed";
        document.SignatureData = signature;
        document.SignedAt = DateTime.UtcNow;
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> ResetDocumentSignatureAsync(int id)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;

        document.Status = "Pending Signature";
        document.SignatureData = null;
        document.SignedAt = null;
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var doc = await _context.InvestorDocuments.FindAsync(id);
        if (doc == null) return false;

        _context.InvestorDocuments.Remove(doc);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    
}
