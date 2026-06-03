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
        var units = await db.Units
            .Include(u => u.BlockRef)
            .Include(u => u.DaireType)
            .OrderBy(u => u.BlockRef!.Name).ThenBy(u => u.Block).ThenBy(u => u.Floor).ThenBy(u => u.Number)
            .ToListAsync();
        var residents = await db.Residents.Where(r => r.IsActive).ToListAsync();
        var residentByUnit = residents.ToDictionary(r => r.UnitId);
        return units.Select(u =>
        {
            residentByUnit.TryGetValue(u.Id, out var r);
            return new UnitSummaryDto(u.Id, u.Number, u.Block, u.Floor, u.SquareMeters, u.OccupancyType,
                r?.Id, r?.FullName, r?.Phone, r?.ResidencyType, r?.Email,
                u.UnitType, u.ArsaPay, u.BlockId, u.BlockRef?.Name,
                u.DaireTypeId, u.DaireType?.Name);
        }).ToList();
    }

    public async Task<UnitSummaryDto> AddUnitAsync(string dbFilePath, AddUnitCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var unit = new SiteUnit
        {
            Number = cmd.Number, Block = cmd.Block, Floor = cmd.Floor,
            SquareMeters = cmd.SquareMeters, UnitType = cmd.UnitType,
            ArsaPay = cmd.ArsaPay, BlockId = cmd.BlockId, DaireTypeId = cmd.DaireTypeId
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
        unit.ArsaPay = cmd.ArsaPay; unit.BlockId = cmd.BlockId; unit.DaireTypeId = cmd.DaireTypeId;
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

    public async Task EnsureDefaultDaireTypesAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        if (await db.DaireTypes.AnyAsync()) return;
        var defaults = new[]
        {
            new SiteDaireType { Name = "1+0", DisplayOrder = 1, IsDefault = true },
            new SiteDaireType { Name = "1+1", DisplayOrder = 2, IsDefault = true },
            new SiteDaireType { Name = "2+1", DisplayOrder = 3, IsDefault = true },
            new SiteDaireType { Name = "3+1", DisplayOrder = 4, IsDefault = true },
            new SiteDaireType { Name = "4+1", DisplayOrder = 5, IsDefault = true },
        };
        db.DaireTypes.AddRange(defaults);
        await db.SaveChangesAsync();
    }

    public async Task<List<DaireTypeDto>> GetDaireTypesAsync(string dbFilePath)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.DaireTypes
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name)
            .Select(d => new DaireTypeDto(d.Id, d.Name, d.IsActive, d.DisplayOrder, d.IsDefault))
            .ToListAsync();
    }

    public async Task<DaireTypeDto> AddDaireTypeAsync(string dbFilePath, DaireTypeCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var d = new SiteDaireType { Name = cmd.Name.Trim(), DisplayOrder = cmd.DisplayOrder };
        db.DaireTypes.Add(d);
        await db.SaveChangesAsync();
        return new DaireTypeDto(d.Id, d.Name, d.IsActive, d.DisplayOrder, d.IsDefault);
    }

    public async Task UpdateDaireTypeAsync(string dbFilePath, Guid id, DaireTypeCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var d = await db.DaireTypes.FindAsync(id) ?? throw new KeyNotFoundException();
        d.Name = cmd.Name.Trim(); d.DisplayOrder = cmd.DisplayOrder;
        await db.SaveChangesAsync();
    }

    public async Task DeleteDaireTypeAsync(string dbFilePath, Guid id)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var d = await db.DaireTypes.FindAsync(id) ?? throw new KeyNotFoundException();
        if (d.IsDefault) throw new InvalidOperationException("Varsayılan tipler silinemez.");
        d.IsActive = false;
        await db.SaveChangesAsync();
    }

    public async Task<int> GenerateUnitsAsync(string dbFilePath, List<BlockGenConfig> configs)
    {
        await EnsureDefaultDaireTypesAsync(dbFilePath);
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        int total = 0;
        foreach (var cfg in configs)
        {
            int seq = 1;
            for (int floor = 1; floor <= cfg.FloorCount; floor++)
            {
                for (int pos = 1; pos <= cfg.UnitsPerFloor; pos++)
                {
                    var number = cfg.Code is not null ? $"{cfg.Code}-{seq}" : $"{seq}";
                    db.Units.Add(new SiteUnit
                    {
                        Number = number, Block = cfg.Code, Floor = floor,
                        SquareMeters = 0, UnitType = UnitType.Daire, ArsaPay = 0,
                        BlockId = cfg.BlockId
                    });
                    seq++; total++;
                }
            }
        }
        await db.SaveChangesAsync();
        return total;
    }

    public async Task<List<BlockAssignmentDto>> GetBlockAssignmentsAsync(string dbFilePath, Guid blockId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        return await db.BlockAssignments
            .Include(a => a.Block)
            .Where(a => a.BlockId == blockId)
            .OrderByDescending(a => a.StartDate)
            .Select(a => new BlockAssignmentDto(a.Id, a.BlockId, a.Block.Name,
                a.ManagerUserId, a.ManagerDisplayName, a.StartDate, a.EndDate,
                a.EndDate == null || a.EndDate >= DateOnly.FromDateTime(DateTime.Today)))
            .ToListAsync();
    }

    public async Task AddBlockAssignmentAsync(string dbFilePath, Guid blockId, BlockAssignmentCommand cmd)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        // Mevcut aktif atamayı kapat
        var active = await db.BlockAssignments
            .Where(a => a.BlockId == blockId && a.EndDate == null)
            .ToListAsync();
        foreach (var a in active) a.EndDate = cmd.StartDate.AddDays(-1);
        db.BlockAssignments.Add(new SiteBlockAssignment
        {
            BlockId = blockId, ManagerUserId = cmd.ManagerUserId,
            ManagerDisplayName = cmd.ManagerDisplayName, StartDate = cmd.StartDate
        });
        await db.SaveChangesAsync();
    }

    public async Task EndBlockAssignmentAsync(string dbFilePath, int assignmentId)
    {
        await using var db = await factory.CreateAndMigrateAsync(dbFilePath);
        var a = await db.BlockAssignments.FindAsync(assignmentId) ?? throw new KeyNotFoundException();
        a.EndDate = DateOnly.FromDateTime(DateTime.Today);
        await db.SaveChangesAsync();
    }
}
