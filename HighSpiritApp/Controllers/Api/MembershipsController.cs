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
    public class MembershipsController : ControllerBase
    {
        private readonly IMembershipService _membershipService;

        public MembershipsController(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        /// <summary>
        /// GET api/memberships/{id}
        /// Get membership by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var membership = await _membershipService.GetByIdAsync(id);
            if (membership == null)
                return NotFound(ApiResponse.Fail("Membership not found."));

            return Ok(ApiResponse<MembershipDto>.Ok(MapToDto(membership)));
        }

        /// <summary>
        /// GET api/memberships/customer/{customerId}
        /// Get all memberships for a customer
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            var memberships = await _membershipService.GetByCustomerIdAsync(customerId);
            var dtos = memberships.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<MembershipDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/memberships/customer/{customerId}/latest
        /// Get latest membership for a customer
        /// </summary>
        [HttpGet("customer/{customerId}/latest")]
        public async Task<IActionResult> GetLatest(int customerId)
        {
            var membership = await _membershipService.GetLatestByCustomerIdAsync(customerId);
            if (membership == null)
                return NotFound(ApiResponse.Fail("No membership found for this customer."));

            return Ok(ApiResponse<MembershipDto>.Ok(MapToDto(membership)));
        }

        /// <summary>
        /// GET api/memberships/expired
        /// Get all expired memberships
        /// </summary>
        [HttpGet("expired")]
        public async Task<IActionResult> GetExpired()
        {
            var memberships = await _membershipService.GetExpiredMembershipsAsync();
            var dtos = memberships.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<MembershipDto>>.Ok(dtos));
        }

        /// <summary>
        /// GET api/memberships/expiring-soon?days=7
        /// Get memberships expiring soon
        /// </summary>
        [HttpGet("expiring-soon")]
        public async Task<IActionResult> GetExpiringSoon([FromQuery] int days = 7)
        {
            var memberships = await _membershipService.GetExpiringSoonAsync(days);
            var dtos = memberships.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<MembershipDto>>.Ok(dtos));
        }

        /// <summary>
        /// POST api/memberships/renew
        /// Renew a customer's membership
        /// </summary>
        [HttpPost("renew")]
        public async Task<IActionResult> Renew([FromBody] MembershipRenewRequest request)
        {
            var membership = new CustomerMembership
            {
                CustomerID = request.CustomerID,
                PlanName = request.PlanName,
                PaidPrice = request.PaidPrice,
                DueAmount = request.DueAmount,
                StartDate = request.StartDate,
                Duration = request.Duration,
                ExpireDate = request.StartDate.AddMonths(request.Duration),
                IsActive = true
            };

            await _membershipService.RenewAsync(membership);

            var latest = await _membershipService.GetLatestByCustomerIdAsync(request.CustomerID);
            return Ok(ApiResponse<MembershipDto>.Ok(MapToDto(latest!), "Membership renewed successfully."));
        }

        /// <summary>
        /// PUT api/memberships/{id}
        /// Update an existing membership
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] MembershipUpdateRequest request)
        {
            var existing = await _membershipService.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse.Fail("Membership not found."));

            await _membershipService.UpdateAsync(id, request.PlanName, request.PaidPrice,
                request.StartDate, request.ExpireDate);

            var updated = await _membershipService.GetByIdAsync(id);
            return Ok(ApiResponse<MembershipDto>.Ok(MapToDto(updated!), "Membership updated successfully."));
        }

        /// <summary>
        /// GET api/memberships/renewal-info/{customerId}
        /// Get renewal info (suggested start date, last plan, etc.)
        /// </summary>
        [HttpGet("renewal-info/{customerId}")]
        public async Task<IActionResult> GetRenewalInfo(int customerId)
        {
            var info = await _membershipService.GetRenewalInfoAsync(customerId);
            return Ok(ApiResponse<RenewalInfo>.Ok(info));
        }

        private static MembershipDto MapToDto(CustomerMembership m) => new()
        {
            MembershipID = m.MembershipID,
            CustomerID = m.CustomerID,
            CustomerName = m.Customer?.FullName,
            PlanName = m.PlanName,
            PaidPrice = m.PaidPrice,
            DueAmount = m.DueAmount,
            TotalPrice = m.TotalPrice,
            StartDate = m.StartDate,
            Duration = m.Duration,
            ExpireDate = m.ExpireDate,
            IsActive = m.IsActive
        };
    }
}
