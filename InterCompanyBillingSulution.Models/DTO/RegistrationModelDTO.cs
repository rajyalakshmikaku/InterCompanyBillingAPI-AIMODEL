using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterCompanyBillingSulution.Models.DTO
{
    public class RegistrationModelDTo
    {
            public int? UserId { get; set; }
            public int role { get; set; }
            public required string username { get; set; }
            public string name { get; set; }
            public string surname { get; set; }
            public required string email { get; set; }
            public string mobile { get; set; }
            public int company { get; set; }
            public int? department { get; set; }
            public string title { get; set; }
            public string gender { get; set; }
            public string customerAccount { get; set; }
            public string address { get; set; }
            public string password { get; set; }
            public string? confirmPassword { get; set; }
            public string? activationlink { get; set; }
            public string? isActive { get; set; }
            public string? isDeleted { get; set; }

    }
}
