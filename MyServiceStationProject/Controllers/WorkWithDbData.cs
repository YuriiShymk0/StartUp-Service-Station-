using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyServiceStation.Controllers
{
    public enum Workers
    {
        [Display(Name = "Andrew  Korochka")]
        Andrew = 30001,
        [Display(Name = "Polina  Tyan")]
        Polina = 30002,
        [Display(Name = "Михайло  Хрущ")]
        Михайло = 30003,
        [Display(Name = "Дмитрий  Колено")]
        Дмитрий = 30005,
        [Display(Name = "Чеснок  Вахукка")]
        Чеснок = 30007
    }

    public enum Status
    {
        [Display(Name = "Idle")]
        Idle,
        [Display(Name = "Done")]
        Done,
        [Display(Name = "Ready")]
        Ready,
        [Display(Name = "In Work")]
        InWork
    }

     
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
        public bool IsAdmin { get; set; }
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
