using MemoryMappedFileDemo.MemoryMappedUtils;
using MemoryMappedFileDemo.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace MemoryMappedFileDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MemoryMap<FileStatus> _memoryMap;

        public static FileStatus _fileStatus { get; set; } = new FileStatus();

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment)
        {
            var dataFolder = Path.Combine(webHostEnvironment.WebRootPath, "data");

            _logger = logger;
            _memoryMap = new MemoryMap<FileStatus>(memoryMapName: "demo", directoryPath: dataFolder, fileName: "info.json");

            _fileStatus = _memoryMap.ReadData();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _memoryMap?.Dispose();
        }

        public IActionResult Index()
        {
            ViewBag.ProcessID = Process.GetCurrentProcess().Id;
            ViewBag.LoadedFromFile = _memoryMap.LoadedFromFile;

            var model = new FileStatus()
            {
                FirstFileChanged = _fileStatus.FirstFileChanged,
                SecondFileChanged = _fileStatus.SecondFileChanged,
                ThirdFileChanged = _fileStatus.ThirdFileChanged,
                FourthFileChanged = _fileStatus.FourthFileChanged,
                FifthFileChanged = _fileStatus.FifthFileChanged
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(FileStatusModel fileStatusModel)
        {
            // Find What values were provided from UI and map the updates to the static variable and write in Memory Mapped File as well

            var properties = fileStatusModel.GetType().GetProperties();
            foreach(var property in properties)
            {
                if (property.GetValue(fileStatusModel) != null)
                {
                    var value = int.Parse((property.GetValue(fileStatusModel).ToString()));
                    _fileStatus.GetType().GetProperty(property.Name).SetValue(_fileStatus, value);
                }
            }

            _memoryMap.WriteData(_fileStatus);

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
