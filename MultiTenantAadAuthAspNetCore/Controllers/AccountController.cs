using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantAadAuthAspNetCore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public void SignIn()
        {
            Response.Redirect("/Index");

        }
    }
}
