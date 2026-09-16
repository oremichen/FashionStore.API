using FashionStore.Domain.Abstractions.Delivery;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.DeliveryRepo;

public sealed class DeliveryRepository(FashionStoreDbContext dbContext) : IDeliveryRepository
{
    public async Task<IReadOnlyList<DeliveryRate>> GetRatesAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.DeliveryRates.Include(rate => rate.Zone).Include(rate => rate.Method).AsNoTracking();
        if (activeOnly) query = query.Where(rate => rate.IsActive && rate.Zone.IsActive && rate.Method.IsActive);
        return await query.OrderBy(rate => rate.Zone.Name).ThenBy(rate => rate.Method.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryZone>> GetZonesAsync(CancellationToken cancellationToken)
    {
        return await (from zone in dbContext.DeliveryZones.AsNoTracking()
                      orderby zone.Name
                      select zone).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryMethod>> GetMethodsAsync(CancellationToken cancellationToken)
    {
        return await (from method in dbContext.DeliveryMethods.AsNoTracking()
                      orderby method.Name
                      select method).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryRate>> GetActiveRatesForStateAsync(string state, CancellationToken cancellationToken)
    {
        var normalized = state.Trim().ToLower();
        var matched = await (from rate in dbContext.DeliveryRates.AsNoTracking().Include(item => item.Zone).Include(item => item.Method)
                             where rate.IsActive && rate.Zone.IsActive && rate.Method.IsActive &&
                                   (from location in dbContext.DeliveryZoneLocations 
                                    where location.ZoneId == rate.ZoneId && location.State.ToLower() == normalized 
                                    select location.Id).Any()
                             select rate).OrderBy(rate => rate.Method.Name).ToListAsync(cancellationToken);
        if (matched.Count > 0) return matched;
        return await (from rate in dbContext.DeliveryRates.AsNoTracking().Include(item => item.Zone).Include(item => item.Method)
                      where rate.IsActive && rate.Zone.IsActive && rate.Method.IsActive && rate.Zone.IsDefault
                      select rate).OrderBy(rate => rate.Method.Name).ToListAsync(cancellationToken);
    }

    public async Task<DeliveryRate?> GetActiveRateForStateAsync(string id, string state, CancellationToken cancellationToken)
    {
        var rate = await (from item in dbContext.DeliveryRates.Include(item => item.Zone).Include(item => item.Method)
                          where item.Id == id && item.IsActive && item.Zone.IsActive && item.Method.IsActive
                          select item).SingleOrDefaultAsync(cancellationToken);
        if (rate is null) return null;
        var ratesForState = await GetActiveRatesForStateAsync(state, cancellationToken);
        return ratesForState.Any(item => item.Id == rate.Id) ? rate : null;
    }

    public Task<DeliveryRate?> GetRateByIdAsync(string id, CancellationToken cancellationToken)
    {
        return dbContext.DeliveryRates.SingleOrDefaultAsync(rate => rate.Id == id, cancellationToken);
    }

    public Task<DeliveryZone?> GetZoneByIdAsync(string id, CancellationToken cancellationToken) => dbContext.DeliveryZones.SingleOrDefaultAsync(zone => zone.Id == id, cancellationToken);
    public Task<DeliveryMethod?> GetMethodByIdAsync(string id, CancellationToken cancellationToken) => dbContext.DeliveryMethods.SingleOrDefaultAsync(method => method.Id == id, cancellationToken);
    public Task<bool> RateExistsAsync(string zoneId, string methodId, string? excludingId, CancellationToken cancellationToken) => dbContext.DeliveryRates.AnyAsync(rate => rate.ZoneId == zoneId && rate.MethodId == methodId && rate.Id != excludingId, cancellationToken);
    public Task AddRateAsync(DeliveryRate rate, CancellationToken cancellationToken) => dbContext.DeliveryRates.AddAsync(rate, cancellationToken).AsTask();
    public async Task DeleteRateAsync(DeliveryRate rate, CancellationToken cancellationToken)
    {
        dbContext.DeliveryRates.Remove(rate);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
