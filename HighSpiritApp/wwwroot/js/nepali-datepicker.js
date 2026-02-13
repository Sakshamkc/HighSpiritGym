/**
 * Nepali Date Picker with BS ↔ AD Conversion
 * For High Spirit Gym Management System
 * Supports toggle between English (AD/Flatpickr) and Nepali (BS) date picker
 */

(function () {
    'use strict';

    // ============================================================
    // BS Calendar Data: Days in each month for BS years 2000-2090
    // Reference: 1 Baisakh 2000 BS = 13 April 1943 AD
    // ============================================================
    const BS_CALENDAR_DATA = {
        2000: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2001: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2002: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2003: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2004: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2005: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2006: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2007: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2008: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31],
        2009: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2010: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2011: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2012: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2013: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2014: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2015: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2016: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2017: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2018: [31, 32, 31, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2019: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2020: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2021: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2022: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2023: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2024: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2025: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2026: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2027: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2028: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2029: [31, 31, 32, 31, 32, 30, 30, 29, 30, 29, 30, 30],
        2030: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2031: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2032: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2033: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2034: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2035: [30, 32, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31],
        2036: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2037: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2038: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2039: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2040: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2041: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2042: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2043: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2044: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2045: [31, 32, 31, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2046: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2047: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2048: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2049: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2050: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2051: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2052: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2053: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2054: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2055: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2056: [31, 31, 32, 31, 32, 30, 30, 29, 30, 29, 30, 30],
        2057: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2058: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2059: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2060: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2061: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2062: [30, 32, 31, 32, 31, 31, 29, 30, 29, 30, 29, 31],
        2063: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2064: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2065: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2066: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 29, 31],
        2067: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2068: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2069: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2070: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2071: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2072: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2073: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2074: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2075: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2076: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2077: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2078: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2079: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2080: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2081: [31, 31, 32, 32, 31, 30, 30, 30, 29, 30, 30, 30],
        2082: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30],
        2083: [31, 31, 32, 31, 31, 30, 30, 30, 29, 30, 30, 30],
        2084: [31, 31, 32, 31, 31, 30, 30, 30, 29, 30, 30, 30],
        2085: [31, 32, 31, 32, 30, 31, 30, 30, 29, 30, 30, 30],
        2086: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30],
        2087: [31, 31, 32, 31, 31, 31, 30, 30, 29, 30, 30, 30],
        2088: [30, 31, 32, 32, 30, 31, 30, 30, 29, 30, 30, 30],
        2089: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30],
        2090: [30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 30, 30],
        2091: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2092: [31, 31, 32, 32, 31, 30, 30, 29, 30, 29, 30, 30],
        2093: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31],
        2094: [31, 31, 31, 32, 31, 31, 29, 30, 30, 29, 30, 30],
        2095: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2096: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 30],
        2097: [31, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31],
        2098: [31, 31, 31, 32, 31, 31, 30, 29, 30, 29, 30, 30],
        2099: [31, 31, 32, 31, 31, 31, 30, 29, 30, 29, 30, 30],
        2100: [31, 32, 31, 32, 31, 30, 30, 30, 29, 29, 30, 31]
    };

    // Reference point: 1 Baisakh 2000 BS = 13 April 1943 AD
    const BS_REF = { year: 2000, month: 1, day: 1 };
    const AD_REF = new Date(1943, 3, 13); // April 13, 1943

    // Nepali month names
    const BS_MONTHS = [
        'बैशाख', 'जेठ', 'असार', 'श्रावण', 'भदौ', 'असोज',
        'कार्तिक', 'मंसिर', 'पौष', 'माघ', 'फाल्गुन', 'चैत्र'
    ];

    const BS_MONTHS_EN = [
        'Baisakh', 'Jestha', 'Ashar', 'Shrawan', 'Bhadra', 'Ashoj',
        'Kartik', 'Mangsir', 'Poush', 'Magh', 'Falgun', 'Chaitra'
    ];

    // Nepali day names (short)
    const BS_DAYS_SHORT = ['आ', 'सो', 'मं', 'बु', 'बि', 'शु', 'श'];

    // Nepali digits
    const NP_DIGITS = ['०', '१', '२', '३', '४', '५', '६', '७', '८', '९'];

    // ============================================================
    // Conversion Functions
    // ============================================================

    function toNepaliDigits(num) {
        return String(num).split('').map(d => NP_DIGITS[parseInt(d)] || d).join('');
    }

    function getTotalDaysInBSYear(year) {
        if (!BS_CALENDAR_DATA[year]) return 365;
        return BS_CALENDAR_DATA[year].reduce((sum, d) => sum + d, 0);
    }

    function getDaysInBSMonth(year, month) {
        if (!BS_CALENDAR_DATA[year]) return 30;
        return BS_CALENDAR_DATA[year][month - 1];
    }

    /**
     * Convert AD (Gregorian) date to BS (Bikram Sambat) date
     * @param {number} adYear
     * @param {number} adMonth (1-12)
     * @param {number} adDay
     * @returns {{ year: number, month: number, day: number }}
     */
    function adToBs(adYear, adMonth, adDay) {
        const adDate = new Date(adYear, adMonth - 1, adDay);
        let diffDays = Math.floor((adDate - AD_REF) / (1000 * 60 * 60 * 24));

        if (diffDays < 0) return null;

        let bsYear = BS_REF.year;
        let bsMonth = BS_REF.month;
        let bsDay = BS_REF.day;

        while (diffDays > 0) {
            const daysInMonth = getDaysInBSMonth(bsYear, bsMonth);
            const daysRemaining = daysInMonth - bsDay;

            if (diffDays <= daysRemaining) {
                bsDay += diffDays;
                diffDays = 0;
            } else {
                diffDays -= (daysRemaining + 1);
                bsMonth++;
                if (bsMonth > 12) {
                    bsMonth = 1;
                    bsYear++;
                }
                bsDay = 1;
            }
        }

        return { year: bsYear, month: bsMonth, day: bsDay };
    }

    /**
     * Convert BS (Bikram Sambat) date to AD (Gregorian) date
     * @param {number} bsYear
     * @param {number} bsMonth (1-12)
     * @param {number} bsDay
     * @returns {Date}
     */
    function bsToAd(bsYear, bsMonth, bsDay) {
        let totalDays = 0;

        // Count days from reference BS year/month/day to target
        let year = BS_REF.year;
        let month = BS_REF.month;

        // Add days for years
        while (year < bsYear) {
            totalDays += getTotalDaysInBSYear(year);
            year++;
        }

        // Add days for months in the target year
        month = 1;
        while (month < bsMonth) {
            totalDays += getDaysInBSMonth(bsYear, month);
            month++;
        }

        // Add remaining days
        totalDays += (bsDay - 1);

        const result = new Date(AD_REF);
        result.setDate(result.getDate() + totalDays);
        return result;
    }

    /**
     * Format BS date as string
     */
    function formatBsDate(bsDate, useNepaliDigits = true) {
        const d = String(bsDate.day).padStart(2, '0');
        const m = BS_MONTHS_EN[bsDate.month - 1];
        const y = bsDate.year;
        if (useNepaliDigits) {
            return `${toNepaliDigits(bsDate.day)} ${BS_MONTHS[bsDate.month - 1]} ${toNepaliDigits(y)}`;
        }
        return `${d} ${m} ${y}`;
    }

    /**
     * Format AD date as YYYY-MM-DD (for form submission)
     */
    function formatAdDate(date) {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    // ============================================================
    // Nepali Date Picker Widget
    // ============================================================

    class NepaliDatePicker {
        constructor(input) {
            this.originalInput = input;
            this.isReadonly = input.hasAttribute('readonly');
            this.calendar = null;
            this.isOpen = false;
            this.selectedBsDate = null;

            // Get current BS date
            const today = new Date();
            const todayBs = adToBs(today.getFullYear(), today.getMonth() + 1, today.getDate());
            this.viewYear = todayBs.year;
            this.viewMonth = todayBs.month;

            // If input has a value, convert it to BS
            if (input.value) {
                const parts = input.value.split('-');
                if (parts.length === 3) {
                    const bs = adToBs(parseInt(parts[0]), parseInt(parts[1]), parseInt(parts[2]));
                    if (bs) {
                        this.selectedBsDate = bs;
                        this.viewYear = bs.year;
                        this.viewMonth = bs.month;
                    }
                }
            }

            this._createDisplayInput();
            this._createCalendar();
            this._bindEvents();
        }

        _createDisplayInput() {
            // Create a visible display input for BS date
            this.displayInput = document.createElement('input');
            this.displayInput.type = 'text';
            this.displayInput.readOnly = true;
            this.displayInput.className = this.originalInput.className + ' nepali-date-display';
            this.displayInput.placeholder = 'नेपाली मिति छान्नुहोस्';
            this.displayInput.style.cursor = this.isReadonly ? 'not-allowed' : 'pointer';

            if (this.selectedBsDate) {
                this.displayInput.value = formatBsDate(this.selectedBsDate);
            }

            // Hide original, show display
            this.originalInput.style.display = 'none';
            this.originalInput.parentNode.insertBefore(this.displayInput, this.originalInput.nextSibling);
        }

        _createCalendar() {
            this.calendar = document.createElement('div');
            this.calendar.className = 'nepali-calendar';
            this.calendar.innerHTML = this._renderCalendar();
            document.body.appendChild(this.calendar);
        }

        _renderCalendar() {
            const daysInMonth = getDaysInBSMonth(this.viewYear, this.viewMonth);
            // Get the AD equivalent of 1st of this BS month to know the day of week
            const firstDayAd = bsToAd(this.viewYear, this.viewMonth, 1);
            const startDay = firstDayAd.getDay(); // 0=Sun

            // Today in BS
            const today = new Date();
            const todayBs = adToBs(today.getFullYear(), today.getMonth() + 1, today.getDate());

            let html = `
                <div class="nc-header">
                    <button type="button" class="nc-nav nc-prev-year" data-action="prev-year" title="Previous Year">
                        <i class="fa-solid fa-angles-left"></i>
                    </button>
                    <button type="button" class="nc-nav nc-prev" data-action="prev" title="Previous Month">
                        <i class="fa-solid fa-chevron-left"></i>
                    </button>
                    <div class="nc-title">
                        <span class="nc-month-name">${BS_MONTHS[this.viewMonth - 1]}</span>
                        <span class="nc-year">${toNepaliDigits(this.viewYear)}</span>
                        <span class="nc-month-en">${BS_MONTHS_EN[this.viewMonth - 1]} ${this.viewYear}</span>
                    </div>
                    <button type="button" class="nc-nav nc-next" data-action="next" title="Next Month">
                        <i class="fa-solid fa-chevron-right"></i>
                    </button>
                    <button type="button" class="nc-nav nc-next-year" data-action="next-year" title="Next Year">
                        <i class="fa-solid fa-angles-right"></i>
                    </button>
                </div>
                <div class="nc-day-names">
                    ${BS_DAYS_SHORT.map((d, i) => `<span class="${i === 6 ? 'nc-sat' : i === 0 ? 'nc-sun' : ''}">${d}</span>`).join('')}
                </div>
                <div class="nc-days">
            `;

            // Empty cells before first day
            for (let i = 0; i < startDay; i++) {
                html += '<span class="nc-empty"></span>';
            }

            // Day cells
            for (let d = 1; d <= daysInMonth; d++) {
                const dayOfWeek = (startDay + d - 1) % 7;
                const isToday = todayBs && todayBs.year === this.viewYear && todayBs.month === this.viewMonth && todayBs.day === d;
                const isSelected = this.selectedBsDate && this.selectedBsDate.year === this.viewYear && this.selectedBsDate.month === this.viewMonth && this.selectedBsDate.day === d;
                const isSat = dayOfWeek === 6;
                const isSun = dayOfWeek === 0;

                let cls = 'nc-day';
                if (isToday) cls += ' nc-today';
                if (isSelected) cls += ' nc-selected';
                if (isSat) cls += ' nc-sat';
                if (isSun) cls += ' nc-sun';

                // Get AD equivalent for tooltip
                const adEquiv = bsToAd(this.viewYear, this.viewMonth, d);
                const adStr = `${adEquiv.getDate()} ${adEquiv.toLocaleString('en', { month: 'short' })} ${adEquiv.getFullYear()}`;

                html += `<span class="${cls}" data-day="${d}" title="AD: ${adStr}">
                    <span class="nc-day-np">${toNepaliDigits(d)}</span>
                    <span class="nc-day-ad">${adEquiv.getDate()}</span>
                </span>`;
            }

            html += '</div>';

            // Today button and AD equivalent display
            const adEquivMonth = bsToAd(this.viewYear, this.viewMonth, 1);
            html += `
                <div class="nc-footer">
                    <button type="button" class="nc-today-btn" data-action="today">
                        <i class="fa-solid fa-calendar-day me-1"></i>आज
                    </button>
                    <span class="nc-ad-equiv">
                        AD: ${adEquivMonth.toLocaleString('en', { month: 'long' })} ${adEquivMonth.getFullYear()}
                    </span>
                </div>
            `;

            return html;
        }

        _bindEvents() {
            // Open calendar on display input click
            if (!this.isReadonly) {
                this.displayInput.addEventListener('click', (e) => {
                    e.stopPropagation();
                    this.toggle();
                });
            }

            // Calendar click events (delegated)
            this.calendar.addEventListener('click', (e) => {
                e.stopPropagation();
                const target = e.target.closest('[data-action], [data-day]');
                if (!target) return;

                if (target.dataset.action === 'prev') {
                    this.viewMonth--;
                    if (this.viewMonth < 1) { this.viewMonth = 12; this.viewYear--; }
                    this._updateCalendar();
                } else if (target.dataset.action === 'next') {
                    this.viewMonth++;
                    if (this.viewMonth > 12) { this.viewMonth = 1; this.viewYear++; }
                    this._updateCalendar();
                } else if (target.dataset.action === 'prev-year') {
                    this.viewYear--;
                    this._updateCalendar();
                } else if (target.dataset.action === 'next-year') {
                    this.viewYear++;
                    this._updateCalendar();
                } else if (target.dataset.action === 'today') {
                    const today = new Date();
                    const todayBs = adToBs(today.getFullYear(), today.getMonth() + 1, today.getDate());
                    this._selectDate(todayBs.year, todayBs.month, todayBs.day);
                } else if (target.dataset.day) {
                    this._selectDate(this.viewYear, this.viewMonth, parseInt(target.dataset.day));
                }
            });

            // Close on outside click
            document.addEventListener('click', (e) => {
                if (this.isOpen && !this.calendar.contains(e.target) && e.target !== this.displayInput) {
                    this.close();
                }
            });

            // Close on Escape
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this.isOpen) this.close();
            });
        }

        _selectDate(year, month, day) {
            this.selectedBsDate = { year, month, day };
            this.viewYear = year;
            this.viewMonth = month;

            // Update display
            this.displayInput.value = formatBsDate(this.selectedBsDate);

            // Convert to AD and set the hidden input
            const adDate = bsToAd(year, month, day);
            this.originalInput.value = formatAdDate(adDate);

            // Trigger change event on original input
            this.originalInput.dispatchEvent(new Event('change', { bubbles: true }));
            this.originalInput.dispatchEvent(new Event('input', { bubbles: true }));

            this._updateCalendar();
            this.close();
        }

        _updateCalendar() {
            this.calendar.innerHTML = this._renderCalendar();
        }

        toggle() {
            if (this.isOpen) this.close();
            else this.open();
        }

        open() {
            if (this.isReadonly) return;
            // Close all other nepali pickers
            document.querySelectorAll('.nepali-calendar.nc-open').forEach(cal => {
                cal.classList.remove('nc-open');
            });

            // Re-sync if original input has changed
            if (this.originalInput.value) {
                const parts = this.originalInput.value.split('-');
                if (parts.length === 3) {
                    const bs = adToBs(parseInt(parts[0]), parseInt(parts[1]), parseInt(parts[2]));
                    if (bs) {
                        this.selectedBsDate = bs;
                        this.viewYear = bs.year;
                        this.viewMonth = bs.month;
                    }
                }
            }

            this._updateCalendar();
            this._positionCalendar();
            this.calendar.classList.add('nc-open');
            this.isOpen = true;
        }

        close() {
            this.calendar.classList.remove('nc-open');
            this.isOpen = false;
        }

        _positionCalendar() {
            const rect = this.displayInput.getBoundingClientRect();
            const calH = 340;
            const spaceBelow = window.innerHeight - rect.bottom;
            const top = spaceBelow < calH ? (rect.top - calH - 5 + window.scrollY) : (rect.bottom + 5 + window.scrollY);
            this.calendar.style.top = top + 'px';
            this.calendar.style.left = rect.left + window.scrollX + 'px';
            this.calendar.style.minWidth = Math.max(rect.width, 300) + 'px';
        }

        destroy() {
            if (this.displayInput && this.displayInput.parentNode) {
                this.displayInput.parentNode.removeChild(this.displayInput);
            }
            if (this.calendar && this.calendar.parentNode) {
                this.calendar.parentNode.removeChild(this.calendar);
            }
            this.originalInput.style.display = '';
        }

        // Refresh the display from original input value
        refresh() {
            if (this.originalInput.value) {
                const parts = this.originalInput.value.split('-');
                if (parts.length === 3) {
                    const bs = adToBs(parseInt(parts[0]), parseInt(parts[1]), parseInt(parts[2]));
                    if (bs) {
                        this.selectedBsDate = bs;
                        this.viewYear = bs.year;
                        this.viewMonth = bs.month;
                        this.displayInput.value = formatBsDate(bs);
                    }
                }
            } else {
                this.displayInput.value = '';
                this.selectedBsDate = null;
            }
        }
    }

    // ============================================================
    // Global Toggle Manager
    // ============================================================

    window.NepaliDatePickerManager = {
        instances: [],
        mode: localStorage.getItem('datePickerMode') || 'english', // 'english' or 'nepali'
        flatpickrInstances: [],

        init: function () {
            // Apply saved mode
            this._applyMode();
        },

        toggle: function () {
            this.mode = this.mode === 'english' ? 'nepali' : 'english';
            localStorage.setItem('datePickerMode', this.mode);
            this._applyMode();
            this._updateToggleButton();
        },

        _applyMode: function () {
            const dateInputs = document.querySelectorAll('input[type="date"], input.datepicker');

            if (this.mode === 'nepali') {
                // Destroy existing flatpickr instances
                this.flatpickrInstances.forEach(fp => {
                    try { fp.destroy(); } catch (e) { }
                });
                this.flatpickrInstances = [];

                // Also destroy flatpickr on any remaining inputs
                dateInputs.forEach(input => {
                    if (input._flatpickr) {
                        try { input._flatpickr.destroy(); } catch (e) { }
                    }
                });

                // Create Nepali date pickers
                this.instances.forEach(inst => {
                    try { inst.destroy(); } catch (e) { }
                });
                this.instances = [];

                dateInputs.forEach(input => {
                    // Keep the input type but hide it
                    input.setAttribute('type', 'text');
                    const picker = new NepaliDatePicker(input);
                    this.instances.push(picker);
                });

            } else {
                // Destroy Nepali pickers
                this.instances.forEach(inst => {
                    try { inst.destroy(); } catch (e) { }
                });
                this.instances = [];

                // Restore flatpickr
                dateInputs.forEach(input => {
                    input.setAttribute('type', 'date');
                });

                // Re-initialize flatpickr
                this.flatpickrInstances = [];
                document.querySelectorAll('input[type="date"]').forEach(input => {
                    try {
                        const fp = flatpickr(input, {
                            dateFormat: "Y-m-d",
                            altInput: true,
                            altFormat: "j M Y",
                            allowInput: true,
                            animate: true
                        });
                        this.flatpickrInstances.push(fp);
                    } catch (e) { }
                });
                document.querySelectorAll('.datepicker').forEach(input => {
                    try {
                        const fp = flatpickr(input, {
                            dateFormat: "Y-m-d",
                            altInput: true,
                            altFormat: "j M Y",
                            allowInput: true,
                            animate: true
                        });
                        this.flatpickrInstances.push(fp);
                    } catch (e) { }
                });
            }

            this._updateToggleButton();
        },

        _updateToggleButton: function () {
            const btn = document.getElementById('dateToggleBtn');
            if (!btn) return;

            const isNepali = this.mode === 'nepali';
            const label = btn.querySelector('.date-toggle-label');
            const icon = btn.querySelector('.date-toggle-icon');

            if (label) label.textContent = isNepali ? 'ने' : 'EN';
            if (icon) {
                icon.className = isNepali
                    ? 'date-toggle-icon fa-solid fa-om'
                    : 'date-toggle-icon fa-solid fa-calendar-days';
            }
            btn.title = isNepali ? 'Switch to English Date' : 'नेपाली मिति मा स्विच गर्नुहोस्';
            btn.classList.toggle('nepali-active', isNepali);
        }
    };

    // Export conversion functions globally
    window.NepaliDateConverter = { adToBs, bsToAd, formatBsDate, formatAdDate, toNepaliDigits, BS_MONTHS, BS_MONTHS_EN };

})();
