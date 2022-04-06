using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiorello_MVC.Interfaces
{
    public interface IEmailService
    {
        public void SendEmail(string emailAddress, string url);
        

    }
}
