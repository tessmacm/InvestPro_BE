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
        return await _context.InvestorDocuments
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new InvestorDocumentDTO
                {
                    Id = d.Id,
                    Title = d.Title,
                    DocumentType = d.DocumentType,
                    StorageUrl = d.StorageUrl,
                    UploadedAt = d.UploadedAt,
                    Status = d.Status

                }).ToListAsync();
    }

    public async Task<IEnumerable<InvestorDocumentDTO>> GetInvestorDocsByInvestorIdAsync(int investorId)
    {
        return await _context.InvestorDocuments
            .Where(d => d.InvestorId == investorId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new InvestorDocumentDTO
            {
                Id = d.Id,
                Title = d.Title,
                DocumentType = d.DocumentType,
                StorageUrl = d.StorageUrl,
                UploadedAt = d.UploadedAt,
                Status = d.Status
            })
            .ToListAsync();

    }

    public async Task<bool> UploadDocumentMetadataAsync(int investorId, UploadDocumentDTO dto)
    {
        var document = new InvestorDocument
        {
            InvestorId = investorId,
            Title = dto.Title,
            DocumentType = dto.Type,
            Size = dto.Size,
            StorageUrl = dto.Url,
            UploadedById = dto.Uploaded_By, //Admin Id (string)
            Status = "PendingReview"
        };

        await _context.InvestorDocuments.AddAsync(document);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<bool?> UpdateDocumentAsync(int docuetId, string status)
    {
        var document = await _context.InvestorDocuments.FindAsync(docuetId);
        if (document == null) return false;

        document.Status = status; //Approved/Rejected
        return await _unitOfWork.CompleteAsync() >= 0;
    }

    
}
