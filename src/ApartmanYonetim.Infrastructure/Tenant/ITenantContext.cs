namespace ApartmanYonetim.Infrastructure.Tenant;

public interface ITenantContext
{
    string? FirmSlug { get; }
    bool HasFirm { get; }
    void SetFirmSlug(string slug);
}
