using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MakeMakeMoney.Models;

namespace MakeMakeMoney.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost("View")]
        public IActionResult View(Models.CreditPost creditPost)
        {
            if (creditPost.Key != "THISISKEY") return Ok("bad key");

            if (creditPost.Count < 0) return Ok("bad cnt");
            if (creditPost.Count > 10) return Ok("bad cnt");

            int res = Gpio.Coin(creditPost.Count);
            if (res == 0) return Ok("ok");
            if (res == -1) return Ok("in prog");

            throw new Exception("shouldn't be here");
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
