namespace ApartmanYonetim.Domain.Enums;

public enum OccupancyType { Empty, Owner, Tenant }
public enum ResidencyType { Owner, Tenant, Family }
public enum UnitType { Daire, Dukkan, Depo, Otopark, Sigınak }
public enum DistributionType { EsitPay, MetreKare, ArsaPayi }
public enum SiteType { Apartman, Site, TopluYapi }
public enum FeePaymentStatus { Pending, Paid, Partial, Overdue }
public enum FeePeriod { Monthly, Quarterly, Yearly }
public enum MaintenanceStatus { Open, InProgress, Resolved, Closed }
public enum MaintenancePriority { Low, Normal, High, Urgent }
public enum MeetingType { Ordinary, Extraordinary, Emergency }
public enum MeetingStatus { Scheduled, Completed, Cancelled }
public enum AccountingEntryType { Income, Expense }
public enum SubscriptionStatus { Trial, Active, Overdue, Suspended, Cancelled }
public enum PaymentRecordStatus { Pending, Paid, Partial, Overdue }
public enum BillingPeriod { Monthly = 1, SixMonthly = 6, Yearly = 12 }
public enum ObligationPaymentStatus { Pending, Paid, Partial, Overdue }

// Firma personel rolleri
public enum StaffRole { SiteAdmin, SiteManager, Accountant, Auditor }

// Sözleşme durumu
public enum ContractStatus { Draft, Active, Expired, Terminated }

// Sözleşme kapsamı (hangi modüller dahil) — bit flags
[Flags]
public enum ContractScope
{
    None     = 0,
    Aidat    = 1,
    Muhasebe = 2,
    Ariza    = 4,
    Toplanti = 8,
    Duyuru   = 16,
    Tumu     = 31
}

// Yönetim ücreti tipi (ilerisi için)
public enum ManagementFeeType { Fixed, PerUnit, PerSqm }

// Denetim logu işlem tipi
public enum AuditActionType { Create, Update, Delete }

// Eğitim modülü
public enum EgitimTuru { Online, YuzYuze, Karma }
public enum DonemDurumu { Planlandi, Aktif, Tamamlandi, Iptal }
public enum OdemeDurumu { Bekliyor, KismiOdeme, Tamamlandi, Iptal }
