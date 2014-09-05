using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQActors.Models
{
    public class Account
    {

        public Account()
        {
            
        }

        public Account(int id, string name, string sortCode, decimal balance)
        {
            Id = id;
            Name = name;
            SortCode = sortCode;
            Balance = balance;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string SortCode { get; set; }
        public decimal Balance { get; set; }
    }
}
