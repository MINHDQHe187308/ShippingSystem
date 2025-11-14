document.addEventListener('DOMContentLoaded', function () {
    if (typeof FullCalendar === 'undefined') {
        console.error('FullCalendar is not loaded. Please check script imports.');
        return;
    }
    const calendarEl = document.getElementById('calendar');
    if (!calendarEl) {
        console.error('Element with ID "calendar" not found.');
        return;
    }
    // --- THÊM: Web Audio API cho tiếng kêu beep (không cần file external, tránh 404)
    let audioContext = null;
    function initAudioContext() {
        if (!audioContext) {
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }
        return audioContext;
    }
    function playBeep(volume = 0.5, frequency = 800, duration = 200) {
        const context = initAudioContext();
        const oscillator = context.createOscillator();
        const gainNode = context.createGain();
        oscillator.connect(gainNode);
        gainNode.connect(context.destination);
        oscillator.frequency.value = frequency; // Tần số cho tiếng beep cao
        oscillator.type = 'sine'; // Hình sóng sine cho âm thanh sạch
        gainNode.gain.setValueAtTime(0, context.currentTime);
        gainNode.gain.linearRampToValueAtTime(volume, context.currentTime + 0.01);
        gainNode.gain.exponentialRampToValueAtTime(0.01, context.currentTime + duration / 1000);
        oscillator.start(context.currentTime);
        oscillator.stop(context.currentTime + duration / 1000);
    }
    // --- Biến global cho delay mode
    let delayMode = false;
    // --- Biến global cho scroll mode (face icon)
    let scrollMode = false;
    // --- Thêm nút Delay Mode
    const delayBtn = document.createElement('button');
    delayBtn.id = 'delayModeBtn';
    delayBtn.className = 'btn btn-warning';
    delayBtn.textContent = 'Delay Mode: Off';
    delayBtn.style.margin = '10px';
    document.getElementById('legend').parentNode.insertBefore(delayBtn, document.getElementById('legend').nextSibling);
    // Event listener cho nút
    delayBtn.addEventListener('click', function () {
        delayMode = !delayMode;
        this.textContent = `Delay Mode: ${delayMode ? 'On' : 'Off'}`;
        this.className = delayMode ? 'btn btn-danger' : 'btn btn-warning';
        playBeep(0.5); // Tiếng kêu khi toggle
        if (delayMode) {
            // Thay đổi cursor toàn bộ body thành not-allowed (hình tròn đỏ với dấu gạch)
            document.body.style.cursor = 'not-allowed';
            // Thêm class cho calendar để apply hover effect mạnh hơn
            calendarEl.classList.add('delay-mode');
        } else {
            document.body.style.cursor = 'default';
            calendarEl.classList.remove('delay-mode');
        }
    });
    // --- THÊM: Cute scroll-face button (click to enable calendar pan/scroll mode)
    const scrollFaceBtn = document.createElement('button');
    scrollFaceBtn.id = 'scrollFaceBtn';
    scrollFaceBtn.className = 'btn btn-light scroll-face-btn';
    scrollFaceBtn.title = 'Toggle Scroll Mode';
    // Use an emoji inside so it's easy and cute — will flip to an angry face when active
    scrollFaceBtn.innerHTML = '<span class="scroll-face-emoji" role="img" aria-label="face">😊</span>';
    // Insert after delay button
    delayBtn.parentNode.insertBefore(scrollFaceBtn, delayBtn.nextSibling);

    // --- THÊM: Speech bubble attached to the face button
    const scrollFaceBubble = document.createElement('div');
    scrollFaceBubble.className = 'scroll-face-bubble';
    scrollFaceBubble.setAttribute('aria-hidden', 'true');
    scrollFaceBubble.textContent = 'Use Left Mouse Scroll right to view History';
    // Ensure button is positioned relative so bubble absolute positioning works
    scrollFaceBtn.style.position = 'relative';
    scrollFaceBtn.appendChild(scrollFaceBubble);

    // Toggle behavior for scroll face
    scrollFaceBtn.addEventListener('click', function (ev) {
        ev.stopPropagation();
        scrollMode = !scrollMode;
        const emoji = this.querySelector('.scroll-face-emoji');
        if (scrollMode) {
            // Active: angry face, change styles, enable scroll-mode class, play stronger beep
            emoji.textContent = '😠';
            this.classList.add('active');
            this.classList.remove('btn-light');
            this.classList.add('btn-danger');
            document.body.style.cursor = 'grab';
            calendarEl.classList.add('scroll-mode');
            try { playBeep(0.6, 1100, 140); } catch (e) { console.warn('playBeep failed', e); }
            // Show speech bubble briefly to guide users
            scrollFaceBubble.setAttribute('aria-hidden', 'false');
            scrollFaceBubble.classList.add('visible');
            // Auto-hide after 4 seconds if still visible
            setTimeout(() => {
                if (scrollFaceBubble) {
                    scrollFaceBubble.classList.remove('visible');
                    scrollFaceBubble.setAttribute('aria-hidden', 'true');
                }
            }, 4000);
        } else {
            // Inactive: smiling face, revert styles, play subtle beep
            emoji.textContent = '😊';
            this.classList.remove('active');
            this.classList.remove('btn-danger');
            this.classList.add('btn-light');
            document.body.style.cursor = 'default';
            calendarEl.classList.remove('scroll-mode');
            // Hide bubble immediately when turning off
            if (scrollFaceBubble) {
                scrollFaceBubble.classList.remove('visible');
                scrollFaceBubble.setAttribute('aria-hidden', 'true');
            }
            try { playBeep(0.35, 700, 100); } catch (e) { console.warn('playBeep failed', e); }
        }
    });
    // --- Thêm CSS styles cho z-index, height, centering và CUSTOM TOOLTIP + HOVER EFFECT CHỈ CHO DELAY MODE + THÊM STYLE CHO DELAY BAR (COMMENT CLASS DELAY-EVENT)
    const style = document.createElement('style');
    style.textContent = `
        .fc-event {
            height: 80px !important; /* Tăng từ 70px lên 80px để chứa thêm thông tin progress */
            display: flex !important;
            align-items: center !important; /* Căn giữa theo chiều dọc trong slot */
            justify-content: center !important; /* Căn giữa text nếu cần */
            margin: 0 !important;
            line-height: 1.2 !important;
            cursor: pointer; /* Thêm cursor pointer để dễ nhận biết hover */
            transition: transform 0.2s ease, box-shadow 0.2s ease; /* Giữ transition để mượt mà khi có effect */
            position: relative; /* THÊM: Để position absolute cho delay bar */
            overflow: visible !important; /* THÊM: Cho phép delay bar extend ra ngoài */
        }
        .fc-event.plan-event {
            z-index: 1;
        }
        .fc-event.actual-event {
            z-index: 2;
            height: 80px !important; /* Đồng bộ height với .fc-event */
            line-height: 1.2 !important;
        }
        /* COMMENT: .fc-event.delay-event { ... } để không apply viền đỏ cho Delay */
        /*
        .fc-event.delay-event {
            border: 3px solid #ff0000 !important;
            box-shadow: 0 0 10px rgba(255, 0, 0, 0.5);
        }
        */
        .fc-event .fc-event-main { /* Đảm bảo inner content cũng center */
            display: flex;
            align-items: center;
            height: 100%;
            font-size: 17px; /* Tăng font-size để dễ đọc hơn */
        }
        /* Tăng chiều cao resource rows để chứa event lớn hơn */
        .fc-resource-timeline .fc-resource-cell {
            height: 90px !important; /* Tăng height của resource cell từ 80px lên 90px */
            padding: 4px !important; /* Thêm padding để không sát mép */
        }
        .fc-resource-timeline .fc-timeline-slot-table .fc-resource-cell {
            height: 90px !important; /* Đảm bảo apply cho slot table cells */
        }
        /* XÓA: Hover effect cơ bản - CHỈ GIỮ CHO DELAY MODE */
        /* THÊM: Hover effect mạnh hơn khi delay mode on */
        .delay-mode .fc-event:hover {
            transform: scale(1.05);
            box-shadow: 0 4px 12px rgba(255, 0, 0, 0.3); /* Đỏ nhạt để match theme */
            border: 2px solid #ff0000; /* Viền đỏ */
        }
        /* THÊM: Style cho delay bar (màu đỏ, nối tiếp) */
        .delay-bar {
            position: absolute;
            top: 0;
            height: 100%;
            background: #ff0000 !important;
            border-radius: 4px;
            z-index: 3;
            opacity: 0.8;
        }
        /* THÊM MỚI: Style cho multi-day delay event (toàn bộ đỏ) */
        .fc-event.multi-day-delay {
            background-color: #ff0000 !important;
            border-color: #ff0000 !important;
            color: #fff !important;
        }
        /* Custom Tooltip Styles - ĐẸP MẮT VÀ DỄ NHÌN - TĂNG KÍCH THƯỚC */
        #custom-tooltip {
            position: absolute;
            background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 20px; /* Tăng padding từ 16px lên 20px */
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
            z-index: 10001;
            pointer-events: none;
            min-width: 300px; /* Tăng từ 250px lên 300px */
            max-width: 500px; /* Tăng từ 400px lên 500px cho responsive */
            width: auto; /* Tự động điều chỉnh */
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            font-size: 16px; /* Tăng font-size tổng thể từ 15px lên 16px */
            line-height: 1.6; /* Tăng line-height từ 1.5 lên 1.6 cho dễ đọc hơn */
            display: none;
            transition: opacity 0.1s ease-out, transform 0.1s ease-out; /* Transition mượt mà nhưng nhanh */
            word-wrap: break-word; /* Tự động xuống dòng nếu text quá dài */
            overflow-wrap: break-word; /* Hỗ trợ break word */
        }
        #custom-tooltip.show {
            display: block;
            opacity: 1;
            transform: translateY(0);zz
        }
        #custom-tooltip h4 {
            margin: 0 0 16px 0; /* Tăng margin-bottom từ 12px lên 16px */
            color: #333;
            font-size: 18px; /* Tăng font-size từ 17px lên 18px */
            font-weight: 600;
            border-bottom: 1px solid #eee;
            padding-bottom: 8px; /* Tăng padding-bottom từ 6px lên 8px */
        }
        #custom-tooltip dl {
            margin: 0;
            display: grid;
            grid-template-columns: auto 1fr;
            gap: 8px 12px; /* Tăng gap từ 6px 10px lên 8px 12px */
            align-items: center;
        }
        #custom-tooltip dt {
            font-weight: 700; /* Tăng font-weight từ 600 lên 700 để đậm hơn */
            color: #000; /* Đổi màu từ #555 sang #000 (đen) */
            text-align: right;
            min-width: 140px; /* Tăng min-width từ 120px lên 140px để thẳng hàng tốt hơn */
            font-size: 16px; /* Tăng font-size từ 15px lên 16px cho dt */
        }
        #custom-tooltip dd {
            margin: 0;
            color: #333;
            font-weight: 400;
            font-size: 16px; /* Tăng font-size từ 15px lên 16px cho dd */
        }
        /* THÊM MỚI: Style cho dd của Plan Time và Actual Time - chữ đen đậm */
        #custom-tooltip dd.plan-time-dd,
        #custom-tooltip dd.actual-time-dd {
            color: #000 !important;
            font-weight: 700 !important;
        }
        #custom-tooltip .status {
            padding: 6px 10px; /* Tăng padding từ 4px 8px lên 6px 10px */
            border-radius: 4px;
            font-size: 13px; /* Tăng font-size từ 12px lên 13px */
            font-weight: 600;
            text-transform: uppercase;
        }
        .delay-bar {
    position: absolute;
    top: 0;
    height: 100%;
    background: #ff0000 !important;
    border-radius: 4px;
    z-index: 3;
    opacity: 0.8;
    /* FIX: Cho phép extend ra ngoài mà không bị clip */
    right: auto;
    left: 0; /* Default, override bằng JS */
    transform-origin: left center; /* Để scale nếu cần */
}
.fc-event { /* Đảm bảo wrapper cho extend */
    overflow: visible !important;
    position: relative;
}
        #custom-tooltip .status.planned { background: #808080; color: #fff; }
        #custom-tooltip .status.pending { background: #007bff; color: #fff; }
        #custom-tooltip .status.shipped { background: #ffc107; color: #000; }
        #custom-tooltip .status.completed { background: #28a745; color: #fff; }
        #custom-tooltip .status.delay { background: #ff0000; color: #fff; } /* THÊM: Style cho Delay status trong tooltip */
   #calendar {
    overflow-x: hidden !important;
    width: 100% !important;
    max-width: 100vw;
}
body, .fc {
    overflow-x: hidden; /* Áp dụng cho body nếu cần */
}
.fc-timeline-event{
margin-top: 20px !important;
margin-bottom: 20px !important;
}
/* Cute scroll-face button styles */
.scroll-face-btn {
    margin-left: 8px;
    padding: 6px 8px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    transition: transform 0.12s ease, box-shadow 0.12s ease;
    position: relative; /* so bubble positions correctly */
}
.scroll-face-btn .scroll-face-emoji {
    font-size: 20px;
    line-height: 1;
    display: inline-block;
    transform-origin: center center;
    transition: transform 0.18s ease;
}
.scroll-face-btn:active .scroll-face-emoji { transform: scale(0.92) translateY(1px); }
.scroll-face-btn.active {
    box-shadow: 0 6px 14px rgba(0,0,0,0.18);
    transform: translateY(-2px) scale(1.02);
}
/* subtle wobble when activating */
@keyframes faceWobble {
    0% { transform: rotate(0deg); }
    25% { transform: rotate(-8deg); }
    50% { transform: rotate(6deg); }
    75% { transform: rotate(-3deg); }
    100% { transform: rotate(0deg); }
}
.scroll-face-btn.active .scroll-face-emoji { animation: faceWobble 420ms ease; }
/* While scroll mode active, make event elements non-interactive so user can drag anywhere */
.scroll-mode .fc-event { pointer-events: none; opacity: 0.98; }
.scroll-mode { cursor: grab; }
/* Speech bubble attached to face button */
.scroll-face-bubble {
    position: absolute;
    bottom: 100%;
    left: 50%;
    transform: translateX(-50%) translateY(-6px);
    background: #ffffff;
    color: #111;
    padding: 10px 16px; /* increased padding for larger bubble */
    border-radius: 14px;
    box-shadow: 0 8px 26px rgba(0,0,0,0.18);
    font-size: 17px; /* larger font for readability */
    line-height: 1.2;
    min-width: 220px; /* ensure wider bubble */
    max-width: 360px;
    text-align: center;
    white-space: normal; /* allow wrapping */
    opacity: 0;
    pointer-events: none;
    transition: opacity 180ms ease, transform 220ms cubic-bezier(.2,.9,.3,1);
    z-index: 10002;
}
.scroll-face-bubble.visible {
    opacity: 1;
    transform: translateX(-50%) translateY(-20px);
}
.scroll-face-bubble::after {
    content: '';
    position: absolute;
    top: 100%;
    left: 50%;
    transform: translateX(-50%);
    border-width: 8px; /* larger arrow */
    border-style: solid;
    border-color: #ffffff transparent transparent transparent;
}
.combined-label{
display: inline-block;
white-space : nowrap;
overflow : hidden;
position : relative;
}
.combined-label spam{
    display : inlibe-block;
    padding-left : 5px;
    animation : marqueeScroll 10s linear infinite;
    font-weight : 700;
    color : darkblue;
}
@keyframes marqueeScroll {
  0%{
      transform : translatex(0);
}
100%{
     transform : translatex(-100%);
}
}`;
    document.head.appendChild(style);
    // --- Tạo global tooltip element
    function createTooltip() {
        let tooltip = document.getElementById('custom-tooltip');
        if (!tooltip) {
            tooltip = document.createElement('div');
            tooltip.id = 'custom-tooltip';
            document.body.appendChild(tooltip);
        }
        return tooltip;
    }
    // --- Helper: lấy chuỗi HH:MM:SS từ Date (local BKK)
    function hhmmss(d) {
        const hh = String(d.getHours()).padStart(2, '0');
        const mm = String(d.getMinutes()).padStart(2, '0');
        const ss = String(d.getSeconds()).padStart(2, '0');
        return `${hh}:${mm}:${ss}`;
    }
    // --- Helper: format date + hour:minute (dd/mm/yyyy HH:MM) local BKK
    function formatDateTimeNoSeconds(d) {
        if (!d) return 'N/A';
        const date = d.toLocaleDateString('vi-VN', { timeZone: 'Asia/Bangkok' });
        const time = d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Bangkok' });
        return `${date} ${time}`;
    }
    // --- Helper: format date without year + hour:minute (dd/mm HH:MM) local BKK
    function formatDateTimeNoYear(d) {
        if (!d) return 'N/A';
        const date = d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', timeZone: 'Asia/Bangkok' });
        const time = d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Bangkok' });
        return `${date} ${time}`;
    }
    // --- THÊM MỚI: Helper format full date-time nếu trong quá khứ (local BKK)
    function formatFullTimeRange(start, end, now = new Date()) {
        if (!start || !end) return 'N/A';
        const isPast = start < now; // Nếu start < now, coi là past
        if (isPast) {
            // Full date: YYYY-MM-DD HH:MM:SS
            const fullStart = start.toLocaleString('sv-SE', { timeZone: 'Asia/Bangkok' }).replace(' ', 'T').slice(0, 19).replace('T', ' ');
            const fullEnd = end.toLocaleString('sv-SE', { timeZone: 'Asia/Bangkok' }).replace(' ', 'T').slice(0, 19).replace('T', ' ');
            return `${fullStart} - ${fullEnd}`;
        } else {
            // Chỉ time nếu future/current
            return `${hhmmss(start)} - ${hhmmss(end)}`;
        }
    }
    // --- Helper: Format time range (local BKK)
    // If dateOnlyIfPast is true and the start is in the past, show only the date (dd/mm/yyyy)
    function formatTimeRange(start, end, now = new Date(), dateOnlyIfPast = false) {
        if (!start || !end) return 'N/A';
        // If caller requests date-only (but we want date+hour:minute) when start is past (used for Actual time)
        // show localized date plus hour:minute (no seconds). Example: "03/11/2025 08:30 - 10:15" or
        // if different days: "03/11/2025 22:00 - 04/11/2025 01:30"
        if (dateOnlyIfPast && start < now) {
            const startDate = start.toLocaleDateString('vi-VN', { timeZone: 'Asia/Bangkok' });
            const endDate = end.toLocaleDateString('vi-VN', { timeZone: 'Asia/Bangkok' });
            const startTime = start.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Bangkok' });
            const endTime = end.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Asia/Bangkok' });
            if (startDate === endDate) {
                return `${startDate} ${startTime} - ${endTime}`;
            }
            return `${startDate} ${startTime} - ${endDate} ${endTime}`;
        }
        // Fallback: preserve existing behavior (full datetime for past, short time for future/current)
        return formatFullTimeRange(start, end, now);
    }
    // --- SỬA: getTimeRange() - Thêm param 'midnightMode' để xử lý mode đặc biệt lúc 00:00, TÍNH THEO LOCAL BKK + THÊM viewEnd
    function getTimeRange(midnightMode = false) {
        const now = new Date(); // Giờ local BKK
        const nowHour = now.getHours();
        const nowMin = now.getMinutes();
        const nowSec = now.getSeconds();
        const todayMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);
        const todayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 24, 0, 0);
        let start, end;
        if (midnightMode) {
            // Mode đặc biệt: Từ 00:00 local đến giờ hiện tại local +4h (cap 24:00 local)
            start = new Date(todayMidnight);
            end = new Date(now);
            end.setHours(nowHour + 4, 0, 0, 0);
            if (end >= todayEnd) {
                end = todayEnd;
            }
        } else {
            // Logic cũ: -6h đến +4h so với giờ local hiện tại
            start = new Date(now);
            start.setHours(nowHour - 6, 0, 0, 0);
            end = new Date(now);
            end.setHours(nowHour + 4, 0, 0, 0);
        }
        let startHour = hhmmss(start);
        let endHour = hhmmss(end);
        // Cap start: nếu < 00:00 hôm nay local, set "00:00:00"
        if (start < todayMidnight) {
            startHour = '00:00:00';
        }
        // Cap end: nếu >= 24:00 hôm nay local, set "24:00:00"
        if (end >= todayEnd) {
            endHour = '24:00:00';
        }
        return {
            startHour,
            endHour,
            currentShort: hhmmss(now).slice(0, 5),
            start,
            end, // ← THÊM: viewEnd Date object
            viewStart: start,
            viewEnd: end
        };
    }
    let timeRange = getTimeRange();
    let isMidnightMode = false; // Flag để track mode
    // --- SỬA: Mapping status từ số sang string - THÊM Delay (4)
    const statusMap = {
        0: 'Planned',
        1: 'Pending',
        2: 'Completed',
        3: 'Shipped',
        4: 'Delay' // THÊM MỚI: Status Delay
    };
    // Tạo customer map để lấy tên
    const customerMap = {};
    customers.forEach(c => {
        customerMap[c.CustomerCode] = c.CustomerName;
    });
    // THÊM: Đảm bảo resourceId của mọi event đều có trong resources (cho initial load) - FIX: Lọc rid undefined/null/empty để tránh dòng "undefined"
    const allResourceIds = Array.from(new Set(orders.map(o => o.resource).filter(rid => rid != null && rid !== '' && rid !== undefined)));
    if (customers) {
        const customerIds = customers.map(c => c.CustomerCode);
        allResourceIds.forEach(rid => {
            if (rid && !customerIds.includes(rid)) { // ← FIX: Thêm kiểm tra rid truthy
                customers.push({ CustomerCode: rid, CustomerName: rid });
            }
        });
    }
    // Tạo resources từ customers, lưu riêng CustomerName để render hai dòng (code above, name below)
    const resources = customers.map(c => ({
        id: c.CustomerCode,
        title: c.CustomerCode,
        customerName: c.CustomerName || ''
    })).filter(r => r.id != null && r.id !== '' && r.id !== undefined); // ← FIX: Lọc resources undefined
    // --- SỬA: Hàm lấy màu dựa trên status (fallback cho Delay để giữ màu cũ) + THÊM: Nếu Delay && delayTime >24 thì return #ff0000 (đỏ)
    function getColorByStatus(status, validActual = false, delayTime = 0) {
        // THÊM MỚI: Nếu Delay && delayTime >24h thì override toàn bộ thành đỏ
        if (status === 'Delay' && delayTime > 24) {
            return '#ff0000'; // Đỏ cho multi-day delay
        }
        // Nếu Delay, fallback về màu dựa trên actual/plan
        if (status === 'Delay') {
            if (validActual) {
                return getColorByStatus('Completed'); // Xanh lá nếu có actual
            } else {
                return getColorByStatus('Planned'); // Đen nếu chỉ plan
            }
        }
        const colors = {
            // Use a light neutral for Planned so planned-only events render softly
            'Planned': '#e9ecef',
            'Pending': '#007bff',
            'Completed': '#28a745',
            'Shipped': '#ffc107',
            'Delay': '#ff0000', // Giữ nguyên nhưng không dùng cho Delay nữa
            'Actual': '#17a2b8'
        };
        return colors[status] || '#d3d3d3';
    }
    // --- SỬA: Hàm lấy textColor dựa trên status (fallback cho Delay) + THÊM: Nếu multi-day delay thì text trắng (#fff)
    function getTextColorByStatus(status, validActual = false, delayTime = 0) {
        // THÊM MỚI: Nếu Delay && delayTime >24h thì text trắng
        if (status === 'Delay' && delayTime > 24) {
            return '#fff'; // Trắng trên nền đỏ
        }
        // Nếu Delay, fallback về text color dựa trên actual/plan
        if (status === 'Delay') {
            if (validActual) {
                return getTextColorByStatus('Completed');
            } else {
                return getTextColorByStatus('Planned');
            }
        }
        const darkBgs = ['#000000', '#007bff', '#28a745', '#17a2b8', '#ff0000'];
        const bgColor = getColorByStatus(status);
        return darkBgs.includes(bgColor) ? '#fff' : '#000';
    }
    // --- FIX: Helper parseAndValidate - PARSE AS LOCAL BKK (KHÔNG APPEND 'Z', GIẢ SỬ DB LÀ LOCAL BKK STRING) - MANUAL SPLIT ĐỂ TRÁNH LỆCH + THÊM: TRUNCATE FRACTIONAL SECONDS ĐỂ TRÁNH LỖI PARSE >3 DECIMALS
    function parseAndValidate(timeStr) {
        if (!timeStr) return null;
        let d;
        // Chuẩn hóa string: Thay space bằng T nếu cần, loại bỏ Z nếu có (vì DB local, không UTC)
        let normalized = timeStr.replace(' ', 'T').replace('Z', '');
        // FIX MỚI: Truncate fractional seconds to 3 decimals (milliseconds) để JS Date.parse đúng
        const dotIndex = normalized.indexOf('.');
        if (dotIndex !== -1) {
            const afterDot = normalized.slice(dotIndex + 1);
            if (afterDot.length > 3) {
                normalized = normalized.slice(0, dotIndex + 4); // Giữ .xxx (3 decimals)
                console.log(`Truncated fractional seconds for "${timeStr}" to "${normalized}"`); // DEBUG
            }
        }
        if (normalized.includes('T')) {
            // ISO-like: Parse as local (new Date() sẽ treat as local nếu no Z)
            d = new Date(normalized);
        } else {
            // Non-ISO (fallback, e.g., '2025-10-31 02:06:24'): Parse as UTC
            // SỬA: Cập nhật regex để capture fractional nếu có, nhưng truncate tương tự
            const parts = normalized.match(/(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})(\.\d{1,3})?/);
            if (parts) {
                const year = parseInt(parts[1]);
                const month = parseInt(parts[2]) - 1;
                const day = parseInt(parts[3]);
                const hour = parseInt(parts[4]);
                const min = parseInt(parts[5]);
                const sec = parseInt(parts[6]);
                const ms = parts[7] ? parseInt(parts[7].slice(1).padEnd(3, '0')) : 0; // Pad to 3 digits nếu <3
                d = new Date(year, month, day, hour, min, sec, ms);
            } else {
                // Fallback: Try Date.parse (local assumption)
                d = new Date(normalized);
            }
        }
        if (isNaN(d.getTime())) {
            console.warn(`Invalid time parse: "${timeStr}"`);
            return null;
        }
        // LOG: Hiển thị local BKK (sau convert từ UTC ms)
        console.log(`Parsed "${timeStr}" (DB local) → Local BKK: ${d.toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' })}`);
        return d; // Date object với ms UTC đúng → Render local BKK
    }
    // --- Tạo event data từ dữ liệu Order - THÊM UId VÀO EXTENDEDPROPS VÀ THÊM CÁC TRƯỜNG PROGRESS + THÊM DELAY INFO NẾU STATUS=4 + FIX: BỎ CLIPPING ĐỂ HIỂN THỊ ĐẦY ĐỦ KHI PAN/ROLL + FIX: Skip nếu customerCode undefined
    const eventsData = orders.map((order, index) => {
        const planStart = parseAndValidate(order.StartTime);
        const planEnd = parseAndValidate(order.EndTime);
        const actualStart = parseAndValidate(order.AcStartTime);
        const actualEnd = parseAndValidate(order.AcEndTime);
        // Valid nếu start < end
        const validPlan = planStart && planEnd && planStart < planEnd;
        const validActual = actualStart && actualEnd && actualStart < actualEnd;
        let eventStart, eventEnd;
        // FIX: ParseInt để map đúng status (nếu order.Status là string "2" → number 2)
        let status = statusMap[parseInt(order.Status, 10)] || 'Planned';
        // Debug log (xóa sau khi test OK) - SỬA: Dùng order.UId thay vì order.id
        console.log('Order UId:', order.UId || 'undefined', 'Status raw:', order.Status, 'Status mapped:', status, 'Color:', getColorByStatus(status));
        if (validActual) {
            eventStart = actualStart;
            eventEnd = actualEnd;
        } else if (validPlan) {
            eventStart = planStart;
            eventEnd = planEnd;
        } else {
            return null; // Skip invalid order
        }
        // FIX: Extend span to union nếu có cả actual và plan (KHÔNG CLIP Ở ĐÂY ĐỂ GIỮ FULL DB RANGE)
        let hasBoth = false;
        if (validActual && validPlan) {
            eventStart = new Date(Math.min(actualStart, planStart));
            eventEnd = new Date(Math.max(actualEnd, planEnd));
            hasBoth = true;
        }
        // BỎ CLIPPING: Để event có full start/end từ DB, FullCalendar sẽ tự handle overlap với slots khi pan
        // Không cần: eventStart = Math.max(eventStart, slotMin); eventEnd = Math.min(eventEnd, viewEnd);
        // Không cần: if (eventEnd <= eventStart) return null; // Giờ luôn render nếu overlap slots
        const customerCode = order.CustomerCode || order.Resource || 'Unknown';
        if (!customerCode || customerCode === 'undefined') return null; // ← FIX: Skip event nếu customerCode undefined/null/empty
        // THÊM: Delay info nếu status=='Delay' (giả sử fetch từ API hoặc include trong data)
        let delayStart = null, delayEnd = null, delayTime = 0;
        if (status === 'Delay') {
            // Giả sử order có thêm fields DelayStartTime, DelayTime (cần update controller để include)
            delayStart = parseAndValidate(order.DelayStartTime); // Từ DB
            delayTime = parseFloat(order.DelayTime) || 0;
            if (delayStart) {
                delayEnd = new Date(delayStart.getTime() + delayTime * 60 * 60 * 1000);
            }
        }
        const eventObj = {
            id: `order-${order.UId}`,
            resourceId: customerCode,
            start: eventStart,
            end: eventEnd,
            title: '', // Đặt rỗng vì dùng custom eventContent
            // Xóa status ở root level, di chuyển vào extendedProps
            hasBoth: hasBoth,
            extendedProps: {
                uid: order.UId, // THÊM: Để sử dụng trong eventClick
                customerCode: customerCode, // THÊM: Lưu CustomerCode vào extendedProps để dùng trong modal title
                planStart: validPlan ? planStart.toISOString() : null, // Lưu ISO string để tránh re-parse
                planEnd: validPlan ? planEnd.toISOString() : null,
                // Always expose actualStart/actualEnd if present; validActual still indicates both exist and are ordered
                actualStart: actualStart ? actualStart.toISOString() : null,
                actualEnd: actualEnd ? actualEnd.toISOString() : null,
                validActual: validActual,
                status: status, // ← SỬA: Di chuyển status vào extendedProps
                totalPallet: order.TotalPallet || 0, // THÊM: Lưu TotalPallet vào extendedProps để dùng trong eventContent
                collectPallet: order.CollectPallet || '0 / 0', // THÊM: Progress Collect
                threePointScan: order.ThreePointScan || '0 / 0', // THÊM: Progress ThreePointScan
                loadCont: order.LoadCont || '0 / 0', // THÊM: Progress LoadCont
                shipDate: order.ShipDate || 'N/A',
                transCd: order.TransCd || 'N/A',
                transMethod: order.TransMethod || 'N/A',
                contSize: order.ContSize || 'N/A',
                totalColumn: order.TotalColumn || 0,
                // THÊM: Delay info
                delayStart: delayStart ? delayStart.toISOString() : null,
                delayEnd: delayEnd ? delayEnd.toISOString() : null,
                delayTime: delayTime
            }
        };
        return eventObj;
    }).filter(e => e); // Remove null events
    // --- Khởi tạo FullCalendar - XÓA timeZone: 'UTC', RENDER LOCAL BKK
    const calendar = new FullCalendar.Calendar(calendarEl, {
        schedulerLicenseKey: 'GPL-My-Project-Is-Open-Source',
        initialView: 'resourceTimelineDay',
        nowIndicator: false,
        width: '85%',
        height: 'auto',
        // timeZone: 'UTC', // ← XÓA: Render theo local BKK (mặc định)
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'resourceTimelineDay,resourceTimelineWeek'
        },
        slotDuration: '01:00:00',
        slotLabelInterval: { hours: 1 },
        slotLabelFormat: { hour: '2-digit', minute: '2-digit', hour12: false },
        slotMinTime: timeRange.startHour,
        slotMaxTime: timeRange.endHour,
        scrollTime: timeRange.currentShort,
        resourceAreaHeaderContent: 'Customer',
        resourceAreaWidth: '120px',
        resources: resources,
        // Render resource label with CustomerCode (top) and CustomerName (bottom)
        resourceLabelContent: function (arg) {
            const container = document.createElement('div');
            container.style.display = 'flex';
            container.style.flexDirection = 'column';
            container.style.alignItems = 'flex-start';
            container.style.lineHeight = '1.1';
            container.style.padding = '4px 8px';
            container.style.boxSizing = 'border-box';
            container.style.width = '100%';
            const code = document.createElement('div');
            code.textContent = arg.resource.title || arg.resource.id || '';
            code.style.fontWeight = '700';
            code.style.fontSize = '15px';
            code.style.whiteSpace = 'nowrap';
            code.style.overflow = 'hidden';
            code.style.textOverflow = 'ellipsis';
            code.style.flex = '0 0 auto';
            // name below code with smaller font and slight top margin
            const name = document.createElement('div');
            name.textContent = arg.resource.extendedProps && arg.resource.extendedProps.customerName ? arg.resource.extendedProps.customerName : (arg.resource.customerName || '');
            name.style.fontSize = '13px';
            name.style.color = '#666';
            name.style.whiteSpace = 'nowrap';
            name.style.overflow = 'hidden';
            name.style.textOverflow = 'ellipsis';
            name.style.marginTop = '8px';
            name.style.width = '100%';
            container.appendChild(code);
            if (name.textContent) container.appendChild(name);
            return { domNodes: [container] };
        },
        eventOverlap: false,
        eventOrderStrict: true,
        slotEventOverlap: false,
        eventOrderStrict: true,
        dayMaxEvents: true,
        events: eventsData.map(e => {
            const extendedProps = e.extendedProps;
            const status = extendedProps.status;
            const validActual = extendedProps.validActual;
            const delayTime = extendedProps.delayTime || 0; // THÊM: Lấy delayTime để check multi-day
            // SỬA: Fallback màu cho Delay + THÊM: Nếu Delay && delayTime >24 thì đỏ
            // If the event is a union of plan+actual (hasBoth) OR it's plan-only (no valid actual),
            // render the outer FC event container as transparent so the inner custom plan bar
            // (which provides the desired gradient/size) is the visible element. This prevents
            // the light-gray fallback box/border around plan-only events.
            const bgColor = (e.hasBoth || !validActual) ? 'transparent' : getColorByStatus(status, validActual, delayTime);
            const borderColor = (e.hasBoth || !validActual) ? 'transparent' : getColorByStatus(status, validActual, delayTime);
            // SỬA: Chỉ thêm class actual-event, XÓA delay-event để không apply viền đỏ + THÊM: Thêm class multi-day-delay nếu >24h
            let classNames = extendedProps.validActual ? ['actual-event'] : [];
            if (status === 'Delay' && delayTime > 24) {
                classNames.push('multi-day-delay'); // THÊM: Class cho CSS override đỏ
            }
            // Không push 'delay-event' nữa
            return {
                ...e,
                backgroundColor: bgColor,
                borderColor: borderColor,
                textColor: getTextColorByStatus(status, validActual, delayTime), // Truyền delayTime
                fontWeight: 'normal',
                classNames: classNames
            };
        }),
        // ... (giữ nguyên eventClick và eventContent như cũ, vì formatTimeRange giờ là local BKK)
        eventClick: function (info) {
            // ... (giữ nguyên code eventClick từ mã trước)
            console.log('Event clicked! Info:', info); // DEBUG: Log toàn bộ info
            window.currentDelayUid = info.event.extendedProps.uid; // THÊM: Lưu global UID cho delay modal
            window.currentDelayEvent = info.event; // THÊM MỚI: Lưu full event info (cho set StartTime future)
            // THÊM: Nếu delayMode on, phát sound và kiểm tra status
            if (delayMode) {
                playBeep(0.5); // Tiếng kêu khi click event
                const extendedProps = info.event.extendedProps;
                const status = extendedProps.status;
                // THÊM MỚI: Check status - Chỉ mở modal nếu Planned, Pending, Completed
                if (status === 'Planned' || status === 'Pending' || status === 'Completed') {
                    const delayModalElement = document.getElementById('delayModal');
                    if (delayModalElement) {
                        // Prefill form với data từ event nếu có (tạm thời: StartTime = current time, read-only)
                        const delayModal = new bootstrap.Modal(delayModalElement);
                        delayModal.show();
                    }
                } else if (status === 'Shipped') {
                    // THÊM MỚI: Hiện thông báo nếu Shipped
                    alert('không thể DELAY order đã xuất hàng đôu :_(((!');
                    playBeep(0.3, 400, 300); // Beep thấp để báo lỗi
                } else {
                    // Fallback cho status khác (như Delay)
                    alert(`Không thể DELAY order có trạng thái ${status.toLowerCase()}!`);
                }
                return; // Không chạy code order details
            }
            // Code gốc cho order details nếu delayMode off
            const uid = info.event.extendedProps.uid;
            const customerCode = info.event.extendedProps.customerCode || info.event.resourceId || 'Unknown'; // Lấy CustomerCode từ extendedProps hoặc resourceId
            console.log('Extracted UID:', uid, 'CustomerCode:', customerCode); // DEBUG: Kiểm tra UID và CustomerCode
            if (!uid) {
                console.log('UID is undefined or null'); // DEBUG
                // FIX: Hiển thị lỗi trong modal thay vì alert
                const bodyEl = document.getElementById('orderDetailsBody');
                bodyEl.innerHTML = '<tr><td colspan="7" class="text-center text-danger">Lỗi: Không tìm thấy ID đơn hàng.</td></tr>';
                document.getElementById('orderDetailsTable').style.display = 'table';
                document.getElementById('modalLoading').style.display = 'none';
                const modal = new bootstrap.Modal(document.getElementById('orderDetailsModal'));
                modal.show();
                return;
            }
            // THÊM: Cập nhật modal title với CustomerCode in đậm
            const modalTitleEl = document.getElementById('orderDetailsModalLabel');
            modalTitleEl.innerHTML = `OrderDetail Information - Customer: <strong>${customerCode}</strong>`;
            // Hiển thị loading
            const loadingEl = document.getElementById('modalLoading');
            const tableEl = document.getElementById('orderDetailsTable');
            const bodyEl = document.getElementById('orderDetailsBody');
            loadingEl.style.display = 'block';
            tableEl.style.display = 'none';
            bodyEl.innerHTML = '';
            // Use server-generated URL template from the Razor view to respect app virtual path
            const fetchUrl = `${orderDetailsUrlTemplate}?orderId=${encodeURIComponent(uid)}`;
            console.log('Fetching from URL:', fetchUrl); // DEBUG: Log URL
            // AJAX gọi Controller để lấy OrderDetails
            fetch(fetchUrl)
                .then(response => {
                    console.log('Fetch response status:', response.status); // DEBUG: Kiểm tra status
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }
                    return response.json();
                })
                .then(data => {
                    console.log('Fetch data received:', data); // DEBUG: Log data
                    console.log('JSON data sample:', data.data[0]); // DEBUG: Log item đầu tiên để xem keys
                    loadingEl.style.display = 'none';
                    if (data.success && data.data && data.data.length > 0) {
                        // Build table rows - HỖ TRỢ PROGRESS BARS VÀ ICONS
                        data.data.forEach(item => {
                            // Hỗ trợ cả Pascal và camel (lấy giá trị đầu tiên tồn tại)
                            const statusText = item.Status || item.status || 'Không xác định';
                            const collectPercent = item.CollectPercent || item.collectPercent || 0;
                            const preparePercent = item.PreparePercent || item.preparePercent || 0;
                            const loadingPercent = item.LoadingPercent || item.loadingPercent || 0;
                            const row = `
                                <tr>
                                    <td>${item.PartNo || item.partNo || 'N/A'}</td>
                                    <td>${item.Quantity || item.quantity || 0}</td>
                                    <td>${item.TotalPallet || item.totalPallet || 0}</td>
                                    <td>${item.PalletSize || item.palletSize || 'N/A'}</td>
                                    <td>${item.Warehouse || item.warehouse || 'N/A'}</td>
                                    <td>${item.ContNo || item.contNo || 'N/A'}</td>
                                    <td class="status-cell">
                                        <span class="badge bg-secondary">${statusText}</span>
                                        <div class="progress-info mt-2">
                                            <!-- Collect Stage -->
                                            <div class="d-flex align-items-center mb-2">
                                                <i class="bi bi-basket-fill text-primary me-2"></i>
                                                <span class="me-auto">Collect</span>
                                                <small class="text-muted">${collectPercent}%</small>
                                            </div>
                                            <div class="progress" style="height: 8px;">
                                                <div class="progress-bar bg-primary" style="width: ${collectPercent}%"></div>
                                            </div>
                                   
                                            <!-- Prepare Stage -->
                                            <div class="d-flex align-items-center mb-2 mt-2">
                                                <i class="bi bi-tools text-success me-2"></i>
                                                <span class="me-auto">Prepare</span>
                                                <small class="text-muted">${preparePercent}%</small>
                                            </div>
                                            <div class="progress" style="height: 8px;">
                                                <div class="progress-bar bg-success" style="width: ${preparePercent}%"></div>
                                            </div>
                                            <!--Loading Stage -->
                                            <div class="d-flex align-items-center mb-2 mt-2">
                                                <i class="bi bi-truck text-warning me-2"></i>
                                                <span class="me-auto">Loading</span>
                                                <small class="text-muted">${loadingPercent}%</small>
                                            </div>
                                            <div class="progress" style="height: 8px;">
                                                <div class="progress-bar bg-warning" style="width: ${loadingPercent}%"></div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            `;
                            bodyEl.innerHTML += row;
                        });
                        tableEl.style.display = 'table';
                    } else if (data.success) {
                        // FIX: Xử lý cụ thể nếu success=true nhưng data rỗng
                        console.log('Success but no data - possible DB issue');
                        bodyEl.innerHTML = '<tr><td colspan="7" class="text-center">Không có đơn hàng chi tiết</td></tr>';
                        tableEl.style.display = 'table';
                    } else {
                        // FIX: Hiển thị lỗi từ backend trong modal
                        console.log('Backend error:', data.message);
                        bodyEl.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Lỗi: ${data.message || 'Không xác định'}</td></tr>`;
                        tableEl.style.display = 'table';
                    }
                    // Mở modal
                    console.log('Attempting to show modal'); // DEBUG
                    const modalElement = document.getElementById('orderDetailsModal');
                    console.log('Modal element:', modalElement); // DEBUG
                    if (modalElement) {
                        const modal = new bootstrap.Modal(modalElement);
                        modal.show();
                        console.log('Modal shown'); // DEBUG
                    } else {
                        console.error('Modal element not found!'); // DEBUG
                    }
                })
                .catch(error => {
                    console.error('Fetch error:', error); // DEBUG: Log error chi tiết
                    loadingEl.style.display = 'none';
                    bodyEl.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Lỗi kết nối: ${error.message}</td></tr>`;
                    tableEl.style.display = 'table';
                    const modal = new bootstrap.Modal(document.getElementById('orderDetailsModal'));
                    modal.show();
                });
        },
        eventContent: function (arg) {
            // FIX MỚI: Tính % relative đến VISIBLE PART của event (intersection với current slots) để tránh lệch khi pan/scroll
            // Lấy status từ extendedProps
            const status = arg.event.extendedProps.status;
            const extendedProps = arg.event.extendedProps;
            const eventStart = arg.event.start; // Full DB start
            const eventEnd = arg.event.end; // Full DB end
            // Parse từ ISO
            const pStart = extendedProps.planStart ? new Date(extendedProps.planStart) : null;
            const pEnd = extendedProps.planEnd ? new Date(extendedProps.planEnd) : null;
            const aStart = extendedProps.actualStart ? new Date(extendedProps.actualStart) : null;
            const aEnd = extendedProps.actualEnd ? new Date(extendedProps.actualEnd) : null;
            // THÊM MỚI: Tính current view slots (local BKK) để clip visible
            const now = new Date(); // Current date (same day as view)
            const slotMinStr = calendar.getOption('slotMinTime'); // e.g., '07:00:00'
            const slotMaxStr = calendar.getOption('slotMaxTime'); // e.g., '24:00:00'
            const [minH, minM, minS] = slotMinStr.split(':').map(Number);
            const [maxH, maxM, maxS] = slotMaxStr.split(':').map(Number);
            const viewStart = new Date(now.getFullYear(), now.getMonth(), now.getDate(), minH, minM, minS);
            const viewEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), maxH, maxM, maxS);
            // Visible intersection của event với view
            const visibleStart = new Date(Math.max(eventStart, viewStart));
            const visibleEnd = new Date(Math.min(eventEnd, viewEnd));
            if (visibleEnd <= visibleStart) return { html: '<div>Invalid duration</div>' }; // Không overlap view
            const visibleDuration = visibleEnd.getTime() - visibleStart.getTime(); // ms visible
            let actualPercent = 0, actualWidth = 0;
            let planPercent = 0, planWidth = 0;
            // Tính % cho actual relative đến visibleDuration (chính xác, không lệch DB)
            if (aStart && aEnd && aEnd > visibleStart) {
                const actualVisibleStart = new Date(Math.max(aStart, visibleStart));
                const actualVisibleEnd = new Date(Math.min(aEnd, visibleEnd));
                if (actualVisibleStart < actualVisibleEnd) {
                    actualPercent = ((actualVisibleStart.getTime() - visibleStart.getTime()) / visibleDuration) * 100;
                    actualWidth = ((actualVisibleEnd.getTime() - actualVisibleStart.getTime()) / visibleDuration) * 100;
                }
            }
            // Tương tự cho plan
            if (pStart && pEnd && pEnd > visibleStart) {
                const planVisibleStart = new Date(Math.max(pStart, visibleStart));
                const planVisibleEnd = new Date(Math.min(pEnd, visibleEnd));
                if (planVisibleStart < planVisibleEnd) {
                    planPercent = ((planVisibleStart.getTime() - visibleStart.getTime()) / visibleDuration) * 100;
                    planWidth = ((planVisibleEnd.getTime() - planVisibleStart.getTime()) / visibleDuration) * 100;
                }
            }
            // Debug log cho rendering (xóa sau khi test OK) - LOCAL BKK + VISIBLE
            console.log(`Event ${extendedProps.uid}: Visible ${hhmmss(visibleStart)}-${hhmmss(visibleEnd)} | Actual %: ${actualPercent.toFixed(1)}-${(actualPercent + actualWidth).toFixed(1)} | Plan %: ${planPercent.toFixed(1)}-${(planPercent + planWidth).toFixed(1)} (BKK)`);
            // THÊM: Delay bar info nếu Delay - relative đến visible
            const dStart = extendedProps.delayStart ? new Date(extendedProps.delayStart) : null;
            const dEnd = extendedProps.delayEnd ? new Date(extendedProps.delayEnd) : null;
            let delayLeftPercent = 0, delayWidthPercent = 0;
            if (status === 'Delay' && dStart && dEnd) {
                const delayVisibleStart = new Date(Math.max(dStart, visibleStart));
                const delayVisibleEnd = new Date(Math.min(dEnd, visibleEnd));
                if (delayVisibleStart < delayVisibleEnd) {
                    delayLeftPercent = ((delayVisibleStart.getTime() - visibleStart.getTime()) / visibleDuration) * 100;
                    delayWidthPercent = ((delayVisibleEnd.getTime() - delayVisibleStart.getTime()) / visibleDuration) * 100;
                } else if (dStart >= visibleEnd) {
                    // Extend right nếu delay future > visibleEnd (left=100%, width dựa trên delayTime)
                    delayLeftPercent = 100;
                    const visibleHours = visibleDuration / (1000 * 60 * 60);
                    delayWidthPercent = (extendedProps.delayTime / visibleHours) * 100;
                }
                console.log('Delay bar positioned BKK (visible):', { left: delayLeftPercent.toFixed(1), width: delayWidthPercent.toFixed(1), isExtend: delayWidthPercent > 100 });
            }
            return {
                domNodes: [
                    (() => {
                        const wrapper = document.createElement('div');
                        // SỬA: Background fallback cho Delay (dùng màu cũ)
                        const fallbackColor = getColorByStatus(status, extendedProps.validActual);
                        wrapper.style.position = 'relative';
                        wrapper.style.height = '100%'; // Đồng bộ với height của .fc-event (80px)
                        wrapper.style.width = '100%';
                        wrapper.style.background = arg.event.backgroundColor || fallbackColor || 'transparent';
                        wrapper.style.borderRadius = '4px';
                        wrapper.style.overflow = 'visible'; // Cho phép extend visible
                        wrapper.style.color = arg.event.textColor;
                        wrapper.style.display = 'flex'; // Flex row để thẳng hàng
                        wrapper.style.flexDirection = 'row';
                        wrapper.style.alignItems = 'center';
                        wrapper.style.justifyContent = 'space-around'; // Phân bố đều các phần
                        wrapper.style.padding = '2px'; // Thêm padding nhẹ để không sát mép
                        // Vẽ plan bar - giữ nguyên kích thước ban đầu, relative visible
                        if (pStart && pEnd) {
                            if (aStart && aEnd) {
                                // Nếu có both: vẽ plan overlay (top 15%, height 70%) - GIỮ NGUYÊN
                                const planBar = document.createElement('div');
                                planBar.classList.add('custom-gradient-box');
                                planBar.style.position = 'absolute';
                                planBar.style.left = Math.max(0, planPercent) + '%'; // FIX: Dùng planPercent visible
                                planBar.style.top = '10%'; // Raise slightly to visually center with increased height
                                planBar.style.height = '80%'; // Tăng chiều cao của plan block từ 70% lên 80%
                                planBar.style.width = planWidth + '%'; // FIX: Dùng planWidth visible
                                //planBar.style.background = getColorByStatus('Planned'); // Đen cho plan overlay
                                planBar.style.borderRadius = '2px';
                                // planBar.title = `Full Plan BKK: ${hhmmss(pStart)} - ${hhmmss(pEnd)}`; // SỬA: Local BKK
                                wrapper.appendChild(planBar);
                            } else {
                                // Nếu chỉ plan: vẽ plan bar with same visual sizing as the overlay used when both plan+actual exist
                                // SỬA MỚI: Set wrapper transparent để không có viền xám nhạt bao quanh
                                wrapper.style.background = 'transparent'; // ← FIX: Loại bỏ fallback xám, chỉ giữ gradient của planBarFull
                                const planBarFull = document.createElement('div');
                                planBarFull.classList.add('custom-gradient-box');
                                planBarFull.style.position = 'absolute';
                                planBarFull.style.left = Math.max(0, planPercent) + '%'; // FIX: Dùng planPercent visible
                                // Match the overlay sizing used above (top 15%, height 70%) so visual size is consistent
                                planBarFull.style.top = '10%';
                                planBarFull.style.height = '80%';
                                planBarFull.style.width = '100%'; // ← FIX: Đổi từ planWidth + '%' thành '100%' để lấp đầy wrapper (giống hệt overlay của both, không khoảng trống)
                                // Use planned color (fallback) for bar to keep consistency - NHƯNG GIỮ GRADIENT TỪ CSS .custom-gradient-box (đen gradient)
                                // Không cần set background vì class đã có gradient đen giống plan overlay
                                planBarFull.style.borderRadius = '2px';
                                //planBarFull.title = `Full Plan BKK: ${hhmmss(pStart)} - ${hhmmss(pEnd)}`;
                                wrapper.appendChild(planBarFull);
                            }
                        }
                        // Vẽ actual bar (nếu có) - đè lên plan với mờ và opacity, giữ full height, relative visible
                        if (aStart && aEnd) {
                            const actualBar = document.createElement('div');
                            actualBar.style.position = 'absolute';
                            actualBar.style.left = Math.max(0, actualPercent) + '%'; // FIX: Dùng actualPercent visible
                            actualBar.style.top = '0';
                            actualBar.style.height = '100%';
                            actualBar.style.width = actualWidth + '%'; // FIX: Dùng actualWidth visible
                            actualBar.style.background = fallbackColor; // SỬA: Fallback cho actual nếu Delay
                            actualBar.style.borderRadius = '4px';
                            if (pStart && pEnd) { // Mờ nếu có plan
                                actualBar.style.filter = 'blur(1px)';
                                // Tăng opacity để actual bar rõ ràng hơn
                                actualBar.style.opacity = '0.55';
                            } else {
                                // Nếu không có plan, ensure actual is fully opaque
                                actualBar.style.opacity = '0.55';
                            }
                            actualBar.title = `Actual BKK: ${hhmmss(aStart)} - ${hhmmss(aEnd)}`; // SỬA: Local BKK
                            wrapper.appendChild(actualBar);
                        }
                        // THÊM: Vẽ delay bar nối tiếp nếu Delay (từ max(visibleEnd, delayStart) đến delayEnd, extend ra phải nếu >100%)
                        if (status === 'Delay' && dStart && dEnd) {
                            const delayBar = document.createElement('div');
                            delayBar.className = 'delay-bar';
                            delayBar.style.left = Math.max(0, delayLeftPercent) + '%'; // Min 0 để không âm, dùng visible
                            delayBar.style.width = Math.max(0, delayWidthPercent) + '%'; // Có thể >100% để extend, dùng visible
                            delayBar.style.background = '#ff0000';
                            delayBar.style.right = 'auto'; // Để left calc đúng
                            delayBar.title = `Delay BKK: ${hhmmss(dStart)} - ${hhmmss(dEnd)} (${extendedProps.delayTime}h)`; // SỬA: Local BKK
                            wrapper.appendChild(delayBar);
                            // Nếu width >100%, thêm pseudo-element để extend visual (optional, nếu CSS hỗ trợ)
                            if (delayWidthPercent > 100) {
                                delayBar.style.position = 'absolute';
                                delayBar.style.clipPath = 'none'; // Đảm bảo không clip
                            }
                        }
                        (function createCombinedBlock() {
                            // ... (giữ nguyên code createCombinedBlock từ mã trước)
                            // Helper: extract first numeric value from strings like "10 / 20" or "50%"
                            function extractFirstNumber(val) {
                                if (val === null || val === undefined) return '0';
                                const s = String(val);
                                const m = s.match(/(\d+(?:\.\d+)?)/);
                                return m ? m[1] : '0';
                            }
                            // Compute desired pixel width for 2.5 hours based on timeline hour cell width
                            const twoAndHalfHours = 2.5; // hours
                            function computeDesiredPx() {
                                const hourCells = getHeaderHourCells();
                                if (hourCells && hourCells.length > 0) {
                                    const hourW = hourCells[0].getBoundingClientRect().width || 0;
                                    // Increase the previous halved target slightly so the fixed width becomes ~75% of 2.5 hours
                                    return Math.round(hourW * twoAndHalfHours * 0.75);
                                }
                                // fallback: approximate using wrapper width and eventDuration
                                const wrapperW = wrapper.getBoundingClientRect().width || 0;
                                if (visibleDuration > 0) {
                                    // convert 2.5 hours to fraction of visible duration, then to px
                                    const frac = (twoAndHalfHours * 60 * 60 * 1000) / visibleDuration;
                                    // increase the computed pixel target slightly as well
                                    return Math.round(Math.min(wrapperW, Math.max(40, wrapperW * frac * 0.75)));
                                }
                                return 80; // safe default px (increased)
                            }
                            const desiredPx = computeDesiredPx();
                            const combined = document.createElement('div');
                            combined.className = 'combined-block';
                            // Single background for the whole block - use soft pink for contrast on white
                            combined.style.background = '#F19C99';
                            combined.style.border = '1px solid #ffdbe6';
                            combined.style.boxShadow = 'inset 0 1px 0 rgba(255,255,255,0.6)';
                            combined.style.borderRadius = '6px';
                            combined.style.display = 'flex';
                            combined.style.alignItems = 'center';
                            combined.style.gap = '4px';
                            combined.style.padding = '4px';
                            // Make combined block visually shorter than the plan bar
                            combined.style.height = '60%';
                            combined.style.alignSelf = 'center';
                            combined.style.boxSizing = 'border-box';
                            combined.style.position = 'relative';
                            combined.style.zIndex = '2';
                            // Position combined BLOCK: make its width equal to the measured label width
                            // (so combined visually equals `.combined-label`) and center it inside
                            // the PLAN segment when available. Use absolute positioning and recompute
                            // on resize to keep it centered responsively.
                            combined.style.position = 'absolute';
                            combined.style.zIndex = '4';
                            combined.style.top = '20%';
                            combined.style.height = '60%';
                            function positionCombinedByLabel() {
                                try {
                                    const wrapperWNow = wrapper.getBoundingClientRect().width || 0;
                                    // measure the label natural width (no wrapping)
                                    // ensure label uses natural width for measurement
                                    txtContent.style.whiteSpace = 'nowrap';
                                    txtContent.style.display = 'inline-block';
                                    // small padding to avoid tight clipping
                                    const labelW = Math.ceil(txtContent.scrollWidth) + 12;
                                    // clamp to wrapper width
                                    const finalW = Math.min(labelW, Math.max(24, wrapperWNow));
                                    combined.style.width = finalW + 'px';
                                    combined.style.flex = '0 0 ' + finalW + 'px';
                                    // Compute plan segment pixel width/left (dùng visible %)
                                    const planLeftPx = wrapperWNow * (planPercent / 100);
                                    const planWidthPx = wrapperWNow * (planWidth / 100);
                                    let leftPx = 0;
                                    if (planWidthPx > 4) {
                                        // center inside plan segment (preferably)
                                        leftPx = planLeftPx + Math.max(0, (planWidthPx - finalW) / 2);
                                    } else {
                                        // fallback: center inside full event area
                                        leftPx = Math.max(0, (wrapperWNow - finalW) / 2);
                                    }
                                    // Clamp within wrapper
                                    leftPx = Math.max(0, Math.min(leftPx, Math.max(0, wrapperWNow - finalW)));
                                    combined.style.left = leftPx + 'px';
                                } catch (err) {
                                    // best-effort fallback
                                }
                            }
                            // Initial position after paint
                            setTimeout(positionCombinedByLabel, 0);
                            // Responsive label: scales font-size and uses ellipsis if event too narrow
                            const txtContent = document.createElement('div');
                            txtContent.className = 'combined-label';
                            // Flexible so it will shrink when combined block is small
                            txtContent.style.flex = '1 1 auto';
                            txtContent.style.width = '100%';
                            txtContent.style.display = 'block';
                            txtContent.style.textAlign = 'center';
                            txtContent.style.alignSelf = 'end';
                            txtContent.style.overflow = 'hidden';
                            txtContent.style.textOverflow = 'ellipsis';
                            txtContent.style.whiteSpace = 'nowrap';
                            txtContent.style.boxSizing = 'border-box';
                            txtContent.style.padding = '0 6px';
                            txtContent.style.fontSize = '1.2rem'; // initial; will be adjusted (bigger)
                            txtContent.style.fontWeight = '700';
                            txtContent.style.color = 'darkblue';
                            txtContent.textContent = extendedProps.customerCode + " " + `(${extendedProps.totalPallet || 0})` + " - " + extractFirstNumber(extendedProps.collectPallet) +
                                " | " + extractFirstNumber(extendedProps.threePointScan) + " | " + extractFirstNumber(extendedProps.loadCont);
                            // Helper: fit text to available width by decreasing font-size until it fits (simple, robust)
                            function fitTextToWidth(el, container, minFontPx = 10, maxFontPx = 18) {
                                try {
                                    // Reset to max to compute natural width
                                    el.style.fontSize = maxFontPx + 'px';
                                    // small padding allowance
                                    const avail = Math.max(8, container.getBoundingClientRect().width - 8);
                                    let fs = parseFloat(getComputedStyle(el).fontSize) || maxFontPx;
                                    // If scrollWidth fits, done
                                    // Use a couple of iterations decreasing by 1px to avoid heavy layout thrash
                                    let iterations = 0;
                                    while (el.scrollWidth > avail && fs > minFontPx && iterations < 20) {
                                        fs = Math.max(minFontPx, fs - 1);
                                        el.style.fontSize = fs + 'px';
                                        iterations++;
                                    }
                                } catch (e) {
                                    // ignore measurement errors
                                }
                            }
                            // Observe size changes on the combined block to adjust text responsively
                            try {
                                const ro = new ResizeObserver(() => {
                                    fitTextToWidth(txtContent, combined, 10, 18);
                                });
                                ro.observe(combined);
                                // Try initial fit after next paint
                                setTimeout(() => fitTextToWidth(txtContent, combined, 10, 18), 0);
                            } catch (err) {
                                // ResizeObserver may not be available in very old browsers; fallback to a one-time fit
                                setTimeout(() => fitTextToWidth(txtContent, combined, 10, 18), 0);
                            }
                            // Left: CustomerCode + TotalPallet
                            //const left = document.createElement('div');
                            //left.style.display = 'flex';
                            //left.style.flex = '0 0 auto';
                            //left.style.alignItems = 'center';
                            //left.style.gap = '8px';
                            //left.style.padding = '2px 4px';
                            //const cust = document.createElement('div');
                            //cust.textContent = extendedProps.customerCode || 'N/A';
                            //cust.style.fontWeight = '700';
                            //cust.style.color = '#111';
                            //cust.style.whiteSpace = 'nowrap';
                            //const totalP = document.createElement('div');
                            //totalP.textContent = `(${extendedProps.totalPallet || 0})`;
                            //totalP.style.fontWeight = '600';
                            //totalP.style.color = '#333';
                            //totalP.style.whiteSpace = 'nowrap';
                            //left.appendChild(cust);
                            //left.appendChild(totalP);
                            //// Right: progress items in a single group; each shows only the actual numeric value
                            //const progressGroup = document.createElement('div');
                            //progressGroup.style.display = 'flex';
                            //progressGroup.style.flex = '1 1 auto';
                            //progressGroup.style.alignItems = 'center';
                            //progressGroup.style.justifyContent = 'space-between';
                            //progressGroup.style.gap = '6px';
                            //progressGroup.style.overflow = 'hidden';
                            //// Create a small helper to create simple progress badge
                            //function makeBadge(text) {
                            // const b = document.createElement('div');
                            // b.textContent = text;
                            // b.style.flex = '0 1 auto';
                            // b.style.padding = '2px 6px';
                            // b.style.borderRadius = '4px';
                            // b.style.background = 'transparent';
                            // b.style.color = '#111';
                            // b.style.fontWeight = '700';
                            // b.style.whiteSpace = 'nowrap';
                            // b.style.textAlign = 'center';
                            // b.style.overflow = 'hidden';
                            // b.style.textOverflow = 'ellipsis';
                            // return b;
                            //}
                            //const collectNum = extractFirstNumber(extendedProps.collectPallet);
                            //const threeNum = extractFirstNumber(extendedProps.threePointScan);
                            //const loadNum = extractFirstNumber(extendedProps.loadCont);
                            //const collectBadge = makeBadge(collectNum);
                            //const threeBadge = makeBadge(threeNum);
                            //const loadBadge = makeBadge(loadNum);
                            //progressGroup.appendChild(collectBadge);
                            //progressGroup.appendChild(threeBadge);
                            //progressGroup.appendChild(loadBadge);
                            //combined.appendChild(left);
                            //combined.appendChild(progressGroup);
                            combined.appendChild(txtContent);
                            // Set initial font sizes (large but not excessive) so text appears prominent
                            try {
                                const initialH = Math.max(1, wrapper.getBoundingClientRect().height);
                                // Reduce font sizes: smaller multiplier and lower max to make text less dominant
                                const fsInit = Math.max(12, Math.min(22, Math.floor(initialH * 0.28)));
                                //cust.style.fontSize = fsInit + 'px';
                                //totalP.style.fontSize = Math.max(12, fsInit - 2) + 'px';
                                //collectBadge.style.fontSize = fsInit + 'px';
                                //threeBadge.style.fontSize = fsInit + 'px';
                                //loadBadge.style.fontSize = fsInit + 'px';
                            } catch (e) {
                                // ignore
                            }
                            // Ensure responsive font sizing / adapt when parent changes size (very large range)
                            try {
                                const ro = new ResizeObserver(() => {
                                    // adjust font size relative to height (if needed)
                                    const h = Math.max(1, wrapper.getBoundingClientRect().height);
                                    const fs = Math.max(12, Math.min(22, Math.floor(h * 0.28)));
                                    // apply to label (if desired) - keep boldness but smaller sizes handled by fitTextToWidth
                                    txtContent.style.fontSize = Math.max(12, Math.min(18, fs)) + 'px';
                                    // Reposition & resize combined to match label width when wrapper changes
                                    positionCombinedByLabel();
                                });
                                ro.observe(wrapper);
                            } catch (err) {
                                console.warn('ResizeObserver not supported or failed:', err);
                            }
                            // Push progress badges to the right edge of the combined block while CustomerCode stays left
                            try {
                                //progressGroup.style.marginLeft = 'auto';
                                //progressGroup.style.justifyContent = 'flex-end';
                            } catch (err) {
                                // ignore
                            }
                            wrapper.appendChild(combined);
                        })();
                        // THÊM: Hover listener cho effect (đã có CSS, nhưng thêm sound subtle nếu delayMode)
                        wrapper.addEventListener('mouseenter', (e) => {
                            if (delayMode) {
                                // Optional: Play sound nhẹ khi hover (giảm volume tạm thời)
                                playBeep(0.2, 600, 100); // Beep nhẹ hơn cho hover
                            }
                            // Cập nhật nội dung tooltip với tất cả thông tin - SỬA SANG LOCAL BKK + THÊM: Tách riêng PlanTime và ActualTime + luôn hiển thị ngày + giờ
                            const now = new Date(); // Giờ local BKK hiện tại để check past
                            // For plan and actual time: always show dd/mm HH:MM (no year) for readability
                            // If there's no actual end time, show only the actual start so users know when execution began
                            const planTime = (pStart && pEnd) ? (formatDateTimeNoYear(pStart) + ' - ' + formatDateTimeNoYear(pEnd)) : 'N/A';
                            let actualLabel = 'Actual Time:';
                            let actualDisplay = 'N/A';
                            // Only show actual end time when we consider it 'final'.
                            // Treat 'Completed' and 'Shipped' as final states; otherwise display only the Actual Start.
                            if (aStart && aEnd && (status === 'Completed' || status === 'Shipped')) {
                                actualDisplay = formatDateTimeNoYear(aStart) + ' - ' + formatDateTimeNoYear(aEnd);
                            } else if (aStart) {
                                actualLabel = 'Actual Start:'; // show only start time while end time is still changing
                                actualDisplay = formatDateTimeNoYear(aStart);
                            }
                            const delayTimeRange = formatTimeRange(dStart, dEnd, now);
                            const tooltip = createTooltip();
                            tooltip.innerHTML = `
                                <h4>Order Information</h4>
                                <dl>
                                    <dt>ShipDate:</dt>
                                    <dd>${extendedProps.shipDate}</dd>
                                  <dt>Plan Time:</dt>
                                    <dd class="plan-time-dd">${planTime}</dd>
                                    <dt>${actualLabel}</dt>
                                    <dd class="actual-time-dd">${actualDisplay}</dd>
                                    ${status === 'Delay' ? `<dt>Delay Time (BKK):</dt><dd>${delayTimeRange}</dd>` : ''}
                                    <dt>TransCD:</dt>
                                    <dd>${extendedProps.transCd}</dd>
                                    <dt>TransMethod:</dt>
                                    <dd>${extendedProps.transMethod}</dd>
                                    <dt>ContSize:</dt>
                                    <dd>${extendedProps.contSize}</dd>
                                    <dt>Total Column:</dt>
                                    <dd>${extendedProps.totalColumn}</dd>
                                    <dt>Total Pallet:</dt>
                                    <dd>${extendedProps.totalPallet}</dd>
                                    <dt>Status:</dt>
                                    <dd><span class="status ${status.toLowerCase()}">${status}</span></dd>
                                </dl>
                            `;
                            // Position tooltip dưới event với responsive logic
                            const rect = e.currentTarget.getBoundingClientRect();
                            tooltip.classList.add('show'); // Hiển thị để tính toán kích thước
                            // Tính toán vị trí ban đầu
                            let left = rect.left + window.scrollX;
                            let top = rect.bottom + window.scrollY + 5;
                            // Đặt vị trí tạm thời để lấy kích thước thực
                            tooltip.style.left = left + 'px';
                            tooltip.style.top = top + 'px';
                            // Lấy kích thước tooltip sau khi render
                            const tooltipRect = tooltip.getBoundingClientRect();
                            const windowWidth = window.innerWidth;
                            const windowHeight = window.innerHeight;
                            // Kiểm tra tràn bên phải
                            if (tooltipRect.right > windowWidth) {
                                left = windowWidth - tooltipRect.width - 10; // 10px padding
                                if (left < 10) left = 10; // Tối thiểu 10px từ mép trái
                            }
                            // Kiểm tra tràn bên trái
                            if (tooltipRect.left < 0) {
                                left = 10;
                            }
                            // Kiểm tra tràn phía dưới
                            if (tooltipRect.bottom > windowHeight) {
                                // Hiển thị phía trên event thay vì dưới
                                top = rect.top + window.scrollY - tooltipRect.height - 5;
                            }
                            // Kiểm tra tràn phía trên (nếu đã đổi lên trên)
                            if (top < window.scrollY) {
                                top = window.scrollY + 10;
                            }
                            // Áp dụng vị trí cuối cùng
                            tooltip.style.left = left + 'px';
                            tooltip.style.top = top + 'px';
                        });
                        wrapper.addEventListener('mouseleave', () => {
                            // Ẩn tooltip
                            const tooltip = document.getElementById('custom-tooltip');
                            tooltip.classList.remove('show');
                        });
                        // Optional: Follow mouse nếu muốn, nhưng giữ fixed dưới event cho đơn giản
                        // wrapper.addEventListener('mousemove', (e) => {
                        // tooltip.style.left = (e.pageX + 10) + 'px';
                        // tooltip.style.top = (e.pageY - 10) + 'px';
                        // });
                        return wrapper;
                    })()
                ]
            };
        }
    });
    calendar.render();
    // --- THÊM MỚI: Horizontal pan/scroll to shift visible time window (view past hours)
    // This allows the user to scroll horizontally (wheel or drag) to move the slot window
    // left/right by hour steps without changing other FullCalendar config.
    // FIX MỚI: Tăng độ mượt mà - Sử dụng requestAnimationFrame + throttle + tăng PIXELS_PER_HOUR + debounce applyPan để tránh khựng khi scroll về quá khứ
    (function enableHorizontalPan() {
        // Base values from initial timeRange
        const baseStartParts = timeRange.startHour.split(':').map(Number);
        const baseStartHourNum = baseStartParts[0] + (baseStartParts[1] / 60);
        const viewHours = (timeRange.end - timeRange.start) / (1000 * 60 * 60); // window size in hours
        let panOffset = 0; // hours shifted from baseStartHourNum (can be negative)
        function clamp(val, min, max) { return Math.max(min, Math.min(max, val)); }
        function formatHHMMSS(hourFloat) {
            // hourFloat may be fractional; convert to HH:MM:SS
            let h = Math.floor(hourFloat);
            let m = Math.round((hourFloat - h) * 60);
            if (m === 60) { h += 1; m = 0; }
            h = clamp(h, 0, 24);
            const hh = String(h).padStart(2, '0');
            const mm = String(m).padStart(2, '0');
            return `${hh}:${mm}:00`;
        }
        // THÊM MỚI: Debounce function để delay applyPan sau khi ngừng scroll (giảm re-render)
        function debounce(fn, delay) {
            let timeoutId;
            return function (...args) {
                clearTimeout(timeoutId);
                timeoutId = setTimeout(() => fn.apply(this, args), delay);
            };
        }
        // THÊM MỚI: Throttled version của applyPan để gọi trong rAF
        let applyPanQueued = false;
        function applyPanThrottled(offsetHours) {
            if (applyPanQueued) return;
            applyPanQueued = true;
            requestAnimationFrame(() => {
                applyPan(offsetHours);
                applyPanQueued = false;
            });
        }
        // THÊM MỚI: Debounced applyPan (gọi sau 150ms ngừng scroll để mượt)
        const debouncedApplyPan = debounce((offsetHours) => {
            applyPan(offsetHours);
        }, 150); // Delay 150ms sau khi ngừng scroll
        function applyPan(offsetHours) {
            panOffset = offsetHours;
            // compute allowed start range
            const minStart = 0;
            const maxStart = Math.max(0, 24 - viewHours);
            // Raw start (may be fractional) then snap to nearest whole hour
            const rawStart = baseStartHourNum + panOffset;
            const snappedStart = Math.round(rawStart); // snap to nearest integer hour
            const newStart = clamp(snappedStart, minStart, maxStart);
            const newEnd = newStart + viewHours;
            const newStartStr = formatHHMMSS(newStart);
            const newEndStr = formatHHMMSS(newEnd);
            try {
                calendar.setOption('slotMinTime', newStartStr);
                calendar.setOption('slotMaxTime', newEndStr);
                // Align scrollTime to the left edge (start) so visible timeline shows round hour
                const scrollStr = formatHHMMSS(newStart);
                calendar.setOption('scrollTime', scrollStr);
                // update global timeRange so other computations use updated values
                timeRange = getTimeRange();
            } catch (err) {
                console.error('Failed to apply horizontal pan:', err);
            }
        }
        // Wheel handling: accumulate pixels to convert to hour steps - THÊM: Throttle wheelAcc và dùng debouncedApplyPan
        let wheelAcc = 0;
        const PIXELS_PER_HOUR = 120; // TĂNG TỪ 80 LÊN 120 để giảm tần suất cập nhật (mượt hơn khi scroll nhanh về quá khứ)
        let lastWheelTime = 0;
        const wheelThrottleDelay = 50; // Throttle wheel events mỗi 50ms
        calendarEl.addEventListener('wheel', function (ev) {
            // Only allow horizontal pan when scrollMode is active (angry face)
            if (!scrollMode) return; // do nothing, allow default scrolling
            // If user scrolls vertically normally, don't hijack unless Shift pressed or horizontal delta present
            const horiz = Math.abs(ev.deltaX) > Math.abs(ev.deltaY) ? ev.deltaX : (ev.shiftKey ? ev.deltaY : 0);
            if (!horiz) return; // not a horizontal intent
            ev.preventDefault();
            const nowTime = Date.now();
            if (nowTime - lastWheelTime < wheelThrottleDelay) return; // Throttle wheel events
            lastWheelTime = nowTime;
            wheelAcc += horiz;
            const hoursDelta = Math.trunc(wheelAcc / PIXELS_PER_HOUR);
            if (hoursDelta !== 0) {
                wheelAcc -= hoursDelta * PIXELS_PER_HOUR;
                // scrolling left (negative delta) should move view right (earlier times visible) depending on sign
                // delta positive usually means scroll right → move view later
                const newOffset = clamp(panOffset + hoursDelta * (horiz > 0 ? 1 : -1), -24, 24);
                applyPanThrottled(newOffset); // Throttled với rAF
                // Sử dụng debounced để finalize sau khi ngừng scroll
                debouncedApplyPan(newOffset);
            }
        }, { passive: false });
        // Pointer drag panning for click-and-drag behavior - THÊM: Throttle move events với rAF
        let isPanning = false;
        let panStartX = 0;
        let panStartOffset = 0;
        const PIXELS_FOR_ONE_HOUR_DRAG = 150; // TĂNG TỪ 120 LÊN 150 để mượt hơn (thay đổi chậm hơn)
        let dragThrottleRafId = null;
        calendarEl.addEventListener('pointerdown', function (ev) {
            // Only allow drag-pan when scrollMode is active (angry face)
            if (!scrollMode) return;
            // Only respond to primary button
            if (ev.button !== 0) return;
            // don't start pan if interacting with inputs/modals inside calendar
            if (ev.target.closest('.fc-event') || ev.target.closest('button') || ev.target.closest('a') || ev.target.closest('input')) return;
            isPanning = true;
            panStartX = ev.clientX;
            panStartOffset = panOffset;
            calendarEl.style.cursor = 'grabbing';
            ev.preventDefault();
        });
        window.addEventListener('pointermove', function (ev) {
            if (!isPanning) return;
            if (dragThrottleRafId) cancelAnimationFrame(dragThrottleRafId);
            dragThrottleRafId = requestAnimationFrame(() => {
                const dx = ev.clientX - panStartX;
                const deltaHours = dx / PIXELS_FOR_ONE_HOUR_DRAG;
                const newOffset = panStartOffset - deltaHours; // dragging right should show earlier times
                applyPanThrottled(clamp(newOffset, -24, 24)); // Throttled với rAF
                // Debounce finalize cho drag (delay ngắn hơn vì drag liên tục)
                debouncedApplyPan(clamp(newOffset, -24, 24));
            });
        });
        window.addEventListener('pointerup', function () {
            if (!isPanning) return;
            isPanning = false;
            calendarEl.style.cursor = '';
            if (dragThrottleRafId) {
                cancelAnimationFrame(dragThrottleRafId);
                dragThrottleRafId = null;
            }
            // Force applyPan cuối cùng sau drag
            setTimeout(() => applyPan(panOffset), 50);
        });
        // Optional: double-click to reset pan
        calendarEl.addEventListener('dblclick', function () {
            applyPan(0);
        });
    })();
    // --- THÊM MỚI: SignalR cho realtime status update (FIX: Cải thiện update logic - remove refetch/changeDate, chỉ render) - SỬA CLIP THEO LOCAL BKK + THÊM cap end + FIX: Lọc undefined resources/events + BỎ CLIPPING TRONG REFETCH
    let connection = null;
    function initSignalR() {
        // Kiểm tra SignalR mới có load chưa
        if (typeof signalR === 'undefined') {
            console.error('SignalR client not loaded. Please include signalr.js.');
            return;
        }
        // Tạo connection với Hub URL
        connection = new signalR.HubConnectionBuilder()
            .withUrl(window.appBaseUrl + 'orderHub') // URL hub từ backend (use appBaseUrl to respect virtual path)
            .configureLogging(signalR.LogLevel.Information) // Optional: Log level
            .build();
        // Start connection
        connection.start().then(function () {
            console.log('SignalR connected for realtime updates.');
        }).catch(function (err) {
            console.error('SignalR connection failed: ' + err.toString());
            // Optional: Retry sau 5s nếu fail
            setTimeout(() => connection.start(), 5000);
        });
        // Listen event OrderStatusUpdated từ backend (callback)
        connection.on('OrderStatusUpdated', function (orderUid, newStatus) {
            console.log(`Realtime update: Order ${orderUid} status changed to ${newStatus}`);
            console.log('SignalR refetch starting...'); // ← THÊM LOG BẮT ĐẦU
            // Refetch data từ server để update calendar
            fetch(window.appBaseUrl + 'DensoWareHouse/GetCalendarData')
                .then(response => {
                    console.log('SignalR response status:', response.status); // ← THÊM LOG STATUS
                    if (!response.ok) throw new Error('SignalR refetch failed');
                    return response.json();
                })
                .then(data => {
                    console.log('SignalR refetched data:', data); // ← THÊM LOG DATA RAW
                    // Log chi tiết từng order
                    if (data.orders) {
                        data.orders.forEach((order, idx) => {
                            console.log(`Order[${idx}]:`, order);
                        });
                    }
                    console.log('SignalR orders length:', data.orders ? data.orders.length : 0); // ← THÊM LOG LENGTH
                    const fetchedOrders = data.orders;
                    const fetchedCustomers = data.customers;
                    // THÊM: Đảm bảo resourceId của mọi event đều có trong resources - FIX: Lọc rid undefined
                    const allResourceIds = Array.from(new Set(fetchedOrders.map(o => o.resource).filter(rid => rid != null && rid !== '' && rid !== undefined))); // ← FIX: Lọc undefined
                    if (fetchedCustomers) {
                        const customerIds = fetchedCustomers.map(c => c.CustomerCode);
                        allResourceIds.forEach(rid => {
                            if (rid && !customerIds.includes(rid)) { // ← FIX: Thêm kiểm tra rid truthy
                                fetchedCustomers.push({ CustomerCode: rid, CustomerName: rid });
                            }
                        });
                    }
                    // Rebuild customerMap từ local
                    const customerMap = {};
                    fetchedCustomers.forEach(c => {
                        customerMap[c.CustomerCode] = c.CustomerName;
                    });
                    // Rebuild resources từ local - include customerName separately for two-line label
                    const resources = fetchedCustomers.map(c => ({
                        id: c.CustomerCode,
                        title: c.CustomerCode,
                        customerName: c.CustomerName || ''
                    })).filter(r => r.id != null && r.id !== '' && r.id !== undefined); // ← FIX: Lọc resources undefined
                    // Rebuild eventsData từ local (copy logic từ code gốc - với delay info + LOG/FALLBACK) - SỬA: BỎ CLIPPING ĐỂ GIỮ FULL DB RANGE
                    const newEventsData = fetchedOrders.map((order) => { // ← BỎ index, dùng UId cho id
                        const planStart = parseAndValidate(order.startTime);
                        const planEnd = parseAndValidate(order.endTime);
                        const actualStart = parseAndValidate(order.acStartTime);
                        const actualEnd = parseAndValidate(order.acEndTime);
                        // THÊM LOG: Check raw values
                        console.log(`SignalR Order ${order.uId}: planStart="${order.startTime}", planEnd="${order.endTime}", actualStart="${order.acStartTime}", actualEnd="${order.acEndTime}"`); // ← THÊM RAW TIMES
                        let validPlan = planStart && planEnd && planStart < planEnd;
                        let validActual = actualStart && actualEnd && actualStart < actualEnd;
                        // FIX: Fallback nếu chỉ có plan nhưng invalid (e.g., EndTime null sau delay) - dùng current time dummy
                        if (!validPlan && order.startTime && !order.endTime) { // Chỉ StartTime có, EndTime null
                            validPlan = true;
                            planEnd = new Date(planStart.getTime() + 60 * 60 * 1000); // Dummy 1h end
                            console.log(`SignalR Fallback dummy end for ${order.uId}: ${planEnd.toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' })}`); // ← THÊM LOG FALLBACK LOCAL
                        }
                        if (!validActual && order.acStartTime && !order.acEndTime) {
                            validActual = true;
                            actualEnd = new Date(actualStart.getTime() + 60 * 60 * 1000); // Dummy
                            console.log(`SignalR Fallback dummy actual end for ${order.uId}`); // ← THÊM LOG
                        }
                        let eventStart, eventEnd;
                        let status = statusMap[parseInt(order.status, 10)] || 'Planned';
                        console.log(`SignalR Rebuilding order ${order.uId}: Status=${order.status} (${status}), ValidPlan=${validPlan}, ValidActual=${validActual}`); // ← THÊM LOG REBUILD
                        if (validActual) {
                            eventStart = actualStart;
                            eventEnd = actualEnd;
                        } else if (validPlan) {
                            eventStart = planStart;
                            eventEnd = planEnd;
                        } else {
                            console.warn(`SignalR Skipping invalid order ${order.uId}: No valid times even after fallback`); // ← THÊM LOG SKIP
                            return null;
                        }
                        let hasBoth = false;
                        if (validActual && validPlan) {
                            eventStart = new Date(Math.min(actualStart, planStart));
                            eventEnd = new Date(Math.max(actualEnd, planEnd));
                            hasBoth = true;
                        }
                        // BỎ CLIPPING: Để full start/end từ DB, FullCalendar handle overlap
                        // Không cần: eventStart = Math.max(eventStart, slotMin); etc.
                        const customerCode = order.customerCode || order.resource || 'Unknown';
                        if (!customerCode || customerCode === 'undefined') return null; // ← FIX: Skip event nếu customerCode undefined/null/empty
                        // THÊM: Delay info nếu status=='Delay'
                        let delayStart = null, delayEnd = null, delayTime = 0;
                        if (status === 'Delay') {
                            delayStart = parseAndValidate(order.delayStartTime);
                            delayTime = parseFloat(order.delayTime) || 0;
                            if (delayStart) {
                                delayEnd = new Date(delayStart.getTime() + delayTime * 60 * 60 * 1000);
                            }
                        }
                        return {
                            id: `order-${order.uId}`, // ← SỬA: Dùng UId thay index để unique
                            resourceId: customerCode,
                            start: eventStart,
                            end: eventEnd,
                            title: '',
                            hasBoth: hasBoth,
                            extendedProps: {
                                uid: order.uId,
                                customerCode: customerCode,
                                planStart: validPlan ? planStart.toISOString() : null,
                                planEnd: validPlan ? planEnd.toISOString() : null,
                                actualStart: validActual ? actualStart.toISOString() : null,
                                actualEnd: validActual ? actualEnd.toISOString() : null,
                                validActual: validActual,
                                status: status,
                                totalPallet: order.totalPallet || 0,
                                collectPallet: order.collectPallet || '0 / 0',
                                threePointScan: order.threePointScan || '0 / 0',
                                loadCont: order.loadCont || '0 / 0',
                                shipDate: order.shipDate || 'N/A',
                                transCd: order.transCd || 'N/A',
                                transMethod: order.transMethod || 'N/A',
                                contSize: order.contSize || 'N/A',
                                totalColumn: order.totalColumn || 0,
                                // THÊM: Delay info
                                delayStart: delayStart ? delayStart.toISOString() : null,
                                delayEnd: delayEnd ? delayEnd.toISOString() : null,
                                delayTime: delayTime
                            }
                        };
                    }).filter(e => e);
                    console.log('SignalR newEventsData length:', newEventsData.length); // ← THÊM LOG LENGTH
                    if (newEventsData.length === 0) {
                        console.error('SignalR: No events after rebuild! Skipping update to avoid blank calendar.');
                        return; // ← THÊM: Không update nếu fail, tránh mất events (fallback manual sẽ chạy sau)
                    }
                    // Update calendar options và events (FIX: Clear sources đúng cách, chỉ render)
                    calendar.setOption('resources', resources);
                    // Log resources hiện tại
                    console.log('Current resources:', resources.map(r => r.id));
                    calendar.removeAllEventSources(); // ← GIỮ, clear tất cả sources cũ
                    const formattedEvents = newEventsData.map(e => {
                        const extendedProps = e.extendedProps;
                        const status = extendedProps.status;
                        const validActual = extendedProps.validActual;
                        const delayTime = extendedProps.delayTime || 0;
                        // Same logic as above for SignalR-updated events: make outer container
                        // transparent for plan-only (no actual) so only the inner plan bar is visible
                        // and the gray border/background disappears.
                        const bgColor = (e.hasBoth || !validActual) ? 'transparent' : getColorByStatus(status, validActual, delayTime);
                        const borderColor = (e.hasBoth || !validActual) ? 'transparent' : getColorByStatus(status, validActual, delayTime);
                        let classNames = extendedProps.validActual ? ['actual-event'] : [];
                        if (status === 'Delay' && delayTime > 24) {
                            classNames.push('multi-day-delay');
                        }
                        return {
                            ...e,
                            backgroundColor: bgColor,
                            borderColor: borderColor,
                            textColor: getTextColorByStatus(status, validActual, delayTime),
                            fontWeight: 'normal',
                            classNames: classNames
                        };
                    });
                    calendar.addEventSource(formattedEvents);
                    // Log resourceId của từng event
                    formattedEvents.forEach((ev, idx) => {
                        console.log(`Event[${idx}] resourceId:`, ev.resourceId, '| id:', ev.id, '| start BKK:', ev.start.toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' }), '| end BKK:', ev.end.toLocaleString('vi-VN', { timeZone: 'Asia/Bangkok' })); // SỬA: Log local BKK
                    });
                    console.log('SignalR Calendar updated with new data (BKK).');
                    // Log để debug sau khi FullCalendar tự render
                    setTimeout(() => {
                        console.log('SignalR Events after refetch:', calendar.getEvents().length);
                        console.log('SignalR Visible events in DOM:', document.querySelectorAll('.fc-event').length);
                    }, 100);
                })
                .catch(error => {
                    console.error('SignalR Error refetching calendar data:', error); // ← THÊM LOG ERROR
                    // FALLBACK: Manual refetch nếu SignalR fail (gọi lại GetCalendarData và rebuild như save handler)
                    console.log('SignalR fallback: Manual refetch...');
                    // ... (copy logic refetch từ save handler ở dưới, nếu cần)
                });
        });
        // Handle disconnect/reconnect
        connection.onclose(function (err) {
            console.warn('SignalR disconnected. Reconnecting...');
            setTimeout(() => connection.start(), 5000);
        });
    }
    // Gọi init sau render
    initSignalR();
    // --- Vạch đỏ hiển thị thời gian hiện tại (FIX: Giảm z-index và ẩn khi modal mở) - SỬA POSITION THEO LOCAL BKK
    const calComputed = window.getComputedStyle(calendarEl);
    if (calComputed.position === 'static') calendarEl.style.position = 'relative';
    const customNow = document.createElement('div');
    Object.assign(customNow.style, {
        position: 'absolute',
        width: '4px', // Tăng từ 2px lên 4px để dễ nhìn hơn
        backgroundColor: 'red',
        zIndex: 10, // FIX: Giảm từ 9999 xuống 10 (dưới modal 1050)
        top: '0px', // Đặt top ở 0 để bắt đầu từ đầu bảng
        height: '100%', // Chiếm hết chiều cao của bảng
        transition: 'left 0.5s linear',
        left: '0px' // SỬA: Ban đầu set left=0 cho midnight mode
    });
    calendarEl.appendChild(customNow);
    // --- Đồng hồ trên đầu vạch đỏ (FIX: Giảm z-index)
    const clockLabel = document.createElement('div');
    Object.assign(clockLabel.style, {
        position: 'absolute',
        backgroundColor: '#fff',
        color: '#000',
        border: '1px solid #ccc',
        borderRadius: '4px',
        padding: '3px 8px',
        fontSize: '16px',
        fontWeight: 'bold',
        transform: 'translate(-50%, 0)', // center horizontally; we'll set top so label sits above the red line
        zIndex: 11, // FIX: Giảm từ 10000 xuống 11 (vẫn trên vạch nhưng dưới modal)
        boxShadow: '0 1px 3px rgba(0,0,0,0.3)',
        left: '0px', // SỬA: Ban đầu set left=0 cho midnight mode
        display: 'block', // THÊM: Đảm bảo display block ban đầu
        opacity: 1 // THÊM: Đảm bảo opacity đầy đủ
    });
    calendarEl.appendChild(clockLabel);
    const hideDefaultNowIndicator = () => {
        document.querySelectorAll('.fc-timeline-now-indicator-line, .fc-now-indicator-line')
            .forEach(el => el.style.display = 'none');
    };
    function getHeaderHourCells() {
        const cells = Array.from(document.querySelectorAll('.fc-timeline-slot'));
        return cells.filter(el => /^\d{1,2}[:.]\d{2}/.test(el.textContent.trim()));
    }
    // SỬA: positionCustomNowIndicator() - Adjust cho midnight mode (ban đầu ở 0), THEO LOCAL BKK + FIX: Tính toán top động cho clockLabel
    function positionCustomNowIndicator() {
        const now = new Date(); // Giờ local BKK
        const nowHour = now.getHours();
        const minutes = now.getMinutes();
        const seconds = now.getSeconds();
        const percent = (minutes * 60 + seconds) / 3600;
        const cells = getHeaderHourCells();
        if (cells.length === 0) return;
        let left;
        if (isMidnightMode && nowHour < 6) {
            // Midnight mode: Position từ đầu (00:00 local), percent dựa trên giờ hiện tại local
            const firstHour = 0; // Bắt đầu từ 00:00 local
            let index = nowHour - firstHour;
            if (index < 0) index = 0;
            if (index >= cells.length) index = cells.length - 1;
            const cell = cells[index];
            const cellRect = cell.getBoundingClientRect();
            const calRect = calendarEl.getBoundingClientRect();
            left = (cellRect.left - calRect.left) + percent * cellRect.width;
        } else {
            // Logic cũ: Position dựa trên giờ local hiện tại so với first slot
            const firstHour = parseInt(cells[0].textContent.trim().split(':')[0]);
            let index = nowHour - firstHour;
            if (index < 0) index = 0;
            if (index >= cells.length) index = cells.length - 1;
            const cell = cells[index];
            const cellRect = cell.getBoundingClientRect();
            const calRect = calendarEl.getBoundingClientRect();
            left = (cellRect.left - calRect.left) + percent * cellRect.width;
        }
        customNow.style.left = left + 'px';
        clockLabel.style.left = left + 'px';
        // Compute top based on the actual position of the red now indicator
        try {
            const calRect = calendarEl.getBoundingClientRect();
            const nowRect = customNow.getBoundingClientRect();
            // nowTop relative to calendarEl
            const nowTop = Math.max(0, nowRect.top - calRect.top);
            const lblRect = clockLabel.getBoundingClientRect();
            const lblH = lblRect.height || clockLabel.offsetHeight || 20;
            // place label so its bottom is ~6px above the top of the red line
            let topPx = Math.round(nowTop - lblH - 6);
            // clamp within calendar bounds
            if (topPx < 2) topPx = 2;
            const calH = calRect.height || calendarEl.offsetHeight || 0;
            if (topPx + lblH > calH - 2) topPx = Math.max(2, calH - lblH - 2);
            clockLabel.style.top = topPx + 'px';
        } catch (err) {
            // Fallback: put label under header if measurement fails
            const headerToolbar = document.querySelector('.fc-header-toolbar');
            const headerHeight = headerToolbar ? headerToolbar.offsetHeight : 60;
            clockLabel.style.top = Math.max(2, headerHeight - (clockLabel.offsetHeight || 20) - 6) + 'px';
        }
    }
    // SỬA: updateClockLabel() - Hiển thị giờ local BKK
    function updateClockLabel() {
        const now = new Date(); // Giờ local BKK
        const hh = String(now.getHours()).padStart(2, '0');
        const mm = String(now.getMinutes()).padStart(2, '0');
        clockLabel.textContent = `${hh}:${mm}`; // Hiển thị giờ BKK
    }
    // THÊM MỚI: Ẩn/hiện now indicator khi modal mở/đóng
    function hideNowIndicator() {
        customNow.style.display = 'none';
        clockLabel.style.display = 'none';
    }
    function showNowIndicator() {
        customNow.style.display = 'block';
        clockLabel.style.display = 'block';
    }
    // Event listener cho modal show/hide
    document.getElementById('orderDetailsModal').addEventListener('show.bs.modal', hideNowIndicator);
    document.getElementById('orderDetailsModal').addEventListener('hidden.bs.modal', showNowIndicator);
    // SỬA: updateVisibleRange() - Thêm detect midnight và switch mode lúc 06:00, THEO LOCAL BKK + SỬA getTimeRange có viewEnd
    function updateVisibleRange() {
        const now = new Date(); // Giờ local BKK
        const nowHour = now.getHours();
        // Detect midnight (00:00 local) và chuyển ngày mới
        if (nowHour === 0 && now.getMinutes() === 0) { // Chính xác lúc 00:00:00 local
            console.log('Midnight BKK detected! Switching to new day and midnight mode.');
            calendar.today(); // Nhảy sang ngày mới
            isMidnightMode = true; // Bật midnight mode
            const midnightRange = getTimeRange(true); // Mode đặc biệt local
            calendar.setOption('slotMinTime', midnightRange.startHour);
            calendar.setOption('slotMaxTime', midnightRange.endHour);
            calendar.setOption('scrollTime', midnightRange.currentShort);
            // Reset position now về 0
            customNow.style.left = '0px';
            clockLabel.style.left = '0px';
        } else if (isMidnightMode && nowHour >= 6) {
            // Switch về logic cũ khi đủ 6 tiếng local
            console.log('6 hours passed in midnight mode BKK! Switching back to normal logic.');
            isMidnightMode = false;
            const normalRange = getTimeRange(false); // Logic cũ local
            calendar.setOption('slotMinTime', normalRange.startHour);
            calendar.setOption('slotMaxTime', normalRange.endHour);
            calendar.setOption('scrollTime', normalRange.currentShort);
        } else if (!isMidnightMode) {
            // Logic cũ bình thường local
            const normalRange = getTimeRange(false);
            calendar.setOption('slotMinTime', normalRange.startHour);
            calendar.setOption('slotMaxTime', normalRange.endHour);
            calendar.setOption('scrollTime', normalRange.currentShort);
        }
        // Nếu vẫn midnight mode (giờ <6 local), update range với mode true
        else if (isMidnightMode) {
            const midnightRange = getTimeRange(true);
            calendar.setOption('slotMinTime', midnightRange.startHour);
            calendar.setOption('slotMaxTime', midnightRange.endHour);
            calendar.setOption('scrollTime', midnightRange.currentShort);
        }
        // Update global timeRange
        timeRange = getTimeRange(isMidnightMode);
    }
    setTimeout(() => {
        hideDefaultNowIndicator();
        updateClockLabel();
        positionCustomNowIndicator();
    }, 300);
    setInterval(() => {
        hideDefaultNowIndicator();
        positionCustomNowIndicator();
    }, 1000);
    setInterval(() => {
        updateClockLabel();
        updateVisibleRange(); // ← SỬA: Luôn update full range với detect midnight/switch local BKK
    }, 60 * 1000);
    // SỬA MỚI: Khi mở delayModal, set hidden OrderId từ event + Set StartTime = max(now, event.start) cho future events (editable) - SỬA NOW LOCAL BKK
    document.getElementById('delayModal').addEventListener('show.bs.modal', function (event) {
        const uid = window.currentDelayUid || ''; // Set global trước khi show
        document.querySelector('#delayModal input[name="OrderId"]').value = uid;
        // THÊM MỚI: Set StartTime = max(now local, event.planStart hoặc event.start) cho future delay
        if (window.currentDelayEvent) {
            const eventObj = window.currentDelayEvent;
            const now = new Date(); // Giờ local BKK
            let defaultStart = now;
            const planStart = eventObj.extendedProps.planStart ? new Date(eventObj.extendedProps.planStart) : eventObj.start;
            if (planStart > now) {
                defaultStart = planStart; // Sử dụng planStart nếu future
            }
            document.querySelector('#delayModal input[name="StartTime"]').value = defaultStart.toISOString().slice(0, 16);
        } else {
            // Fallback nếu không có event info
            const now = new Date(); // Giờ local BKK
            document.querySelector('#delayModal input[name="StartTime"]').value = now.toISOString().slice(0, 16);
        }
        // THÊM: Làm StartTime editable (remove readonly nếu có)
        const startInput = document.querySelector('#delayModal input[name="StartTime"]');
        startInput.removeAttribute('readonly'); // Làm editable
    });
    // SỬA: Xử lý save button cho delay modal - GỌI API POST ĐỂ SAVE VÀO DELAYHISTORY + UPDATE STATUS SANG DELAY + REFETCH DATA + SỬA: Sử dụng input StartTime/ChangeTime + THÊM: Switch to week view nếu delayTime >24h (FIX: BỎ REFETCH THỦ CÔNG, DÙNG SIGNALR)
    document.addEventListener('click', function (e) {
        if (e.target.id === 'saveDelay') {
            console.log('SAVE DELAY CLICKED! Starting handler...');
            const form = document.getElementById('delayForm');
            const formData = new FormData(form);
            const data = {
                OrderId: document.querySelector('input[name="OrderId"]').value || '', // THÊM: Lấy OrderId từ hidden input (sẽ set khi mở modal)
                DelayType: parseInt(formData.get('DelayType')) || 0,
                Reason: formData.get('Reason') || '',
                StartTime: formData.get('StartTime') || '', // SỬA: Lấy từ input (editable)
                ChangeTime: new Date().toISOString(), // Default ChangeTime = now (có thể editable nếu thêm field)
                DelayTime: parseFloat(formData.get('DelayTime')) || 0
            };
            // THÊM MỚI: Gọi API POST để save
            fetch(window.appBaseUrl + 'api/DelayHistory/SaveDelay', { // Route mới trong DelayController
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(data)
            })
                .then(response => response.json())
                .then(result => {
                    if (result.success) {
                        console.log('Delay saved successfully:', result);
                        const delayModal = bootstrap.Modal.getInstance(document.getElementById('delayModal'));
                        if (delayModal) delayModal.hide();
                        playBeep(0.3);
                        // THÊM: Hiển thị thông báo dựa trên message
                        if (result.message.includes('email sent')) {
                            alert('Delay applied, status updated, and email notification sent successfully!'); // Thông báo đầy đủ
                        } else {
                            alert('Delay applied and status updated!');
                        }
                    } else {
                        alert('Error saving delay: ' + (result.message || 'Unknown error'));
                    }
                })
                .catch(error => {
                    console.error('Error saving delay:', error);
                    alert('Network error saving delay!');
                });
        }
    });
    // THÊM VÀO CUỐI FILE JS: Xử lý counter cho Reason textarea
    const reasonTextarea = document.getElementById('reason');
    const counter = document.getElementById('reasonCounter');
    if (reasonTextarea && counter) {
        reasonTextarea.addEventListener('input', function () {
            const length = this.value.length;
            counter.textContent = `${length} / 255`;
            if (length > 200) {
                counter.classList.remove('text-muted');
                counter.classList.add('text-danger');
            } else {
                counter.classList.remove('text-danger');
                counter.classList.add('text-muted');
            }
        });
    }
    // THÊM: Function getHeaderHourCells (dùng trong createCombinedBlock)
    function getHeaderHourCells() {
        return Array.from(document.querySelectorAll('.fc-timeline-slot')).filter(el => /^\d{1,2}[:.]\d{2}/.test(el.textContent.trim()));
    }
});