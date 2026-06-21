using System.Security.Claims;
using LafErp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace LafErp.Web.Controllers;

/// <summary>
/// Real username/password login (PBKDF2) issuing a cookie identity with role claims. Hardened:
/// anti-forgery on POST, audited login/logout, and lockout messaging from the auth service.
/// </summary>
public class AccountController : Controller
{
    private readonly UserAuthService _auth;
    public AccountController(UserAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password)
    {
        var result = _auth.Authenticate(username ?? "", password ?? "");
        if (!result.Ok) { ViewBag.Error = result.Error ?? "Invalid username or password."; return View(); }

        var claims = new List<Claim> { new(ClaimTypes.Name, result.Username) };
        claims.AddRange(result.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var id = new ClaimsIdentity(claims, "LafErpCookie");
        await HttpContext.SignInAsync("LafErpCookie", new ClaimsPrincipal(id),
            new AuthenticationProperties { IsPersistent = false });
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var name = User?.Identity?.Name;
        if (!string.IsNullOrEmpty(name)) _auth.RecordLogout(name);
        await HttpContext.SignOutAsync("LafErpCookie");
        return RedirectToAction("Login");
    }
}
