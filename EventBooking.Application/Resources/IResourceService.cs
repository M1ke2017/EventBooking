using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBooking.Domain.Resources;


namespace EventBooking.Application.Resources
{
    public interface IResourceService
    {
        Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken ct);
        Task<Resource?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Resource> CreateAsync(string name, int capacity, CancellationToken ct);
        Task<bool> UpdateAsync(Guid id, string name, int capacity, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}
