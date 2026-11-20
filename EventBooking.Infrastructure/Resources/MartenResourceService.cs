using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Application.Resources;
using EventBooking.Domain.Resources;
using Marten;

namespace EventBooking.Infrastructure.Resources
{
    public class MartenResourceService(IDocumentSession session) : IResourceService
    {
        public async Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken ct)
        { return await session.Query<Resource>().OrderBy(x => x.Name).ToListAsync(ct); }

        public Task<Resource?> GetByIdAsync(Guid id, CancellationToken ct)
        { return session.LoadAsync<Resource>(id, ct); }

        public async Task<Resource> CreateAsync(string name, int capacity, CancellationToken ct)
        {
            var resource = new Resource { Id = Guid.NewGuid(), Name = name.Trim(), Capacity = capacity };
            session.Store(resource); await session.SaveChangesAsync(ct); return resource;
        }
        public async Task<bool> UpdateAsync(Guid id, string name, int capacity, CancellationToken ct)
        {
            var resource = await session.LoadAsync<Resource>(id, ct);
            if (resource is null)return false; resource.Name = name.Trim(); resource.Capacity = capacity; session.Store(resource);
            await session.SaveChangesAsync(ct); return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var resource = await session.LoadAsync<Resource>(id, ct);
            if (resource is null)return false;
            session.Delete(resource); await session.SaveChangesAsync(ct); return true;
        }
    }
}
