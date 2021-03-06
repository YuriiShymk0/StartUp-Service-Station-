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

        public string[] Statuses = { "Idle", "Done", "Ready", "In Work" };

        private readonly ILogger<HomeController> _logger;

        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IDbConnection DbConnection => new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

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

        
        public IActionResult UserProfile()
        {
            string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
            var client = GetClientFromDb(login);
            return View(client[0]);
        }

        [HttpPost("ChangeUser")]
        public IActionResult ChangeUser(string firstName, string lastName, string phone, string email, string password)
        {
            UpdateUser(firstName, lastName, phone, email, password);
            return Redirect("/Home/UserProfile");
        }
        public void UpdateUser(string firstName, string lastName, string phone, string email, string password)
        {
            if (email != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Order> order = db.Query<Order>($"UPDATE Clients SET FirstName = '{firstName}', LastName = '{ lastName }', Phone = '{ phone }', Email = '{ email }', Password = '{ password }' WHERE Email = '{ email }'").ToList();
                }
            }
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
                var user = GetClientFromDb(login);
                var orders = GetClientOrdersFromDb(user[0].EMail);
                List<Worker> workers = new List<Worker>();
                foreach (var item in orders)
                {
                    workers.Add(GetWorkerNameFromDb(item.WorkerID));
                }
                if (orders != null)
                {
                    TempData["worker"] = workers;
                    ViewData["order"] = orders;
                    return View(orders);
                }
            }
            return View();
        }

        public IActionResult SignUp() => View();

        [Authorize(Roles = "Admin")]
        public IActionResult OrdersList()
        {
            if (User.Identity.IsAuthenticated)
            {
                string login = User.Claims.FirstOrDefault(c => c.Type == "username").Value;
                var orders = GetAllOrdersFromDb();
                ViewData["order"] = orders;
                List<Order> phoneGroups = orders.Select(x => x).OrderBy(x => x.Status).Reverse().ToList();
                List<Worker> workers = new List<Worker>();
                foreach (var item in phoneGroups)
                {
                    workers.Add(GetWorkerNameFromDb(item.WorkerID));
                }
                if (orders != null)
                {
                    TempData["worker"] = workers;
                    ViewData["order"] = phoneGroups;
                    return View(phoneGroups);
                }
                TempData["worker"] = workers;
                return View();
            }
            return Redirect("/");
        }

        [HttpPost("addorder")]
        public IActionResult AddOrder(string CarNumber, string Brand, string Model, string PhoneNumber, int WorkerID, DateTime Deadline, int Price)
        {
            int check = 0;
            if (CarNumber != null && Brand != null && Model != null && Deadline != default && Price != 0)
            {
                var user = GetClientFromDb(PhoneNumber);
                if (user.Count != 0)
                {
                    CreateNewOrder(CarNumber, Brand, Model, user[0].Id, WorkerID, "Idle", Deadline, Price);
                }
                else
                {
                    PutClientIntoDb("FirstName", "LastName", PhoneNumber, "email", "password");
                    var usr = GetClientFromDb(PhoneNumber);
                    CreateNewOrder(CarNumber, Brand, Model, usr[0].Id, WorkerID, "Idle", Deadline, Price);
                }
                TempData["Success"] = "Order was successuly created!";
                return View("CreateOrder");
            }
            else if (check == 0)
            {
                TempData["Error"] = "Error. Field can`t be empty!";
                return View("CreateOrder");
            }
            else
            {
                check++;
                return View("CreateOrder");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateOrder() => View();

        [HttpPost("ManageOrder")]
        public IActionResult ManageOrder(string carNumber)
        {
            var order = GetCarFromDb(carNumber);
            return View(order[0]);
        }

        [HttpPost("UpdateOrder")]
        public IActionResult UpdateOrder(int orderID, string carNumber, string brand, string model, string deadline, int price, int status)
        {
            if (orderID != 0 && carNumber != null && brand != null && model != null && deadline != default && price != 0)
            {
                    UpdateCarInDB(orderID, carNumber, brand, model, deadline, price, Statuses[status]);
                    return Redirect("/Home/OrdersList");
                
            }
            TempData["Error"] = "Error. Field can`t be empty!";
            var order = DbConnection.Query<Order>($"select * from Orders where ID = '{ orderID }' AND Status != 'Done'").ToList();
            return View("ManageOrder", order[0]);
        }

        [HttpPost("SearchResult")]
        public IActionResult SearchResult(string adminQuery)
        {
            List<Order> searchResult = SearchingForOrders(adminQuery);
            if (searchResult.Count != 0)
            {
                return View("OrdersList", searchResult);

            }
            else {
                TempData["Error"] = "Nothing to see here :^(!";
                return View("OrdersList",searchResult);
            }
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
                if ((client[0].EMail == username || client[0].Phone == username) && client[0].Password == password)
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

        public List<Order> GetClientOrdersFromDb(string email)
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

        public List<Order> SearchingForOrders(string searchQuery)
        {
            if (User.Identity.IsAuthenticated)
            {
                if(searchQuery != null)
                {
                    using (IDbConnection db = DbConnection)
                    {
                        List<Order> order = db.Query<Order>($"select * from Orders where CarNumber = '{ searchQuery }' OR Status = '{ searchQuery }' OR Brand = '{ searchQuery }' OR Model = '{ searchQuery }'").ToList();
                        if (order.Count == 0)
                        {
                            List<Client> clientID = db.Query<Client>($"select ID from Clients where Phone = '{ searchQuery }' ").ToList();
                            if (clientID.Count != 0)
                            {
                                List<Order> postSearch = db.Query<Order>($"select * from Orders where ClientID = '{ clientID[0].Id }'").ToList();
                                return postSearch.Count != 0 ? postSearch : new List<Order>();
                            }else
                                return new List<Order>();
                            //List<Order> daClient = db.Query<Order>($"select * from Clients where Phone = '{ searchQuery }' OR FirstName = '{ searchQuery }' OR LastName = '{ searchQuery }' OR Model = '{ searchQuery }'").ToList();
                            //int i = 0;
                            //List<Order> filterArray = new List<Order>;
                            //foreach (var item in daClient){
                            //    List<Order> postOrders = db.Query<Order>($"select * from Orders where ClientID = '{ daClient[0].Id }'").ToList();
                            //    var j = 0;
                            //    foreach (var thing in postOrders) { 

                            //        filterArray.Insert(0, postOrders[j]) = postOrders;
                            //        j++;
                            //    }
                            //}
                            //return filterArray;
                            //order = db.Query<Order>($"select * from Orders where CarNumber = '{ searchQuery }' OR Status = '{ searchQuery }' OR Brand = '{ searchQuery }' OR Model = '{ searchQuery }'").ToList();
                        }
                        return order.Count != 0 ? order : new List<Order>();
                    }
                }
                else
                    return new List<Order>();
            }
            else
                return new List<Order>();
        }

        public List<Worker> GetWorkerFromDb(string email)
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

        public Worker GetWorkerNameFromDb(int workerId)
        {
            if (workerId != 0)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Worker> workers = db.Query<Worker>($"SELECT * FROM Workers WHERE ID = '{ workerId }' AND Admin = 'False'").ToList();
                    return workers[0];
                }
            }
            else
                return new Worker();
        }

        public List<Order> GetCarFromDb(string carNumber)
        {
            if (carNumber != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Order> order = db.Query<Order>($"select * from Orders where CarNumber = '{ carNumber }' AND Status != 'Done'").ToList();
                    return order.Count != 0 ? order : new List<Order>();
                }
            }
            else
                return new List<Order>();
        }

        public void UpdateCarInDB(int id, string carNumber, string brand, string model, string deadline, int price, string status)
        {
            if (carNumber != null)
            {
                using (IDbConnection db = DbConnection)
                {
                    List<Order> order = db.Query<Order>($"UPDATE Orders SET CarNumber = '{ carNumber }', Brand = '{ brand }', Model = '{ model }', Deadline = '{ deadline }', Price = '{ (int)price }', Status = '{ status }' WHERE ID = '{ id }'; ").ToList();
                }
            }
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
            var mounth = Dedline.Month;
            var day = Dedline.Day;
            var year = Dedline.Year;
            using (IDbConnection db = DbConnection)
            {
                db.Query($"INSERT INTO Orders (CarNumber, Brand, Model, ClientID, WorkerID, Status, Deadline, Price) VALUES ('{ CarNumber }','{ Brand }','{ Model }','{ ClientID }','{ WorkerID }','{ Status }','{$"{mounth}.{day}.{year}" }','{ Price }') ");
            }
        }
    }
}
