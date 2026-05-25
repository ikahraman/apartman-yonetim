namespace ApartmanYonetim.Domain.Enums;

public enum OccupancyType { Empty, Owner, Tenant }
public enum ResidencyType { Owner, Tenant, Family }
public enum FeePaymentStatus { Pending, Paid, Partial, Overdue }
public enum FeePeriod { Monthly, Quarterly, Yearly }
public enum MaintenanceStatus { Open, InProgress, Resolved, Closed }
public enum MaintenancePriority { Low, Normal, High, Urgent }
public enum MeetingType { Ordinary, Extraordinary, Emergency }
public enum MeetingStatus { Scheduled, Completed, Cancelled }
public enum AccountingEntryType { Income, Expense }
