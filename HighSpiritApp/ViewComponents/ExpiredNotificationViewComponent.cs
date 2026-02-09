using HighSpiritApp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HighSpiritApp.ViewComponents
{
    /// <summary>
    /// ViewComponent for displaying expired gym membership notifications
    /// </summary>
    public class ExpiredNotificationViewComponent : ViewComponent
    {
        private readonly IMembershipService _membershipService;

        public ExpiredNotificationViewComponent(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? planName = null)
        {
            var expiredMemberships = (await _membershipService.GetExpiredMembershipsAsync()).ToList();

            // Filter by plan if specified - using same logic as CustomerService
            if (!string.IsNullOrEmpty(planName))
            {
                var planFilter = planName.ToLower();
                expiredMemberships = planFilter switch
                {
                    "custom2" or "custom-2" => expiredMemberships.Where(m => IsCustomPlan(m.PlanName, 2)).ToList(),
                    "custom3" or "custom-3" => expiredMemberships.Where(m => IsCustomPlan(m.PlanName, 3)).ToList(),
                    "gym" => expiredMemberships.Where(m => IsExactPlan(m.PlanName, "Gym")).ToList(),
                    "cardio" => expiredMemberships.Where(m => IsExactPlan(m.PlanName, "Cardio")).ToList(),
                    "premium" => expiredMemberships.Where(m => m.PlanName != null && m.PlanName.Contains("Premium", StringComparison.OrdinalIgnoreCase)).ToList(),
                    "zumba" => expiredMemberships.Where(m => IsExactPlanMultiple(m.PlanName, new[] { "Zumba", "Aerobics" })).ToList(),
                    "sauna" => expiredMemberships.Where(m => IsExactPlanMultiple(m.PlanName, new[] { "Sauna", "Steam" })).ToList(),
                    _ => expiredMemberships.Where(m => m.PlanName != null && m.PlanName.Contains(planName, StringComparison.OrdinalIgnoreCase)).ToList()
                };
            }

            ViewBag.ExpiredCount = expiredMemberships.Count;
            ViewBag.CurrentPlan = planName;

            return View(expiredMemberships.Take(5).ToList());
        }

        // Helper: Check if plan contains keyword but is NOT a customized package
        private bool IsExactPlan(string? planName, string planKeyword)
        {
            if (string.IsNullOrEmpty(planName)) return false;

            bool containsKeyword = planName.Contains(planKeyword, StringComparison.OrdinalIgnoreCase);
            bool isCustomized = planName.Contains("Customized", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Custom", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(2)", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(3)", StringComparison.OrdinalIgnoreCase);

            return containsKeyword && !isCustomized;
        }

        // Helper: Check if plan contains any of the keywords but is NOT a customized package
        private bool IsExactPlanMultiple(string? planName, string[] keywords)
        {
            if (string.IsNullOrEmpty(planName)) return false;

            bool containsKeyword = keywords.Any(k => planName.Contains(k, StringComparison.OrdinalIgnoreCase));
            bool isCustomized = planName.Contains("Customized", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Custom", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(2)", StringComparison.OrdinalIgnoreCase) ||
                               planName.Contains("(3)", StringComparison.OrdinalIgnoreCase);

            return containsKeyword && !isCustomized;
        }

        // Helper: Check if plan is customized package (2 or 3 facilities)
        private bool IsCustomPlan(string? planName, int count)
        {
            if (string.IsNullOrEmpty(planName)) return false;

            return count switch
            {
                2 => planName.Contains("Two", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("(2)", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Custom 2", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Custom-2", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Customized 2", StringComparison.OrdinalIgnoreCase),
                3 => planName.Contains("Three", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("(3)", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Custom 3", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Custom-3", StringComparison.OrdinalIgnoreCase) ||
                     planName.Contains("Customized 3", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
