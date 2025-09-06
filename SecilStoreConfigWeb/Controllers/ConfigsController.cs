using Microsoft.AspNetCore.Mvc;
using SecilStoreCodeCase;
using SecilStoreConfigWeb.Models;
using System.Reflection.PortableExecutable;

namespace SecilStoreConfigWeb.Controllers
{
    public class ConfigsController : Controller
    {
        private readonly IConfigStore _store;
        private readonly ConfigurationReader _reader;
        public ConfigsController(IConfigStore store, ConfigurationReader reader)
        {
            _store = store;
            _reader = reader;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string app=null!, CancellationToken ct = default)
        {
            List<string> apps;
            try
            {
                apps = (await _store.GetApplicationsAsync(ct)).ToList();

                // RLS sebebiyle boş gelirse fallback
                if (apps.Count == 0)
                    apps = new() { "SERVICE-A", "SERVICE-B" };
            }
            catch (Exception ex)
            {
                ViewBag.Apps = Array.Empty<string>();
                ViewBag.DbDown = "DB unreachable: " + ex.Message;
                return View(new List<ConfigurationItem>());
            }

            if (string.IsNullOrWhiteSpace(app))
                app = apps.FirstOrDefault() ?? "SERVICE-A";

            ViewBag.Apps = apps;
            ViewBag.App = app;

            var items = (await _store.GetByApplicationAsync(app, ct))
                       .OrderBy(i => i.Id)
                       .ToList();

            try
            {
                var site = _reader.GetValue<string>("SiteName");
                var max = _reader.GetValue<int>("MaxItemCount");
                var flag = _reader.GetValue<bool>("IsFeatureXOpen");
                ViewBag.Demo = $"SiteName={site} | MaxItemCount={max} | IsFeatureXOpen={flag}";
            }
            catch (Exception ex) { ViewBag.Demo = "ERR: " + ex.Message; }

            return View(items);
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ConfigUpsertModel model, CancellationToken ct)
        {
            model.ApplicationName = (model.ApplicationName ?? "").Trim();
            model.Name = (model.Name ?? "").Trim();

            if(string.IsNullOrWhiteSpace(model.ApplicationName) || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Eksik Parametre");
            if(!Enum.TryParse<ConfigItemType>(model.Type,true, out var type ))
                return BadRequest("Geçersiz Veri Tipi");

            var item = new ConfigurationItem
            {
                ApplicationName = model.ApplicationName,
                Name = model.Name,
                Type = type,
                Value = model.Value,
                IsActive = true,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await _store.UpsertAsync(item, ct);
            return RedirectToAction(nameof(Index), new { app = model.ApplicationName });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string applicationName,string name,CancellationToken ct)
        {
           if(string.IsNullOrWhiteSpace(applicationName) || string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Eksik Parametre");
            }
            await _store.DeactivateAsync(applicationName, name, ct);
            return RedirectToAction(nameof(Index), new { app = applicationName });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string applicationName, string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(applicationName) || string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Eksik Parametre");
            }
            await _store.ActivateAsync(applicationName, name, ct);
            return RedirectToAction(nameof(Index), new { app = applicationName });
        }
        [HttpGet]
        public IActionResult Snapshot()
        {
            try
            {
                var site = _reader.GetValue<string>("SiteName");
                var max = _reader.GetValue<int>("MaxItemCount");
                var flag = _reader.GetValue<bool>("IsFeatureXOpen");

                var last = _reader.LastRefreshUtc;

                return Json(new
                {
                    ok = true,
                    site,
                    max,
                    flag,
                    last = last.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}
