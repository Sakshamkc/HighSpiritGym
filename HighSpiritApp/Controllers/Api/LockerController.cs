using HighSpiritApp.Models.Api;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LockerModel = HighSpiritApp.Models.Locker.Locker;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class LockerController : ControllerBase
    {
        private readonly ILockerService _lockerService;

        public LockerController(ILockerService lockerService)
        {
            _lockerService = lockerService;
        }

        /// <summary>
        /// GET api/locker?gender=Gents&search=john&status=Occupied
        /// Get lockers with optional filters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? gender,
            [FromQuery] string? search,
            [FromQuery] string? status)
        {
            IEnumerable<LockerModel> lockers;

            if (!string.IsNullOrEmpty(search))
                lockers = await _lockerService.SearchAsync(search, gender);
            else if (!string.IsNullOrEmpty(gender))
                lockers = await _lockerService.GetByGenderAsync(gender);
            else if (!string.IsNullOrEmpty(status))
                lockers = await _lockerService.GetByStatusAsync(status);
            else
                lockers = await _lockerService.GetAllAsync();

            var dtos = lockers.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<LockerDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/locker/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null)
                return NotFound(ApiResponse.Fail("Locker not found."));

            return Ok(ApiResponse<LockerDto>.Ok(MapToDto(locker)));
        }

        /// <summary>
        /// POST api/locker
        /// Create a single locker
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LockerCreateRequest request)
        {
            var exists = await _lockerService.IsLockerNumberExistsAsync(request.LockerNumber, request.Gender);
            if (exists)
                return BadRequest(ApiResponse.Fail($"Locker number {request.LockerNumber} already exists for {request.Gender}."));

            var locker = new LockerModel
            {
                LockerNumber = request.LockerNumber,
                Gender = request.Gender,
                Remarks = request.Remarks,
                Status = "Empty"
            };

            var created = await _lockerService.CreateAsync(locker);
            return CreatedAtAction(nameof(GetById), new { id = created.LockerID },
                ApiResponse<LockerDto>.Ok(MapToDto(created), "Locker created successfully."));
        }

        /// <summary>
        /// POST api/locker/initialize
        /// Bulk create lockers
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] LockerInitRequest request)
        {
            if (request.Count <= 0 || request.Count > 200)
                return BadRequest(ApiResponse.Fail("Count must be between 1 and 200."));

            await _lockerService.InitializeLockersAsync(request.Gender, request.Count);
            return Ok(ApiResponse.Ok($"Initialized {request.Count} {request.Gender} lockers."));
        }

        /// <summary>
        /// POST api/locker/assign
        /// Assign a locker to a member
        /// </summary>
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] LockerAssignRequest request)
        {
            var locker = await _lockerService.GetByIdAsync(request.LockerID);
            if (locker == null)
                return NotFound(ApiResponse.Fail("Locker not found."));

            await _lockerService.AssignLockerAsync(
                request.LockerID,
                request.MemberName,
                request.Phone,
                request.CustomerID,
                request.Package,
                request.Months,
                request.TotalAmount,
                request.PaidAmount);

            var updated = await _lockerService.GetByIdAsync(request.LockerID);
            return Ok(ApiResponse<LockerDto>.Ok(MapToDto(updated!), "Locker assigned successfully."));
        }

        /// <summary>
        /// POST api/locker/{id}/renew
        /// Renew a locker assignment
        /// </summary>
        [HttpPost("{id}/renew")]
        public async Task<IActionResult> Renew(int id, [FromBody] LockerRenewRequest request)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null)
                return NotFound(ApiResponse.Fail("Locker not found."));

            await _lockerService.RenewLockerAsync(id, request.Months, request.PaidAmount);

            var updated = await _lockerService.GetByIdAsync(id);
            return Ok(ApiResponse<LockerDto>.Ok(MapToDto(updated!), "Locker renewed successfully."));
        }

        /// <summary>
        /// POST api/locker/{id}/release
        /// Release a locker (make it empty)
        /// </summary>
        [HttpPost("{id}/release")]
        public async Task<IActionResult> Release(int id)
        {
            var locker = await _lockerService.GetByIdAsync(id);
            if (locker == null)
                return NotFound(ApiResponse.Fail("Locker not found."));

            await _lockerService.ReleaseLockerAsync(id);
            return Ok(ApiResponse.Ok("Locker released successfully."));
        }

        /// <summary>
        /// DELETE api/locker/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _lockerService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Locker not found."));

            await _lockerService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Locker deleted successfully."));
        }

        /// <summary>
        /// GET api/locker/stats?gender=Gents
        /// Get locker statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] string? gender)
        {
            var stats = await _lockerService.GetStatsAsync(gender);
            return Ok(ApiResponse<LockerStats>.Ok(stats));
        }

        /// <summary>
        /// GET api/locker/expired
        /// Get expired lockers
        /// </summary>
        [HttpGet("expired")]
        public async Task<IActionResult> GetExpired()
        {
            var lockers = await _lockerService.GetExpiredLockersAsync();
            var dtos = lockers.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<LockerDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/locker/expiring-soon?days=7
        /// </summary>
        [HttpGet("expiring-soon")]
        public async Task<IActionResult> GetExpiringSoon([FromQuery] int days = 7)
        {
            var lockers = await _lockerService.GetExpiringSoonLockersAsync(days);
            var dtos = lockers.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<LockerDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/locker/random-empty?gender=Gents
        /// Get a random empty locker for assignment
        /// </summary>
        [HttpGet("random-empty")]
        public async Task<IActionResult> GetRandomEmpty([FromQuery] string gender = "Gents")
        {
            var locker = await _lockerService.GetRandomEmptyLockerAsync(gender);
            if (locker == null)
                return NotFound(ApiResponse.Fail($"No empty {gender} lockers available."));

            return Ok(ApiResponse<LockerDto>.Ok(MapToDto(locker)));
        }

        /// <summary>
        /// GET api/locker/export?gender=Gents&status=Occupied
        /// Export lockers to Excel
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] string gender = "Gents",
            [FromQuery] string? status = null)
        {
            var bytes = await _lockerService.ExportToExcelAsync(gender, status);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Lockers_{gender}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>
        /// POST api/locker/import?gender=Gents
        /// Import lockers from Excel
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file, [FromQuery] string gender = "Gents")
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse.Fail("No file provided."));

            using var stream = file.OpenReadStream();
            var result = await _lockerService.ImportFromExcelAsync(stream, gender);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
                return BadRequest(ApiResponse.Fail(result.ErrorMessage));

            return Ok(ApiResponse<LockerImportResult>.Ok(result,
                $"Imported {result.Imported} lockers, updated {result.Updated}."));
        }

        private static LockerDto MapToDto(LockerModel l) => new()
        {
            LockerID = l.LockerID,
            LockerNumber = l.LockerNumber,
            Gender = l.Gender,
            Status = l.Status,
            CustomerID = l.CustomerID,
            AssignedTo = l.AssignedTo,
            AssignedPhone = l.AssignedPhone,
            Package = l.Package,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            RentalMonths = l.RentalMonths,
            MonthlyRate = l.MonthlyRate,
            TotalAmount = l.TotalAmount,
            PaidAmount = l.PaidAmount,
            DueAmount = l.DueAmount,
            Remarks = l.Remarks,
            IsExpired = l.IsExpired,
            IsExpiringSoon = l.IsExpiringSoon,
            DaysRemaining = l.DaysRemaining
        };
    }
}
