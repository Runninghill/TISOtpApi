using Microsoft.AspNetCore.Mvc;
using TISOtpApi.Data;
using TISOtpApi.Models;

namespace TISOtpApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OtpController : ControllerBase
{
    private static readonly List<string> ValidEmails = new()
    {
        "test1@example.com",
        "test2@example.com",
        "test3@example.com",
        "test4@example.com",
        "test5@example.com"
    };

    private readonly OtpDbContext _db;
    private readonly Random _rng = new();

    public OtpController(OtpDbContext db)
    {
        _db = db;
    }

    [HttpPost("request")]
    public IActionResult RequestOtp([FromBody] string email)
    {
        if (!ValidEmails.Contains(email))
            return BadRequest("Email not found.");

        var otpCode = _rng.Next(100000, 999999).ToString();

        var existing = _db.OtpEntries.FirstOrDefault(e => e.Email == email);
        if (existing != null)
            _db.OtpEntries.Remove(existing);

        _db.OtpEntries.Add(new OtpEntry
        {
            Email = email,
            OtpCode = otpCode
        });

        _db.SaveChanges();

        // In a real system, you'd send this via SMS. For demo, return it.
        return Ok(new { email, otp = otpCode });
    }

    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpEntry input)
    {
        var entry = _db.OtpEntries.FirstOrDefault(e => e.Email == input.Email);
        if (entry == null)
            return BadRequest("OTP not found.");

        if (entry.IsUsed)
            return BadRequest("OTP already used.");

        if (entry.FailedAttempts >= 3)
            return BadRequest("Too many attempts. OTP expired.");

        if (entry.OtpCode != input.OtpCode)
        {
            entry.FailedAttempts++;
            _db.SaveChanges();
            return BadRequest("Invalid OTP.");
        }

        entry.IsUsed = true;
        _db.SaveChanges();

        return Ok("OTP verified successfully.");
    }
}
