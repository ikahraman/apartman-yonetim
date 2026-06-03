using ApartmanYonetim.Application.Services;
using ApartmanYonetim.Domain.Entities;
using ApartmanYonetim.Domain.Enums;
using ApartmanYonetim.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace ApartmanYonetim.Infrastructure.Services;

public class FirmContractService(FirmDbContext db) : IFirmContractService
{
    private static ContractDto ToDto(SiteContract c) =>
        new(c.Id, c.SiteId, c.Site?.Name ?? "",
            c.ContractNumber, c.StartDate, c.EndDate,
            c.Status, c.Scope, c.FeeType, c.MonthlyFee,
            c.Notes, c.TerminationReason,
            c.CreatedAt, c.SignedAt);

    public async Task<List<ContractDto>> GetBySiteAsync(Guid siteId)
        => await db.SiteContracts
            .Include(c => c.Site)
            .Where(c => c.SiteId == siteId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => ToDto(c))
            .ToListAsync();

    public async Task<List<ContractDto>> GetAllForFirmAsync()
        => await db.SiteContracts
            .Include(c => c.Site)
            .OrderBy(c => c.Site!.Name)
            .ThenByDescending(c => c.StartDate)
            .Select(c => ToDto(c))
            .ToListAsync();

    public async Task<ContractDto> CreateAsync(ContractCommand cmd, string createdByUserId)
    {
        // Eğer yeni sözleşme Active ise, mevcut aktif sözleşmeleri Expired yap
        if (cmd.Status == ContractStatus.Active)
        {
            var existing = await db.SiteContracts
                .Where(c => c.SiteId == cmd.SiteId && c.Status == ContractStatus.Active)
                .ToListAsync();
            foreach (var e in existing) e.Status = ContractStatus.Expired;
        }

        var contract = new SiteContract
        {
            SiteId = cmd.SiteId,
            ContractNumber = cmd.ContractNumber,
            StartDate = cmd.StartDate,
            EndDate = cmd.EndDate,
            Status = cmd.Status,
            Scope = cmd.Scope,
            FeeType = cmd.FeeType,
            MonthlyFee = cmd.MonthlyFee,
            Notes = cmd.Notes,
            CreatedByUserId = createdByUserId,
            SignedAt = cmd.SignedAt
        };
        db.SiteContracts.Add(contract);
        await db.SaveChangesAsync();

        await db.Entry(contract).Reference(c => c.Site).LoadAsync();
        return ToDto(contract);
    }

    public async Task UpdateAsync(Guid contractId, ContractCommand cmd)
    {
        var contract = await db.SiteContracts.FindAsync(contractId)
            ?? throw new InvalidOperationException("Sözleşme bulunamadı.");

        // Eğer bu sözleşme Active yapılıyorsa, diğerlerini Expired yap
        if (cmd.Status == ContractStatus.Active && contract.Status != ContractStatus.Active)
        {
            var existing = await db.SiteContracts
                .Where(c => c.SiteId == contract.SiteId && c.Id != contractId && c.Status == ContractStatus.Active)
                .ToListAsync();
            foreach (var e in existing) e.Status = ContractStatus.Expired;
        }

        contract.ContractNumber = cmd.ContractNumber;
        contract.StartDate = cmd.StartDate;
        contract.EndDate = cmd.EndDate;
        contract.Status = cmd.Status;
        contract.Scope = cmd.Scope;
        contract.FeeType = cmd.FeeType;
        contract.MonthlyFee = cmd.MonthlyFee;
        contract.Notes = cmd.Notes;
        contract.SignedAt = cmd.SignedAt;
        await db.SaveChangesAsync();
    }

    public async Task TerminateAsync(Guid contractId, TerminateContractCommand cmd)
    {
        var contract = await db.SiteContracts.FindAsync(contractId)
            ?? throw new InvalidOperationException("Sözleşme bulunamadı.");
        contract.Status = ContractStatus.Terminated;
        contract.TerminationReason = cmd.Reason;
        await db.SaveChangesAsync();
    }
}
