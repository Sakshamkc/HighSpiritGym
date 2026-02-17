using HighSpiritApp.Models;
using HighSpiritApp.Models.Api;
using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IMembershipService _membershipService;

        public CustomersController(
            ICustomerService customerService,
            IMembershipService membershipService)
        {
            _customerService = customerService;
            _membershipService = membershipService;
        }

        /// <summary>
        /// GET api/customers
        /// Get paginated, filtered list of customers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? filter,
            [FromQuery] string? sort,
            [FromQuery] int? duration,
            [FromQuery] string? planName,
            [FromQuery] string? shift,
            [FromQuery] string? gender,
            [FromQuery] string? paymentStatus,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _customerService.GetFilteredCustomersAsync(new CustomerFilterRequest
            {
                Search = search,
                Filter = filter,
                Sort = sort,
                Duration = duration,
                PlanName = planName,
                Shift = shift,
                Gender = gender,
                PaymentStatus = paymentStatus,
                Page = page,
                PageSize = pageSize
            });

            var dtos = result.Customers.Select(MapToDto);

            return Ok(new PaginatedResponse<CustomerDto>
            {
                Data = dtos,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                CurrentPage = result.CurrentPage,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// GET api/customers/{id}
        /// Get customer by ID with membership details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdWithMembershipsAsync(id);
            if (customer == null)
                return NotFound(ApiResponse.Fail("Customer not found."));

            return Ok(ApiResponse<CustomerDto>.Ok(MapToDto(customer)));
        }

        /// <summary>
        /// POST api/customers
        /// Create a new customer with initial membership
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerCreateRequest request)
        {
            var customer = new Customer
            {
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Gender = request.Gender,
                BloodGroup = request.BloodGroup,
                WeightKG = request.WeightKG,
                Height = request.Height,
                Occupation = request.Occupation,
                JoinDate = request.JoinDate,
                DateOfBirth = request.DateOfBirth,
                Remarks = request.Remarks,
                Shift = request.Shift
            };

            byte[]? photo = null;
            if (!string.IsNullOrEmpty(request.PhotoBase64))
            {
                try { photo = Convert.FromBase64String(request.PhotoBase64); }
                catch { return BadRequest(ApiResponse.Fail("Invalid photo format. Use Base64 encoded string.")); }
            }

            var created = await _customerService.CreateAsync(customer, photo);

            // Create initial membership if provided
            if (!string.IsNullOrEmpty(request.PlanName) && request.Duration > 0)
            {
                var membership = new CustomerMembership
                {
                    CustomerID = created.CustomerID,
                    PlanName = request.PlanName,
                    PaidPrice = request.PaidPrice,
                    DueAmount = request.DueAmount,
                    StartDate = request.StartDate,
                    Duration = request.Duration,
                    ExpireDate = request.StartDate.AddMonths(request.Duration),
                    IsActive = true
                };
                await _membershipService.CreateAsync(membership);
            }

            var result = await _customerService.GetByIdWithMembershipsAsync(created.CustomerID);
            return CreatedAtAction(nameof(GetById), new { id = created.CustomerID },
                ApiResponse<CustomerDto>.Ok(MapToDto(result!), "Customer created successfully."));
        }

        /// <summary>
        /// PUT api/customers/{id}
        /// Update customer and optionally membership
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateRequest request)
        {
            var existing = await _customerService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Customer not found."));

            byte[]? photo = null;
            if (!string.IsNullOrEmpty(request.PhotoBase64))
            {
                try { photo = Convert.FromBase64String(request.PhotoBase64); }
                catch { return BadRequest(ApiResponse.Fail("Invalid photo format.")); }
            }

            var vm = new CustomerEditVM
            {
                CustomerID = id,
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Height = request.Height,
                WeightKG = request.WeightKG,
                BloodGroup = request.BloodGroup,
                Occupation = request.Occupation,
                Remarks = request.Remarks,
                Shift = request.Shift,
                MembershipID = request.MembershipID,
                PlanName = request.PlanName,
                PaidPrice = request.PaidPrice,
                DueAmount = request.DueAmount,
                StartDate = request.StartDate ?? DateTime.Today,
                ExpireDate = request.ExpireDate
            };

            await _customerService.UpdateAsync(vm, photo);

            var updated = await _customerService.GetByIdWithMembershipsAsync(id);
            return Ok(ApiResponse<CustomerDto>.Ok(MapToDto(updated!), "Customer updated successfully."));
        }

        /// <summary>
        /// DELETE api/customers/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _customerService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Customer not found."));

            await _customerService.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Customer deleted successfully."));
        }

        /// <summary>
        /// GET api/customers/{id}/photo
        /// Get customer photo as image
        /// </summary>
        [HttpGet("{id}/photo")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer?.Photo == null || customer.Photo.Length == 0)
                return NotFound(ApiResponse.Fail("No photo available."));

            return File(customer.Photo, "image/jpeg");
        }

        /// <summary>
        /// GET api/customers/export
        /// Export customers to Excel
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] string? search,
            [FromQuery] string? filter,
            [FromQuery] string? planName,
            [FromQuery] string? gender)
        {
            var bytes = await _customerService.ExportToExcelAsync(new CustomerFilterRequest
            {
                Search = search,
                Filter = filter,
                PlanName = planName,
                Gender = gender,
                PageSize = int.MaxValue
            });

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Customers_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        /// <summary>
        /// POST api/customers/import
        /// Import customers from Excel file
        /// </summary>
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse.Fail("No file provided."));

            using var stream = file.OpenReadStream();
            var result = await _customerService.ImportFromExcelAsync(stream);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.ErrorMessage ?? "Import failed."));

            return Ok(ApiResponse<ImportResult>.Ok(result, $"Imported {result.Imported} customers."));
        }

        // ===== Helper: Map Customer entity to DTO =====
        private static CustomerDto MapToDto(Customer customer)
        {
            // Get the latest membership that is either flagged active OR not yet expired
            var activeMembership = customer.Memberships?
                .Where(m => m.IsActive || m.ExpireDate >= DateTime.Today)
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefault();

            var latestMembership = activeMembership ?? customer.Memberships?
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefault();

            var membershipToShow = activeMembership ?? latestMembership;
            var isExpired = membershipToShow != null && membershipToShow.ExpireDate < DateTime.Today;

            return new CustomerDto
            {
                CustomerID = customer.CustomerID,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                Gender = customer.Gender,
                BloodGroup = customer.BloodGroup,
                WeightKG = customer.WeightKG,
                Height = customer.Height,
                Occupation = customer.Occupation,
                JoinDate = customer.JoinDate,
                DateOfBirth = customer.DateOfBirth,
                PhotoBase64 = customer.Photo != null ? Convert.ToBase64String(customer.Photo) : null,
                Remarks = customer.Remarks,
                Shift = customer.Shift,
                CreatedAt = customer.CreatedAt,
                CurrentPlan = membershipToShow?.PlanName,
                MembershipStart = membershipToShow?.StartDate,
                MembershipExpire = membershipToShow?.ExpireDate,
                PaidPrice = membershipToShow?.PaidPrice,
                DueAmount = membershipToShow?.DueAmount,
                IsActive = membershipToShow != null && !isExpired,
                IsExpired = isExpired
            };
        }
    }
}
