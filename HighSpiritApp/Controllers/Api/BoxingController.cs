using HighSpiritApp.Models.Api;
using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class BoxingController : ControllerBase
    {
        private readonly IBoxingService _boxingService;

        public BoxingController(IBoxingService boxingService)
        {
            _boxingService = boxingService;
        }

        /// <summary>
        /// GET api/boxing
        /// Get all boxing members (optionally filter by category/search)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? category)
        {
            IEnumerable<BoxingMember> members;

            if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(search))
                members = await _boxingService.SearchByCategoryAsync(search, category);
            else if (!string.IsNullOrEmpty(category))
                members = await _boxingService.GetByCategoryAsync(category);
            else if (!string.IsNullOrEmpty(search))
                members = await _boxingService.SearchAsync(search);
            else
                members = await _boxingService.GetAllAsync();

            var dtos = members.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<BoxingMemberDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/boxing/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member == null)
                return NotFound(ApiResponse.Fail("Boxing member not found."));

            return Ok(ApiResponse<BoxingMemberDto>.Ok(MapToDto(member)));
        }

        /// <summary>
        /// POST api/boxing
        /// Create a new boxing member
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BoxingMemberCreateRequest request)
        {
            var member = new BoxingMember
            {
                Name = request.Name,
                JoinDate = request.JoinDate,
                GuardianName = request.GuardianName,
                GuardianContact = request.GuardianContact,
                PerMonthClass = request.PerMonthClass,
                CashAmount = request.CashAmount,
                EsewaAmount = request.EsewaAmount,
                DueAmount = request.DueAmount,
                Price = request.Price,
                Remarks = request.Remarks,
                ExpireDate = request.ExpireDate,
                Category = request.Category
            };

            if (!string.IsNullOrEmpty(request.PhotoBase64))
            {
                try { member.Photo = Convert.FromBase64String(request.PhotoBase64); }
                catch { return BadRequest(ApiResponse.Fail("Invalid photo format.")); }
            }

            var created = await _boxingService.CreateAsync(member);
            return CreatedAtAction(nameof(GetById), new { id = created.BoxingMemberID },
                ApiResponse<BoxingMemberDto>.Ok(MapToDto(created), "Boxing member created successfully."));
        }

        /// <summary>
        /// PUT api/boxing/{id}
        /// Update a boxing member
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BoxingMemberUpdateRequest request)
        {
            var existing = await _boxingService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Boxing member not found."));

            existing.Name = request.Name;
            existing.JoinDate = request.JoinDate;
            existing.GuardianName = request.GuardianName;
            existing.GuardianContact = request.GuardianContact;
            existing.PerMonthClass = request.PerMonthClass;
            existing.CashAmount = request.CashAmount;
            existing.EsewaAmount = request.EsewaAmount;
            existing.DueAmount = request.DueAmount;
            existing.Price = request.Price;
            existing.Remarks = request.Remarks;
            existing.ExpireDate = request.ExpireDate;
            existing.Category = request.Category;

            if (!string.IsNullOrEmpty(request.PhotoBase64))
            {
                try { existing.Photo = Convert.FromBase64String(request.PhotoBase64); }
                catch { return BadRequest(ApiResponse.Fail("Invalid photo format.")); }
            }

            await _boxingService.UpdateAsync(existing);
            return Ok(ApiResponse<BoxingMemberDto>.Ok(MapToDto(existing), "Boxing member updated successfully."));
        }

        /// <summary>
        /// DELETE api/boxing/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _boxingService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Boxing member not found."));

            await _boxingService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Boxing member deleted successfully."));
        }

        /// <summary>
        /// GET api/boxing/stats?category=Adult
        /// Get boxing statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] string? category)
        {
            BoxingStats stats;
            if (!string.IsNullOrEmpty(category))
                stats = await _boxingService.GetStatsByCategoryAsync(category);
            else
                stats = await _boxingService.GetStatsAsync();

            return Ok(ApiResponse<BoxingStats>.Ok(stats));
        }

        /// <summary>
        /// GET api/boxing/with-due
        /// Get boxing members with outstanding due amounts
        /// </summary>
        [HttpGet("with-due")]
        public async Task<IActionResult> GetWithDue()
        {
            var members = await _boxingService.GetMembersWithDueAsync();
            var dtos = members.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<BoxingMemberDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/boxing/{id}/photo
        /// </summary>
        [HttpGet("{id}/photo")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var member = await _boxingService.GetByIdAsync(id);
            if (member?.Photo == null || member.Photo.Length == 0)
                return NotFound(ApiResponse.Fail("No photo available."));

            return File(member.Photo, "image/jpeg");
        }

        /// <summary>
        /// POST api/boxing/import?category=Children
        /// Import boxing members from Excel
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file, [FromQuery] string category = "Children")
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse.Fail("No file provided."));

            using var stream = file.OpenReadStream();
            var result = await _boxingService.ImportFromExcelAsync(stream, category);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "Import failed."));

            return Ok(ApiResponse<ImportResult>.Ok(result, $"Imported {result.Imported} boxing members."));
        }

        private static BoxingMemberDto MapToDto(BoxingMember m) => new()
        {
            BoxingMemberID = m.BoxingMemberID,
            Name = m.Name,
            JoinDate = m.JoinDate,
            GuardianName = m.GuardianName,
            GuardianContact = m.GuardianContact,
            PerMonthClass = m.PerMonthClass,
            CashAmount = m.CashAmount,
            EsewaAmount = m.EsewaAmount,
            DueAmount = m.DueAmount,
            Price = m.Price,
            Remarks = m.Remarks,
            ExpireDate = m.ExpireDate,
            PhotoBase64 = m.Photo != null ? Convert.ToBase64String(m.Photo) : null,
            Category = m.Category,
            CreatedAt = m.CreatedAt
        };
    }
}
