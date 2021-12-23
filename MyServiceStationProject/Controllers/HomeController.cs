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
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var client = GetClientFromDb(login);
                return View(client);
            }
            return View();
        }


        public IActionResult Home()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var client = GetClientFromDb(login);
                return View(client);
            }
            return View();
        }


        public IActionResult Privacy()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var order = GetOrderFromDb(login);
                //ViewData["order"] = order;
                return View(order);
            }
            return View();
        }
        [Authorize]
        public IActionResult Secured()
        {
            return View();
        }

        public IActionResult SignUp()
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
            if (client.EMail == username && client.Password == password)
            {
                var claims = new List<Claim>();
                claims.Add(new Claim("username", username));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimsPrincipal);
                return Redirect("/");
            }
            TempData["Error"] = "Error. Login or password is incorrect!";
            return View("login");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }

        [HttpPost("registration")]
        public IActionResult Registration(string firstName, string lastName, string email, string phone, string password, string confirmPassword)
        {
            if (firstName != null && lastName != null && email != null && phone != null && password != null && confirmPassword != null )
            {
                PutClientIntoDb(firstName, lastName, phone, email, password);
                return Redirect("/");
            }
            TempData["Error"] = "Error. Field can`t be empty!";
            return View("SignUp");
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
                return client.Count != 0 ? client[0] : new Client();
            }
        }


        public void PutClientIntoDb(string firstName, string lastName, string phone, string email, string password)
        {
            using (IDbConnection db = DbConnection)
            {
                db.Query($"INSERT INTO Clients (FirstName, LastName, Phone, Email, Password) VALUES ('{firstName}','{lastName}','{phone}','{email}','{password}')");
            }
        }

        public Order GetOrderFromDb(string email = "ddd@ddd.net")
        {
            if (User.Identity.IsAuthenticated)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Client> clientID = db.Query<Client>($"select ID from Clients where Email = '{ email }' ").ToList();
                    List<Order> order = db.Query<Order>($"select * from Orders where ClientID = '{ clientID[0].Id }'").ToList();
                    return order.Count != 0 ? order[0] : new Order();
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
