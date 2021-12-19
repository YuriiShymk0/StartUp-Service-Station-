using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyServiceStation.Models;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace MyServiceStation.Controllers
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
            //var worker = GetWorkerFromDb();
            var client = GetClientFromDb();
            return View(client);
        }

        public IActionResult Privacy()
        {
            var order = GetOrderFromDb();
            return View(order);
        }
        [Authorize]
        public IActionResult Secured()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public List<Client> GetClientFromDb(int id = 10000)
        {
            using (IDbConnection db = DbConnection)
            {
                List<Client> client = db.Query<Client>($"SELECT * FROM Clients WHERE ID = {id} ").ToList();
                return client;
            }
        }
        public List<Order> GetOrderFromDb(int id = 20001)
        {
            using (IDbConnection db = DbConnection)
            {
                List<Order> order = db.Query<Order>($"SELECT * FROM Orders WHERE ID = {id} ").ToList();
                return order;
            }
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
