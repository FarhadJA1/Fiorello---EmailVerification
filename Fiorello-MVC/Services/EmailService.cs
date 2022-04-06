using Fiorello_MVC.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiorello_MVC.Services
{
    public class EmailService : IEmailService
    {
        public void SendEmail(string emailAddress, string url)
        {
            throw new NotImplementedException();
        }
    }
}
