using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NextHorizon.Filters
{
    public class AuthenticationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var username = context.HttpContext.Session.GetString("Username");

            if (userId == null || string.IsNullOrEmpty(username))
            {
                context.Result = new RedirectToActionResult("AdminLogin", "Login", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
