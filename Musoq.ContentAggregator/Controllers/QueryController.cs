using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Musoq.ContentAggregator.Data;
using Musoq.ContentAggregator.Models;
using Musoq.ContentAggregator.Services;
using Musoq.Service.Client.Core;
using Musoq.Service.Client.Core.Helpers;
using Newtonsoft.Json;

namespace Musoq.ContentAggregator.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class QueryController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _services;

        public QueryController(
          UserManager<ApplicationUser> userManager,
          ApplicationDbContext context, 
          IBackgroundTaskQueue queue,
          IServiceScopeFactory serviceScopeFactory)
        {
            _userManager = userManager;
            _context = context;
            _queue = queue;
            _services = serviceScopeFactory;
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

        public IActionResult Create()
        {
            return RedirectToAction(nameof(EditorController.Create), "Editor");
        }

        public IActionResult Update(Guid scriptId)
        {
            return RedirectToAction(nameof(EditorController.Update), "Editor", new { ScriptId = scriptId });
        }
    }
}