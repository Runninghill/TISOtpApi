using Microsoft.AspNetCore.Mvc;
using TISOtpApi.Data;
using TISOtpApi.Data.Dtos;
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
    private const int MaxAttempts = 3;
    private const int OtpExpirySeconds = 60;
    private const int RequestThrottleSeconds = 10;

    public OtpController(OtpDbContext db)
    {
        _db = db;
    }

    [HttpPost("request")]
    public IActionResult RequestOtp([FromBody] OtpRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!ValidEmails.Contains(email))
            return BadRequest("Email not found.");

        var existing = _db.OtpEntries.FirstOrDefault(e => e.Email == email);
        if (existing != null)
        {
            var now = DateTime.UtcNow;
            var secondsSinceLast = (now - existing.GeneratedAt).TotalSeconds;
            if (secondsSinceLast < RequestThrottleSeconds)
            {
                return BadRequest($"OTP was already generated recently. Please wait {Math.Ceiling(10 - secondsSinceLast)} seconds before requesting a new OTP.");
            }
            _db.OtpEntries.Remove(existing);
        }

        var otpCode = _rng.Next(100000, 999999).ToString();
        _db.OtpEntries.Add(new OtpEntry
        {
            Email = email,
            OtpCode = otpCode,
            GeneratedAt = DateTime.UtcNow
        });

        _db.SaveChanges();

        //Return OTP for demo purposes
        return Ok(new { email, otp = otpCode });
    }

    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpVerifyDto input)
    {
        var entry = _db.OtpEntries.FirstOrDefault(e => e.Email == input.Email.Trim().ToLowerInvariant());
        if (entry == null)
            return BadRequest("Validation failed, request new OTP.");

        if (entry.IsUsed)
            return BadRequest("OTP already used. Remaining Attempts: 0");

        // Check if OTP is expired (older than 1 minute)
        if ((DateTime.UtcNow - entry.GeneratedAt).TotalSeconds > OtpExpirySeconds)
        {
            _db.OtpEntries.Remove(entry);
            _db.SaveChanges();
            return BadRequest("OTP expired. Remaining Attempts: 0");
        }

        if (entry.OtpCode != input.OtpCode)
        {
            entry.FailedAttempts++;
            if (entry.FailedAttempts >= MaxAttempts)
            {
                _db.OtpEntries.Remove(entry);
                _db.SaveChanges();
                return BadRequest("Too many attempts. OTP expired.");
            }

            _db.SaveChanges();
            int remaining = MaxAttempts - entry.FailedAttempts;
            return BadRequest($"Invalid OTP. Remaining Attempts: {remaining}");
        }

        entry.IsUsed = true;
        _db.SaveChanges();

        return Ok(new { message = "OTP verified successfully" });
    }
}
