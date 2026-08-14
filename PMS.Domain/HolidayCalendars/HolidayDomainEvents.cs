using PMS.SharedKernel;

namespace PMS.Domain.HolidayCalendars;

public sealed record HolidayCreatedDomainEvent(Guid HolidayId) : IDomainEvent;
public sealed record HolidayUpdatedDomainEvent(Guid HolidayId) : IDomainEvent;
public sealed record HolidayDeletedDomainEvent(Guid HolidayId) : IDomainEvent;
