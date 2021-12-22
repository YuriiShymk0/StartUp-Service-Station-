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
using System.Collections.Generic;
using System.Collections;
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
            return View();
        }

        
        public IActionResult Home(string username)
        {
            if (User.Identity.IsAuthenticated)
            {
                var client = GetClientFromDb(username);
                return View(client);
            }
            return View();
        }

        public IActionResult Privacy()
        {
            var order = GetOrderFromDb();
            string[] arrclient = {"" };
            ViewData["order"] = order;
            return View(order);

        }
        [Authorize]
        public IActionResult Secured()
        {
            return View();
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            var client = GetClientFromDb(username);
            ViewData["ReturnUrl"] = returnUrl;
            if (client.EMail == username && client.Password == password)
            {
                var claims = new List<Claim>();
                claims.Add(new Claim("username", username));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, username));

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                return Redirect(returnUrl);
            }
            TempData["Error"] = "Error. Login or password is incorrect!";
            return View("login");
        }

        [Authorize]
        public async  Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public Client GetClientFromDb(string email = "ddd@ddd.net")
        {
            using (IDbConnection db = DbConnection)
            {
                List<Client> client = db.Query<Client>($"select * from Clients where Email = '{ email }' ").ToList();
                return client[0];
            }
        }

        public Order GetOrderFromDb(string email = "ddd@ddd.net")
        {

            if (User.Identity.IsAuthenticated)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Order> order = db.Query<Order>($"select * from Orders inner join Clients on  Orders.ClientID=(select ClientID from Clients where Email = '{ email }')").ToList();

                    return order[0];
                }
            }
            else
                return new Order();
        }

        public List<Worker> GetWorkerFromDb(int id = 30001)
        {
            using (IDbConnection db = DbConnection)
            {
                List<Worker> worker = db.Query<Worker>($"SELECT * FROM Workers WHERE ID = {id} ").ToList();
                return worker;
            }
        }
    }
}
