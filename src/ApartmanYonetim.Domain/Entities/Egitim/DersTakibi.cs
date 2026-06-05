namespace ApartmanYonetim.Domain.Entities.Egitim;

public class DersTakibi
{
    public int Id { get; set; }
    public int KursiyerId { get; set; }
    public int DersProgramiId { get; set; }
    public bool Katildi { get; set; }
    public string? Not { get; set; }
    public Kursiyer Kursiyer { get; set; } = default!;
    public DersProgrami DersProgrami { get; set; } = default!;
}
