using IMS.Core.Entities;
using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace IMS.Persistance.Repositories;

public class LookupService : ILookupService
{
    private readonly ApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public LookupService(ApplicationDbContext context, 
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InvestorType>> AllInvestorTypes()
    {
        var investorTypes = _unitOfWork.InvestorTypes.GetAllAsync();
        return await investorTypes;

        //return (IEnumerable<InvestorType>)investorTypes;
    }

    public async Task<IEnumerable<RoiRange>> AllRoiRanges()
    {
        var roiRanges = _unitOfWork.RoiRanges.GetAllAsync();
        return await roiRanges;
    }

    public async Task<IEnumerable<RoiType>> AllRoiTypes()
    {
        var roiTypes = _unitOfWork.RoiTypes.GetAllAsync();
        return await roiTypes;
    }
}

