using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using System.Security.Claims;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsumerController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ConsumerController(AppDbContext db) { _db = db; }

        // GET: api/consumer/my-addresses
        // Returns addresses belonging ONLY to the currently logged-in consumer.
        // Auth detection order: Claims ? Session ? Cookie
        [HttpGet("my-addresses")]
        public async Task<IActionResult> GetMyAddresses()
        {
            // ?? 1. Try ASP.NET Core claims (standard auth) ??????????????
            var claimEmail  = HttpContext.User?.FindFirst(ClaimTypes.Email)?.Value
                           ?? HttpContext.User?.FindFirst(ClaimTypes.Name)?.Value;
            var claimUserId = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ?? 2. Try session (common key names the login module might use) ??
            var sessEmail  = HttpContext.Session.GetString("user_email")
                          ?? HttpContext.Session.GetString("email")
                          ?? HttpContext.Session.GetString("UserEmail");
            var sessUserId = HttpContext.Session.GetString("user_id")
                          ?? HttpContext.Session.GetString("userId")
                          ?? HttpContext.Session.GetString("UserId");

            // ?? 3. Try cookies ???????????????????????????????????????????
            var cookieEmail  = HttpContext.Request.Cookies["user_email"]
                            ?? HttpContext.Request.Cookies["userEmail"]
                            ?? HttpContext.Request.Cookies["UserEmail"];
            var cookieUserId = HttpContext.Request.Cookies["user_id"]
                            ?? HttpContext.Request.Cookies["userId"]
                            ?? HttpContext.Request.Cookies["UserId"];

            // Pick the best available identifier
            var email  = claimEmail  ?? sessEmail  ?? cookieEmail;
            var userId = claimUserId ?? sessUserId ?? cookieUserId;

            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(userId))
                return Ok(new { status = "not_logged_in", addresses = new List<object>() });

            // ?? Look up the user ?????????????????????????????????????????
            MyAspNetApp.Models.DbUser? user = null;

            if (!string.IsNullOrEmpty(email))
                user = await _db.Users.FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email.ToLower());

            if (user == null && int.TryParse(userId, out int uid))
                user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == uid);

            if (user == null)
                return Ok(new { status = "not_logged_in", addresses = new List<object>() });

            // ?? Find this user's consumer record ?????????????????????????
            var consumer = await _db.Consumers
                .FirstOrDefaultAsync(c => c.UserId == user.UserId);

            if (consumer == null)
                return Ok(new { status = "no_address", addresses = new List<object>() });

            var fullName = string.IsNullOrEmpty(consumer.MiddleName)
                ? $"{consumer.FirstName} {consumer.LastName}"
                : $"{consumer.FirstName} {consumer.MiddleName} {consumer.LastName}";

            var addresses = new[]
            {
                new
                {
                    consumerId = consumer.ConsumerId,
                    fullName,
                    email   = user.Email,
                    phone   = consumer.PhoneNumber ?? "",
                    address = consumer.Address
                }
            };

            return Ok(new { status = "ok", addresses });
        }

        // GET: api/consumer/addresses  — all addresses (kept for admin/testing)
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAllAddresses()
        {
            var consumers = await _db.Consumers.ToListAsync();
            var userIds   = consumers.Select(c => c.UserId).Distinct().ToList();
            var users     = await _db.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
            var result = consumers.Select(c =>
            {
                var user     = users.FirstOrDefault(u => u.UserId == c.UserId);
                var fullName = string.IsNullOrEmpty(c.MiddleName)
                    ? c.FirstName + " " + c.LastName
                    : c.FirstName + " " + c.MiddleName + " " + c.LastName;
                return new { consumerId = c.ConsumerId, fullName, email = user?.Email ?? "", phone = c.PhoneNumber ?? "", address = c.Address };
            }).ToList();
            return Ok(result);
        }
    }
}
