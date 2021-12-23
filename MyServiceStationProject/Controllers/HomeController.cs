using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using MyServiceStationProject.Models;
using MyServiceStation.Controllers;

namespace MyServiceStationProject.Controllers
{
    public class HomeController : Controller
    {
        
        private readonly ILogger<HomeController> _logger;

        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IDbConnection DbConnection
        {
            get
            {
                return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            }
        }
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var client = GetClientFromDb(login);
                return View(client[0]);
            }
            return View();
        }

        public IActionResult Home()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var client = GetClientFromDb(login);
                return View(client[0]);
            }
            return View();
        }


        public IActionResult Privacy()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var order = GetOrderFromDb(login);
                ViewData["order"] = order;
                return View(order[0]);
            }
            return View();

        }

        public IActionResult SignUp()
        {
            return View();
        }

        [Authorize]
        public IActionResult OrdersList()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var order = GetOrderFromDb(login);
                ViewData["order"] = order;
                return View(order);
            }
            return Redirect("/");
        }

        [Authorize]
        public IActionResult CreateOrder()
        {
            return View();
        }

        [Authorize]
        public IActionResult ManageOrder()
        {
            return View();
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [HttpPost("login")]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            var client = GetClientFromDb(username);
            if (client.Count != 0)
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (client[0].EMail == username && client[0].Password == password)
                {
                    var claims = new List<Claim>();
                    claims.Add(new Claim("username", username));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(claimsPrincipal);
                    return Redirect("/");
                }
                TempData["Error"] = "Error. Login or password is incorrect!";
                return View("login");
            }
            else
            {
                var worker = GetWorkerFromDb(username);
                if (worker.Count != 0)
                {
                    ViewData["ReturnUrl"] = returnUrl;
                    if (worker[0].EMail == username && worker[0].Password == password)
                    {
                        var claims = new List<Claim>();
                        claims.Add(new Claim("username", username));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                        await HttpContext.SignInAsync(claimsPrincipal);
                        return Redirect("/");
                    }
                    TempData["Error"] = "Error. Login or password is incorrect!";
                    return View("login");
                }
                else
                {
                    TempData["Error"] = "Error. Login or password is incorrect!";
                    return View("login");
                }
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public List<Client> GetClientFromDb(string email = "ddd@ddd.net")
        {
            if (email != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Client> client = db.Query<Client>($"select * from Clients where Email = '{ email }' ").ToList();
                    return client;
                }
            }
            else
                return new List<Client>();
        }

        public List<Order> GetOrderFromDb(string email = "ddd@ddd.net")
        {
            if (User.Identity.IsAuthenticated)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Client> clientID = db.Query<Client>($"select ID from Clients where Email = '{ email }' ").ToList();
                    List<Order> order = db.Query<Order>($"select * from Orders where ClientID = '{ clientID[0].Id }'").ToList();
                    return order.Count != 0 ? order : new List<Order>();
                }
            }
            else
                return new List<Order>();
        }

        public List<Worker>GetWorkerFromDb(string email) //add correct query
        {
            if (email != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Worker> worker = db.Query<Worker>($"SELECT * FROM Workers WHERE EMail = '{ email }' AND Admin = 'True' ").ToList();
                    return worker;
                }
            }
            else
                return new List<Worker>();
        }
    }
}
