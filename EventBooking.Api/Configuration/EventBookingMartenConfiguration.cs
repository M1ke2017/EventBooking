using Marten;

namespace EventBooking.Api.Configuration;

public class EventBookingMartenConfiguration : IConfigureMarten
{
    public void Configure(
        IServiceProvider services,
        StoreOptions options)
    {
        var configuration = services
            .GetRequiredService<IConfiguration>();

        var connectionString = configuration
            .GetConnectionString("EventBooking")
            ?? throw new InvalidOperationException(
                "Missing EventBooking connection string.");

        options.Connection(connectionString);
    }
}
