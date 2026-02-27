using HighSpiritApp.DataContext;
using HighSpiritApp.Models;
using HighSpiritApp.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ScheduleController : ControllerBase
    {
        private readonly GymDbContext _context;

        public ScheduleController(GymDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET api/schedule
        /// Get all active schedules, optionally filtered by day and/or gender
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? day = null, [FromQuery] string? category = null, [FromQuery] string? gender = null)
        {
            var query = _context.GymSchedules.AsQueryable();

            if (!string.IsNullOrEmpty(day))
                query = query.Where(s => s.DayOfWeek == day);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrEmpty(gender))
                query = query.Where(s => s.Gender.ToLower() == gender.ToLower());

            query = query.Where(s => s.IsActive).OrderBy(s => s.SortOrder);

            var schedules = await query.ToListAsync();
            return Ok(ApiResponse<IEnumerable<ScheduleDto>>.Ok(schedules.Select(MapToDto)));
        }

        /// <summary>
        /// GET api/schedule/{id}
        /// Get schedule by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var schedule = await _context.GymSchedules.FindAsync(id);
            if (schedule == null)
                return NotFound(ApiResponse.Fail("Schedule not found."));

            return Ok(ApiResponse<ScheduleDto>.Ok(MapToDto(schedule)));
        }

        /// <summary>
        /// POST api/schedule
        /// Create a new schedule entry (admin only)
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ScheduleCreateRequest request)
        {
            var schedule = new GymSchedule
            {
                DayOfWeek = request.DayOfWeek,
                Gender = request.Gender,
                ClassName = request.ClassName,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Instructor = request.Instructor,
                Description = request.Description,
                Category = request.Category,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder
            };

            _context.GymSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = schedule.ScheduleID },
                ApiResponse<ScheduleDto>.Ok(MapToDto(schedule), "Schedule created."));
        }

        /// <summary>
        /// PUT api/schedule/{id}
        /// Update schedule (admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleCreateRequest request)
        {
            var schedule = await _context.GymSchedules.FindAsync(id);
            if (schedule == null)
                return NotFound(ApiResponse.Fail("Schedule not found."));

            schedule.DayOfWeek = request.DayOfWeek;
            schedule.Gender = request.Gender;
            schedule.ClassName = request.ClassName;
            schedule.StartTime = request.StartTime;
            schedule.EndTime = request.EndTime;
            schedule.Instructor = request.Instructor;
            schedule.Description = request.Description;
            schedule.Category = request.Category;
            schedule.IsActive = request.IsActive;
            schedule.SortOrder = request.SortOrder;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<ScheduleDto>.Ok(MapToDto(schedule), "Schedule updated."));
        }

        /// <summary>
        /// DELETE api/schedule/{id}
        /// Delete schedule (admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.GymSchedules.FindAsync(id);
            if (schedule == null)
                return NotFound(ApiResponse.Fail("Schedule not found."));

            _context.GymSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse.Ok("Schedule deleted."));
        }

        /// <summary>
        /// GET api/schedule/today?gender=Male
        /// Get today's schedule entries for the given gender
        /// </summary>
        [HttpGet("today")]
        public async Task<IActionResult> GetToday([FromQuery] string? gender = null)
        {
            var query = _context.GymSchedules
                .Where(s => s.IsActive);

            if (!string.IsNullOrEmpty(gender))
                query = query.Where(s => s.Gender.ToLower() == gender.ToLower());

            var schedules = await query
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<ScheduleDto>>.Ok(schedules.Select(MapToDto)));
        }

        private static ScheduleDto MapToDto(GymSchedule s) => new()
        {
            ScheduleID = s.ScheduleID,
            DayOfWeek = s.DayOfWeek,
            Gender = s.Gender,
            ClassName = s.ClassName,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Instructor = s.Instructor,
            Description = s.Description,
            Category = s.Category,
            IsActive = s.IsActive,
            SortOrder = s.SortOrder
        };
    }
}
