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
    public class EditorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _services;

        public EditorController(
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
        
        public IActionResult Index(QueryModel model)
        {
            if (model == null)
                return View(new QueryModel());

            return View(model);
        }

        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Save(QueryModel model)
        {
            if (model == null)
                return StatusCode(500);

            if (model.ScriptId == Guid.Empty)
                return await Create(model);
            else
                return await Update(model);
        }

        public IActionResult Update(Guid scriptId)
        {
            var script = (from queryScript in _context.QueryScripts
                          join userScript in _context.UserScripts on queryScript.ScriptId equals userScript.ScriptId
                          where queryScript.ScriptId == scriptId
                          select new { QueryScript = queryScript, UserScript = userScript }).Single();

            return RedirectToAction(nameof(Index), new QueryModel { ScriptId = scriptId, Name = script.QueryScript.ManagingName, Text = script.QueryScript.Query, ShowAt = script.UserScript.ShowAt });
        }

        public IActionResult HasBatch(Guid batchId)
        {
            var hasBatch = _context.Tables.Any(f => f.BatchId == batchId);

            return Json(new { HasBatch = hasBatch });
        }

        public IActionResult Table(Guid batchId)
        {
            var table = _context.Tables.Single(t => t.BatchId == batchId);

            return Json(JsonConvert.DeserializeObject<ResultTable>(table.Json));
        }

        public IActionResult Compile(Guid scriptId)
        {
            _queue.QueueBackgroundWorkItem(async token => {
                using (var service = _services.CreateScope())
                {
                    var context = service.ServiceProvider.GetService<ApplicationDbContext>();
                    var api = new ApplicationFlowApi("127.0.0.1:9001");
                    var script = context.QueryScripts.Single(f => f.ScriptId == scriptId);
                    await api.Compile(QueryContext.FromQueryText(script.ScriptId, script.Query));
                }
            });

            return Ok();
        }

        public IActionResult Run(Guid scriptId)
        {
            var batchId = Guid.NewGuid();

            _queue.QueueBackgroundWorkItem(async token => {
                using (var service = _services.CreateScope())
                {
                    var context = service.ServiceProvider.GetService<ApplicationDbContext>();
                    var api = new ApplicationFlowApi("127.0.0.1:9001");
                    var script = context.QueryScripts.Single(f => f.ScriptId == scriptId);
                    var tableResult = await api.RunQueryAsync(QueryContext.FromQueryText(script.ScriptId, script.Query));

                    var table = new Table
                    {
                        ScriptId = scriptId,
                        Json = JsonConvert.SerializeObject(tableResult),
                        InsertedAt = DateTimeOffset.UtcNow,
                        BatchId = batchId
                    };

                    context.Tables.Add(table);

                    try
                    {
                        context.SaveChanges();
                    }
                    catch(Exception ex)
                    {

                    }
                }
            });

            return Json(new { BatchId = batchId });
        }

        private async Task<IActionResult> Create(QueryModel model)
        {
            var script = new MusoqQueryScript { Query = model.Text, ManagingName = model.Name };

            _context.QueryScripts.Add(script);

            var user = await _userManager.GetUserAsync(HttpContext.User);

            _context.UserScripts.Add(new UserScripts { ScriptId = script.ScriptId, UserId = user.Id, ShowAt = model.ShowAt, RefreshedAt = DateTimeOffset.UtcNow });

            await _context.SaveChangesAsync();

            return Ok();
        }
        
        private async Task<IActionResult> Update(QueryModel model)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userScript = _context.UserScripts.Single(s => s.ScriptId == model.ScriptId && s.UserId == user.Id);
            var queryScript = _context.QueryScripts.Single(s => s.ScriptId == userScript.ScriptId);

            userScript.RefreshedAt = DateTimeOffset.UtcNow;
            userScript.ShowAt = model.ShowAt;

            queryScript.Query = model.Text;
            queryScript.ManagingName = model.Name;

            _context.UserScripts.Update(userScript);
            _context.QueryScripts.Update(queryScript);

            _context.SaveChanges();

            return Ok();
        }
    }
}