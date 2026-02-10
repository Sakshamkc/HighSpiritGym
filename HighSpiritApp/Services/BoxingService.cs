using ClosedXML.Excel;
using HighSpiritApp.Models.Boxing;
using HighSpiritApp.Repositories.Interfaces;
using HighSpiritApp.Services.Interfaces;

namespace HighSpiritApp.Services
{
    /// <summary>
    /// Boxing member business logic implementation
    /// </summary>
    public class BoxingService : IBoxingService
    {
        private readonly IBoxingRepository _boxingRepository;

        public BoxingService(IBoxingRepository boxingRepository)
        {
            _boxingRepository = boxingRepository;
        }

        public async Task<BoxingMember?> GetByIdAsync(int id)
        {
            return await _boxingRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<BoxingMember>> GetAllAsync()
        {
            return await _boxingRepository.GetAllAsync();
        }

        public async Task<IEnumerable<BoxingMember>> SearchAsync(string? searchTerm)
        {
            return await _boxingRepository.SearchAsync(searchTerm);
        }

        public async Task<IEnumerable<BoxingMember>> GetMembersWithDueAsync()
        {
            return await _boxingRepository.GetMembersWithDueAsync();
        }

        public async Task<BoxingMember> CreateAsync(BoxingMember member)
        {
            // Calculate price from cash + esewa
            member.Price = member.CashAmount + member.EsewaAmount;
            member.CreatedAt = DateTime.Now;

            await _boxingRepository.AddAsync(member);
            await _boxingRepository.SaveChangesAsync();
            return member;
        }

        public async Task UpdateAsync(BoxingMember member)
        {
            var existing = await _boxingRepository.GetByIdAsync(member.BoxingMemberID);
            if (existing == null)
                throw new KeyNotFoundException($"Boxing member with ID {member.BoxingMemberID} not found.");

            existing.Name = member.Name;
            existing.JoinDate = member.JoinDate;
            existing.GuardianName = member.GuardianName;
            existing.GuardianContact = member.GuardianContact;
            existing.PerMonthClass = member.PerMonthClass;
            existing.CashAmount = member.CashAmount;
            existing.EsewaAmount = member.EsewaAmount;
            existing.Price = member.CashAmount + member.EsewaAmount;
            existing.DueAmount = member.DueAmount;
            existing.Remarks = member.Remarks;

            if (member.Photo != null)
                existing.Photo = member.Photo;

            existing.UpdatedAt = DateTime.Now;
            _boxingRepository.Update(existing);
            await _boxingRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var member = await _boxingRepository.GetByIdAsync(id);
            if (member != null)
            {
                _boxingRepository.Remove(member);
                await _boxingRepository.SaveChangesAsync();
            }
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResult { Success = true };

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                foreach (var sheet in workbook.Worksheets)
                {
                    var lastRow = sheet.LastRowUsed();
                    if (lastRow == null) continue;

                    for (int r = 2; r <= lastRow.RowNumber(); r++)
                    {
                        var row = sheet.Row(r);
                        var name = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrEmpty(name)) continue;

                        // Parse join date
                        var joinDateText = row.Cell(2).GetString().Trim()
                            .Replace("st", "").Replace("nd", "")
                            .Replace("rd", "").Replace("th", "");

                        DateTime? joinDate = null;
                        if (DateTime.TryParse(joinDateText, out DateTime parsed))
                            joinDate = parsed;

                        var guardianContact = row.Cell(4).GetString().Trim();

                        // Check if exists
                        var exists = await _boxingRepository.AnyAsync(b =>
                            b.Name == name && b.GuardianContact == guardianContact);

                        if (exists)
                        {
                            result.Skipped++;
                            result.SkippedUsers.Add($"Row {r}: {name} - Already exists");
                            continue;
                        }

                        var member = new BoxingMember
                        {
                            Name = name,
                            JoinDate = joinDate,
                            GuardianName = row.Cell(3).GetString(),
                            GuardianContact = guardianContact,
                            PerMonthClass = string.IsNullOrWhiteSpace(row.Cell(5).GetString())
                                ? "0+0+0+0"
                                : row.Cell(5).GetString(),
                            CashAmount = row.Cell(6).GetValue<int?>() ?? 0,
                            EsewaAmount = row.Cell(7).GetValue<int?>() ?? 0,
                            DueAmount = row.Cell(8).GetValue<int?>() ?? 0,
                            Remarks = row.Cell(9).GetString()
                        };

                        member.Price = member.CashAmount + member.EsewaAmount;
                        member.CreatedAt = DateTime.Now;

                        await _boxingRepository.AddAsync(member);
                        result.Imported++;
                    }
                }

                await _boxingRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<BoxingStats> GetStatsAsync()
        {
            var all = await _boxingRepository.GetAllAsync();
            var members = all.ToList();

            return new BoxingStats
            {
                TotalMembers = members.Count,
                PaidMembers = members.Count(b => b.DueAmount == 0),
                MembersWithDue = members.Count(b => b.DueAmount > 0),
                TotalDueAmount = members.Sum(b => b.DueAmount)
            };
        }
    }
}
