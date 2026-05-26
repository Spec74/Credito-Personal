using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Api.Hosting;

internal sealed class ConfigureForwardedHeadersOptions(IOptions<ForwardedHeadersBindingOptions> bindingOptions)
    : IConfigureOptions<ForwardedHeadersOptions>
{
    public void Configure(ForwardedHeadersOptions fo)
    {
        var binding = bindingOptions.Value;
        fo.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        fo.KnownProxies.Clear();
        if (!binding.Enabled)
            return;

        foreach (var s in binding.KnownProxies)
        {
            if (string.IsNullOrWhiteSpace(s))
                continue;
            if (System.Net.IPAddress.TryParse(s.Trim(), out var ip))
                fo.KnownProxies.Add(ip);
        }
    }
}
