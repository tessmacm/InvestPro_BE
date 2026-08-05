using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMS.Persistance.Repositories;

public class InvestorDocumentService : IInvestorDocumentService
{
    private readonly ApplicationDbContext _context;

    public InvestorDocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InvestorDocumentDTO>> GetAllInvestorDocs()
    {
        var allDocs = await _context.InvestorDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        // Pre-load all Users and Investors into O(1) in-memory dictionaries (eliminates 150+ N+1 SQL queries)
        var usersDict = await _context.Users
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u);

        var investorsDict = await _context.Investors
            .AsNoTracking()
            .Where(i => i.InvestorId.HasValue)
            .ToDictionaryAsync(i => i.InvestorId!.Value, i => i);

        // Deduplicate agreement documents per investor: keep only the latest single agreement document per InvestorId
        var agreementDocIds = allDocs
            .Where(d => d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement")))
            .GroupBy(d => d.InvestorId)
            .Select(g => g.First().Id)
            .ToHashSet();

        var docs = allDocs.Where(d => 
            !(d.DocumentType == "Agreement" || (d.Title != null && d.Title.Contains("Agreement"))) ||
            agreementDocIds.Contains(d.Id)
        ).ToList();

        var list = new List<InvestorDocumentDTO>();
        foreach (var d in docs)
        {
            var user = d.UploadedById != null && usersDict.TryGetValue(d.UploadedById, out var uObj) ? uObj : null;
            var userName = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";

            string investorName = "Investor Profile";
            string investorEmail = "";

            if (investorsDict.TryGetValue(d.InvestorId, out var inv))
            {
                if (!string.IsNullOrEmpty(inv.OwnerUserId) && usersDict.TryGetValue(inv.OwnerUserId, out var invUser))
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
            var inv = await _context.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.OwnerUserId == userId);
            if (inv != null && inv.InvestorId.HasValue) return inv.InvestorId.Value;
        }

        if (!string.IsNullOrEmpty(email))
        {
            var userAcc = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (userAcc != null)
            {
                var inv = await _context.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.OwnerUserId == userAcc.Id);
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
            .AsNoTracking()
            .Where(d => d.InvestorId == investorId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var usersDict = await _context.Users
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u);

        var investorsDict = await _context.Investors
            .AsNoTracking()
            .Where(i => i.InvestorId.HasValue)
            .ToDictionaryAsync(i => i.InvestorId!.Value, i => i);

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
            var user = d.UploadedById != null && usersDict.TryGetValue(d.UploadedById, out var uObj) ? uObj : null;
            var userName = user != null ? (user.LastName == "User" || string.IsNullOrWhiteSpace(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}".Trim()) : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";

            string investorName = "Investor Profile";
            string investorEmail = "";

            if (investorsDict.TryGetValue(d.InvestorId, out var inv))
            {
                if (!string.IsNullOrEmpty(inv.OwnerUserId) && usersDict.TryGetValue(inv.OwnerUserId, out var invUser))
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
        return await _context.InvestorDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto)
    {
        var doc = new InvestorDocument
        {
            InvestorId = investorId,
            Title = dto.title,
            DocumentType = dto.type,
            StorageUrl = dto.url,
            UploadedAt = DateTime.UtcNow,
            Status = "PendingReview"
        };
        await _context.InvestorDocuments.AddAsync(doc);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool?> UpdateDocumentAsync(int docuetId, string status)
    {
        var document = await _context.InvestorDocuments.FindAsync(docuetId);
        if (document == null) return false;
        document.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDocumentStatusAsync(int id, string status)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;
        document.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDocumentSignatureAsync(int id, string signature)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;
        document.SignatureData = signature;
        document.Status = "Signed";
        document.SignedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetDocumentSignatureAsync(int id)
    {
        var document = await _context.InvestorDocuments.FindAsync(id);
        if (document == null) return false;
        document.SignatureData = null;
        document.Status = "Pending Signature";
        document.SignedAt = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var doc = await _context.InvestorDocuments.FindAsync(id);
        if (doc == null) return false;
        _context.InvestorDocuments.Remove(doc);
        await _context.SaveChangesAsync();
        return true;
    }
}
