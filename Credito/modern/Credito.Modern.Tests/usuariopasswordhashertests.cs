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

    [Fact]
    public void CreateHash_cabe_en_nvarchar_256_y_no_en_50()
    {
        var hash = UsuarioPasswordHasher.CreateHash("MiClave_S3gura");
        Assert.InRange(hash.Length, 51, 256);
    }

    [Fact]
    public void CreateTemporaryHash_marca_requiere_cambio_y_verifica()
    {
        var plain = UsuarioPasswordHasher.GenerateTemporaryPassword();
        var hash = UsuarioPasswordHasher.CreateTemporaryHash(plain);
        Assert.True(UsuarioPasswordHasher.RequiresPasswordChange(hash));
        Assert.False(UsuarioPasswordHasher.RequiresPasswordChange(UsuarioPasswordHasher.CreateHash(plain)));
        Assert.True(UsuarioPasswordHasher.Verify(hash, plain, out _));
    }
}
