using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Musoq.ContentAggregator.Data;
using Musoq.ContentAggregator.Models;
using Musoq.Service.Client.Core;
using Musoq.Service.Client.Core.Helpers;

namespace Musoq.ContentAggregator.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class QueryController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public QueryController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Delete(QueryModel model)
        {
            var script = new MusoqQueryScript { Query = model.Text, ScriptId = model.ScriptId };

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var queryScript = _context.UserScripts.Single(s => s.ScriptId == script.ScriptId && s.UserId == user.Id);

            _context.UserScripts.Remove(queryScript);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            var userScripts = 
                from allUserScripts in _context.UserScripts
                join musoqQuery in _context.QueryScripts on allUserScripts.ScriptId equals musoqQuery.ScriptId
                where allUserScripts.UserId == user.Id select new QueryModel { ScriptId = musoqQuery.ScriptId, Text = musoqQuery.Query, Name = musoqQuery.ManagingName };

            return View(userScripts);
        }

        public IActionResult Update(Guid scriptId)
        {
            var script = _context.QueryScripts.Single(f => f.ScriptId == scriptId);
            return View(new QueryModel { ScriptId = scriptId, Name = script.ManagingName, Text = script.Query });
        }

        [HttpPost]
        public async Task<IActionResult> Update(QueryModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userScript = _context.UserScripts.Single(s => s.ScriptId == model.ScriptId && s.UserId == user.Id);
            var queryScript = _context.QueryScripts.Single(s => s.ScriptId == userScript.ScriptId);

            queryScript.Query = model.Text;
            queryScript.ManagingName = model.Name;

            _context.QueryScripts.Update(queryScript);

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(QueryModel model)
        {
            var script = new MusoqQueryScript { Query = model.Text, ManagingName = model.Name };

            _context.QueryScripts.Add(script);

            var user = await _userManager.GetUserAsync(HttpContext.User);

            _context.UserScripts.Add(new UserScripts { ScriptId = script.ScriptId, UserId = user.Id });

            await _context.SaveChangesAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Compile(Guid scriptId)
        {
            var script = _context.QueryScripts.Single(f => f.ScriptId == scriptId);

            var api = new ApplicationFlowApi("");

            await api.RunQueryAsync(QueryContext.FromQueryText(script.ScriptId, script.Query));

            return RedirectToAction(nameof(Index));
        }
    }
}