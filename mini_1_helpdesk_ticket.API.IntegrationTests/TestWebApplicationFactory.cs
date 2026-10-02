using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace mini_1_helpdesk_ticket.API.IntegrationTests;

internal sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _configureServices;

    public TestWebApplicationFactory(
        Action<IServiceCollection> configureServices)
    {
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
            logging.ClearProviders());

        builder.ConfigureTestServices(_configureServices);
    }
}
