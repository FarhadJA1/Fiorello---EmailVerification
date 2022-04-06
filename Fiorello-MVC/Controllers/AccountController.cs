using Fiorello_MVC.Models;
using Fiorello_MVC.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiorello_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Register()
        {

            return View();
        }
        //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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM registerVM )
        {
            if (!ModelState.IsValid) return View(registerVM);
            AppUser newUser = new AppUser
            {
                FullName=registerVM.FullName,
                Email=registerVM.Email,
                UserName=registerVM.Username,
                
            };
            IdentityResult result =await _userManager.CreateAsync(newUser, registerVM.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("",error.Description);
                    return View(registerVM);
                }
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

            var link = Url.Action("VerifyEmail", "Account", new {userId=newUser.Id,token=code },Request.Scheme,Request.Host.ToString());
            
            await SendEmail(newUser.Email,link);
            
            return RedirectToAction(nameof(EmailVerification));
        }

        public async Task<IActionResult> VerifyEmail(string userId,string token)
        {
            AppUser user =await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            if (userId == null || token == null) return BadRequest();
            
            await _userManager.ConfirmEmailAsync(user, token);
            await _signInManager.SignInAsync(user,false);
            return RedirectToAction("Index","Home");
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid) return View(loginVM);

            AppUser user =await _userManager.FindByEmailAsync(loginVM.EmailOrUsername);
            
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(loginVM.EmailOrUsername);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Email of password is wrong");
                return View();
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your profile is not activated");
                return View(loginVM);
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, loginVM.Password, false,false);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsNotAllowed)
                {
                    ModelState.AddModelError("", "Please confirm your account");
                    return View();
                }
                ModelState.AddModelError("", "Email or Password is wrong");
                return View();
            }

            return RedirectToAction("Index","Home");
        }
        public IActionResult EmailVerification()
        {
            return View();
        }
        public async Task SendEmail(string emailAddress,string url)
        {
            var apiKey = "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";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("Maggotfarhad@gmail.com", "Farhad");
            var subject = "Sending with SendGrid is Fun";
            var to = new EmailAddress(emailAddress, "Example User");
            var plainTextContent = "and easy to do anywhere, even with C#";
            var htmlContent = $"<a href = {url}>Click Here!</a>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
    }
}
