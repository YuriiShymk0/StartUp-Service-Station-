using System;

namespace MyServiceStation.Controllers
{

    public class Client
    {
        public int Id { get ; set; }
        public string FirstName { get ; set; }
        public string LastName { get ; set ; }
        public string Phone { get ; set; }
        public string EMail { get  ; set; }
        public string Password { get; set; }
    }

    public class Worker
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string EMail { get; set; }
        public string Password { get; set; }
        public short Rating { get; set; }
        public bool Admin { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public string CarNumber { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int ClientID { get; set; }
        public int WorkerID { get; set; }
        public string Status { get; set; }
        public DateTime DeadLine { get; set; }
        public int Price { get; set; }
    }
}
