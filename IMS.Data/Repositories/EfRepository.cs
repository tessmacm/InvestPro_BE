using IMS.Core.Interfaces;
using IMS.Persistance.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace IMS.Persistance.Repositories;

public class EfRepository<T> : IAsyncRepository<T> where T : class
{
    protected readonly ApplicationDbContext? _dbContext;

    public EfRepository(ApplicationDbContext? dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbContext.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync() => await _dbContext.Set<T>().ToListAsync();

    public async Task<T> AddAsync(T entity)
    {
        await _dbContext.Set<T>().AddAsync(entity);
        return entity;
    }

    public void Update(T entity)
    {
        _dbContext.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
    }

}
