using DeltaPLC.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeltaPLC.Controllers
{
    public class PlcController : Controller
    {
        private readonly PlcService _plcService;

        public PlcController(PlcService plcService)
        {
            _plcService = plcService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Connect([FromBody] string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                return BadRequest(new { message = "❌ PortName cannot be null or empty." });
            }

            string result = _plcService.Connect(portName);

            if (result.Contains("✅"))
            {
                return Ok(new { message = result });
            }
            else
            {
                return StatusCode(500, new { message = result });
            }
        }

        [HttpPost]
        public IActionResult Disconnect()
        {
            try
            {
                string message = _plcService.Disconnect();
                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReadRegister(int register)
        {
            try
            {
                int value = await _plcService.ReadRegisterAsync(register);
                return Json(new { success = true, register, value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> WriteRegister(int register, int value)
        {
            if (register <= 0 || value <= 0)
            {
                return Json(new { success = false, message = "Invalid input data." });
            }

            try
            {
                string message = await _plcService.WriteRegisterAsync(register, value);
                return Json(new { success = true, register, value, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}

