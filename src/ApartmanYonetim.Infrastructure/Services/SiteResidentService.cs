using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities.Site;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class SiteResidentService(SiteDbContextFactory factory) : ISiteResidentService
{
    public async Task<List<UnitSummaryDto>> GetUnitsAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var units = await db.Units.Include(u => u.BlockRef).OrderBy(u => u.BlockRef!.Name).ThenBy(u => u.Block).ThenBy(u => u.Floor).ThenBy(u => u.Number).ToListAsync();
        var residents = await db.Residents.Where(r => r.IsActive).ToListAsync();
        var residentByUnit = residents.ToDictionary(r => r.UnitId);
        return units.Select(u =>
        {
            residentByUnit.TryGetValue(u.Id, out var r);
            return new UnitSummaryDto(u.Id, u.Number, u.Block, u.Floor, u.SquareMeters, u.OccupancyType,
                r?.Id, r?.FullName, r?.Phone, r?.ResidencyType, r?.Email,
                u.UnitType, u.ArsaPay, u.BlockId, u.BlockRef?.Name);
        }).ToList();
    }

    public async Task<UnitSummaryDto> AddUnitAsync(string dbFilePath, AddUnitCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var unit = new SiteUnit
        {
            Number = cmd.Number, Block = cmd.Block, Floor = cmd.Floor,
            SquareMeters = cmd.SquareMeters, UnitType = cmd.UnitType,
            ArsaPay = cmd.ArsaPay, BlockId = cmd.BlockId
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync();
        return new UnitSummaryDto(unit.Id, unit.Number, unit.Block, unit.Floor, unit.SquareMeters, unit.OccupancyType,
            null, null, null, null, null, unit.UnitType, unit.ArsaPay, unit.BlockId);
    }

    public async Task UpdateUnitAsync(string dbFilePath, Guid unitId, AddUnitCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var unit = await db.Units.FindAsync(unitId) ?? throw new InvalidOperationException("Daire bulunamadı.");
        unit.Number = cmd.Number; unit.Block = cmd.Block; unit.Floor = cmd.Floor;
        unit.SquareMeters = cmd.SquareMeters; unit.UnitType = cmd.UnitType;
        unit.ArsaPay = cmd.ArsaPay; unit.BlockId = cmd.BlockId;
        await db.SaveChangesAsync();
    }

    public async Task<List<ResidentDto>> GetResidentsForUnitAsync(string dbFilePath, Guid unitId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.Residents.Where(r => r.UnitId == unitId)
            .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.MoveInDate)
            .Select(r => new ResidentDto(r.Id, r.UnitId, r.FirstName, r.LastName, r.FullName,
                r.Phone, r.Email, r.ResidencyType, r.MoveInDate, r.MoveOutDate, r.IsActive, r.Notes))
            .ToListAsync();
    }

    public async Task<ResidentDto> AddResidentAsync(string dbFilePath, Guid unitId, ResidentFormCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var unit = await db.Units.FindAsync(unitId) ?? throw new InvalidOperationException("Daire bulunamadı.");
        var existing = await db.Residents.Where(r => r.UnitId == unitId && r.IsActive).ToListAsync();
        foreach (var e in existing) { e.IsActive = false; e.MoveOutDate = cmd.MoveInDate; }
        var resident = new SiteResident
        {
            UnitId = unitId, FirstName = cmd.FirstName, LastName = cmd.LastName,
            Phone = cmd.Phone, Email = cmd.Email, ResidencyType = cmd.ResidencyType,
            MoveInDate = cmd.MoveInDate, Notes = cmd.Notes
        };
        db.Residents.Add(resident);
        unit.OccupancyType = cmd.ResidencyType == ResidencyType.Owner ? OccupancyType.Owner : OccupancyType.Tenant;
        await db.SaveChangesAsync();
        return new ResidentDto(resident.Id, resident.UnitId, resident.FirstName, resident.LastName, resident.FullName,
            resident.Phone, resident.Email, resident.ResidencyType, resident.MoveInDate, null, true, resident.Notes);
    }

    public async Task UpdateResidentAsync(string dbFilePath, Guid residentId, ResidentFormCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.Residents.FindAsync(residentId) ?? throw new InvalidOperationException("Sakin bulunamadı.");
        r.FirstName = cmd.FirstName; r.LastName = cmd.LastName;
        r.Phone = cmd.Phone; r.Email = cmd.Email;
        r.ResidencyType = cmd.ResidencyType; r.MoveInDate = cmd.MoveInDate; r.Notes = cmd.Notes;
        await db.SaveChangesAsync();
    }

    public async Task MoveOutAsync(string dbFilePath, Guid residentId, DateOnly moveOutDate)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.Residents.FindAsync(residentId) ?? throw new InvalidOperationException("Sakin bulunamadı.");
        r.IsActive = false; r.MoveOutDate = moveOutDate;
        var unit = await db.Units.FindAsync(r.UnitId);
        if (unit is not null) unit.OccupancyType = OccupancyType.Empty;
        await db.SaveChangesAsync();
    }

    public async Task SetResidentUserIdAsync(string dbFilePath, Guid residentId, string userId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.Residents.FindAsync(residentId) ?? throw new InvalidOperationException("Sakin bulunamadı.");
        r.UserId = userId;
        await db.SaveChangesAsync();
    }

    public async Task<ResidentDto?> GetByUserIdAsync(string dbFilePath, string userId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var r = await db.Residents.FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (r is null) return null;
        return new ResidentDto(r.Id, r.UnitId, r.FirstName, r.LastName, r.FullName,
            r.Phone, r.Email, r.ResidencyType, r.MoveInDate, r.MoveOutDate, r.IsActive, r.Notes);
    }

    public async Task<List<SiteKisimDto>> GetKisimlarAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.Kisimlar.OrderBy(k => k.Name)
            .Select(k => new SiteKisimDto(k.Id, k.Name, k.Code, k.Description))
            .ToListAsync();
    }

    public async Task<SiteKisimDto> AddKisimAsync(string dbFilePath, SiteKisimCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var kisim = new SiteKisim { Name = cmd.Name, Code = cmd.Code, Description = cmd.Description };
        db.Kisimlar.Add(kisim);
        await db.SaveChangesAsync();
        return new SiteKisimDto(kisim.Id, kisim.Name, kisim.Code, kisim.Description);
    }

    public async Task UpdateKisimAsync(string dbFilePath, Guid kisimId, SiteKisimCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var kisim = await db.Kisimlar.FindAsync(kisimId) ?? throw new InvalidOperationException("Kısım bulunamadı.");
        kisim.Name = cmd.Name; kisim.Code = cmd.Code; kisim.Description = cmd.Description;
        await db.SaveChangesAsync();
    }

    public async Task DeleteKisimAsync(string dbFilePath, Guid kisimId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var kisim = await db.Kisimlar.FindAsync(kisimId) ?? throw new InvalidOperationException("Kısım bulunamadı.");
        var blocks = await db.Blocks.Where(b => b.KisimId == kisimId).ToListAsync();
        foreach (var b in blocks) b.KisimId = null;
        db.Kisimlar.Remove(kisim);
        await db.SaveChangesAsync();
    }

    public async Task<List<SiteBlockDto>> GetBlocksAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.Blocks.Include(b => b.Kisim).OrderBy(b => b.Kisim!.Name).ThenBy(b => b.Name)
            .Select(b => new SiteBlockDto(b.Id, b.Name, b.Code, b.FloorCount, b.UnitCount, b.KisimId, b.Kisim != null ? b.Kisim.Name : null))
            .ToListAsync();
    }

    public async Task<SiteBlockDto> AddBlockAsync(string dbFilePath, SiteBlockCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var block = new SiteBlock { Name = cmd.Name, Code = cmd.Code, FloorCount = cmd.FloorCount, UnitCount = cmd.UnitCount, KisimId = cmd.KisimId };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();
        return new SiteBlockDto(block.Id, block.Name, block.Code, block.FloorCount, block.UnitCount, block.KisimId);
    }

    public async Task UpdateBlockAsync(string dbFilePath, Guid blockId, SiteBlockCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var block = await db.Blocks.FindAsync(blockId) ?? throw new InvalidOperationException("Blok bulunamadı.");
        block.Name = cmd.Name; block.Code = cmd.Code; block.FloorCount = cmd.FloorCount;
        block.UnitCount = cmd.UnitCount; block.KisimId = cmd.KisimId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteBlockAsync(string dbFilePath, Guid blockId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var block = await db.Blocks.FindAsync(blockId) ?? throw new InvalidOperationException("Blok bulunamadı.");
        var units = await db.Units.Where(u => u.BlockId == blockId).ToListAsync();
        foreach (var u in units) u.BlockId = null;
        db.Blocks.Remove(block);
        await db.SaveChangesAsync();
    }
}
