using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyServiceStation.Controllers;
using MyServiceStationProject.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
                if (client.Count == 0)
                {
                    var worker = GetWorkerFromDb(login);
                    return View(worker);
                }
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
                if (client.Count == 0)
                {
                    var worker = GetWorkerFromDb(login);
                    return View(worker[0]);
                }
                else
                {
                    return View(client[0]);
                }
            }
            return View();
        }


        public IActionResult Privacy()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var order = GetClientOrderFromDb(login);
                ViewData["order"] = order;
                return View(order);
            }
            return View();

        }

        public IActionResult SignUp()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult OrdersList()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var order = GetAllOrdersFromDb();
                ViewData["order"] = order;
                return View(order);
            }
            return Redirect("/");
        }
        //[Authorize(Roles = "Admin")]
        //public IActionResult CreateOrder()
        //{
        //    return View();
        //}


        [HttpPost("addorder")]
        public IActionResult AddOrder(string CarNumber, string Brand, string Model, int ClientID, int WorkerID, string Status, DateTime Deadline, int Price)
        {
            if (CarNumber != null && Brand != null && Model != null && Status != null && Deadline != default && Price != 0)
            {
                CreateNewOrder(CarNumber, Brand, Model, ClientID, WorkerID, Status, Deadline, Price);
                return Redirect("/Home/OrdersList");
            }
            TempData["Error"] = "Error. Field can`t be empty!";
            return View("/Home/CreateOrder");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateOrder()
        {
            return View();
        }


        [Authorize(Roles = "Admin")]
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

        [HttpPost("registration")]
        public IActionResult Registration(string firstName, string lastName, string email, string phone, string password, string confirmPassword)
        {
            if (firstName != null && lastName != null && email != null && phone != null && password != null && confirmPassword != null)
            {
                if (password == confirmPassword)
                {
                    PutClientIntoDb(firstName, lastName, phone, email, password);
                    return Redirect("/");
                }
            }
            TempData["Error"] = "Error. Field can`t be empty!";
            return View("SignUp");
        }


        [HttpPost("login")]
        public async Task<IActionResult> Validate(string username, string password, string returnUrl)
        {
            var client = GetClientFromDb(username);
            if (client.Count != 0)
            {
                ViewData["ReturnUrl"] = returnUrl;
                if (client[0].EMail == username || client[0].Phone == username && client[0].Password == password)
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

        public List<Client> GetClientFromDb(string userLogin)
        {
            if (userLogin != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Client> clients = db.Query<Client>($"select * from Clients where Email = '{ userLogin }' OR Phone = '{ userLogin }' ").ToList();
                    return clients;
                }
            }
            else
                return new List<Client>();
        }

        public List<Order> GetClientOrderFromDb(string email = "ddd@ddd.net")
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
        public List<Order> GetAllOrdersFromDb()
        {
            if (User.Identity.IsAuthenticated)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Order> order = db.Query<Order>($"select * from Orders ").ToList();
                    return order.Count != 0 ? order : new List<Order>();
                }
            }
            else
                return new List<Order>();
        }

        public List<Worker> GetWorkerFromDb(string email) //add correct query
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

        public void PutClientIntoDb(string firstName, string lastName, string phone, string email, string password)
        {
            using (IDbConnection db = DbConnection)
            {
                db.Query($"INSERT INTO Clients (FirstName, LastName, Phone, Email, Password) VALUES ('{firstName}','{lastName}','{phone}','{email}','{password}')");
            }
        }

        public void CreateNewOrder(string CarNumber, string Brand, string Model, int ClientID, int WorkerID, string Status, DateTime Dedline, int Price)
        {
            using (IDbConnection db = DbConnection)
            {
                db.Query($"INSERT INTO Orders (CarNumber, Brand, Model, ClientID, WorkerID, Status, Deadline, Price) VALUES ('{ CarNumber }','{ Brand }','{ Model }','{ ClientID }','{ WorkerID }','{ Status }','{ Dedline }','{ Price }')");
            }
        }
    }
}
