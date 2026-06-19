using LocalAIFactory.Core.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace LocalAIFactory.Data.Security;

public sealed class DataProtectionApiKeyProtector : IApiKeyProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionApiKeyProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("LocalAIFactory.ApiKeys.v1");
    }

    public string Protect(string? plaintext)
        => string.IsNullOrEmpty(plaintext) ? "" : _protector.Protect(plaintext);

    public string Unprotect(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return "";
        try { return _protector.Unprotect(ciphertext); }
        catch { return ""; }
    }
}
