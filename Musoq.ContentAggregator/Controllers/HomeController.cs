using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Musoq.ContentAggregator.Data;
using Musoq.ContentAggregator.Models;
using Musoq.Service.Client.Core;
using Newtonsoft.Json;

namespace Musoq.ContentAggregator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IQueryable<UserScripts> scripts;

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                scripts = _context.UserScripts.Where(script => script.ShowAt == ShowAt.MainPage || (script.ShowAt == ShowAt.UserMainPage && script.UserId == user.Id));
            }
            else
                scripts = _context.UserScripts.Where(script => script.ShowAt == ShowAt.MainPage);

            var result = (from table in _context.Tables
                          join script in scripts on table.TableId equals script.ScriptId
                          join musoqScript in _context.QueryScripts on script.ScriptId equals musoqScript.ScriptId
                          select new ResultTableMessageModel() { RefreshedAt = script.RefreshedAt, Result = JsonConvert.DeserializeObject<ResultTable>(table.Json) }).OrderByDescending(f => f.RefreshedAt);

            return View(result.ToList());
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
