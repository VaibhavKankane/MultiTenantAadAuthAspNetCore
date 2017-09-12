using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultiTenantAadAuthAspNetCore.Controllers
{
    public class AccountController : Controller
    {
        public string SignIn()
        {
            return "working";
        }
    }
}
