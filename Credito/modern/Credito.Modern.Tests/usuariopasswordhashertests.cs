using Credito.Modern.Infrastructure.Auth;

namespace Credito.Modern.Tests;

public class UsuarioPasswordHasherTests
{
    [Fact]
    public void CreateHash_y_Verify_aciertan()
    {
        var hash = UsuarioPasswordHasher.CreateHash("MiClave_S3gura");
        Assert.True(UsuarioPasswordHasher.LooksLikeStoredHash(hash));
        Assert.True(UsuarioPasswordHasher.Verify(hash, "MiClave_S3gura", out var plain));
        Assert.False(plain);
    }

    [Fact]
    public void Verify_contrasena_incorrecta_falla()
    {
        var hash = UsuarioPasswordHasher.CreateHash("una");
        Assert.False(UsuarioPasswordHasher.Verify(hash, "otra", out _));
    }

    [Fact]
    public void Verify_legado_plano_sin_prefijo()
    {
        Assert.True(UsuarioPasswordHasher.Verify("secreto", "secreto", out var plain));
        Assert.True(plain);
        Assert.False(UsuarioPasswordHasher.Verify("secreto", "Secreto", out _));
    }

    [Fact]
    public void LooksLikeStoredHash_rechaza_plano()
    {
        Assert.False(UsuarioPasswordHasher.LooksLikeStoredHash("password12"));
    }
}
