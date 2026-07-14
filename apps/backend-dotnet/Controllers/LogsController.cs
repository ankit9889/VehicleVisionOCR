using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VehicleVisionOCR.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly string _logPath;

        public LogsController()
        {
            _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        }

        [HttpGet]
        public IActionResult GetRecentLogs()
        {
            if (!Directory.Exists(_logPath)) return Ok(new List<string>());

            var files = Directory.GetFiles(_logPath, "*.txt").OrderByDescending(f => f).ToList();
            if (files.Count == 0) return Ok(new List<string>());

            // Read the most recent log file
            var latestLogFile = files.First();
            var logs = new List<string>();

            // Read safely if file is locked
            using (var fs = new FileStream(latestLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    logs.Add(line);
                }
            }

            // Return last 1000 lines
            return Ok(logs.TakeLast(1000));
        }
    }
}
