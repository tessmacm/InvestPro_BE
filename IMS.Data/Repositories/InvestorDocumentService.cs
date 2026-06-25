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
        var docs = await _context.InvestorDocuments
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var list = new List<InvestorDocumentDTO>();
        foreach (var d in docs)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == d.UploadedById);
            var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";
            
            list.Add(new InvestorDocumentDTO
            {
                id = d.Id,
                title = d.Title ?? string.Empty,
                type = d.DocumentType ?? "PDF",
                size = d.Size.HasValue ? $"{d.Size:F1} MB" : "0.2 MB",
                url = d.StorageUrl ?? "#",
                uploaded_by = userName,
                created_at = d.UploadedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return list;
    }

    public async Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int investorId)
    {
        var docs = await _context.InvestorDocuments
            .Where(d => d.InvestorId == investorId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var list = new List<InvestorDocumentDTO>();
        foreach (var d in docs)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == d.UploadedById);
            var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "System Admin";
            if (string.IsNullOrEmpty(userName)) userName = "System Admin";

            list.Add(new InvestorDocumentDTO
            {
                id = d.Id,
                title = d.Title ?? string.Empty,
                type = d.DocumentType ?? "PDF",
                size = d.Size.HasValue ? $"{d.Size:F1} MB" : "0.2 MB",
                url = d.StorageUrl ?? "#",
                uploaded_by = userName,
                created_at = d.UploadedAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }
        return list;
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

    public async Task<bool> DeleteDocumentAsync(int id)
    {
        var doc = await _context.InvestorDocuments.FindAsync(id);
        if (doc == null) return false;

        _context.InvestorDocuments.Remove(doc);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    
}
