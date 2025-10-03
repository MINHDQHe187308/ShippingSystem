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

    // --- Thêm CSS styles cho z-index và height
    const style = document.createElement('style');
    style.textContent = `
        .fc-event.plan-event {
            z-index: 1;
        }
        .fc-event.actual-event {
            z-index: 2;
            height: 1.2em !important;
            line-height: 1.2em !important;
        }
    `;
    document.head.appendChild(style);

    // --- Helper: lấy chuỗi HH:MM:SS từ Date
    function hhmmss(d) {
        const hh = String(d.getHours()).padStart(2, '0');
        const mm = String(d.getMinutes()).padStart(2, '0');
        const ss = String(d.getSeconds()).padStart(2, '0');
        return `${hh}:${mm}:${ss}`;
    }

    // --- Lấy khung giờ hiển thị
    function getTimeRange() {
        const now = new Date();
        const start = new Date(now);
        const end = new Date(now);
        start.setHours(now.getHours() - 6, 0, 0, 0);
        end.setHours(now.getHours() + 4, 0, 0, 0);
        return {
            startHour: hhmmss(start),
            endHour: hhmmss(end),
            currentShort: hhmmss(now).slice(0, 5),
            start, end
        };
    }

    let timeRange = getTimeRange();

    // --- Mapping status từ số sang string
    const statusMap = {
        0: 'Planned',
        1: 'Pending',
        2: 'Shipped',
        3: 'Completed'
    };

    // Tạo customer map để lấy tên
    const customerMap = {};
    customers.forEach(c => {
        customerMap[c.CustomerCode] = c.CustomerName;
    });

    // Tạo resources từ customers hiển thị CustomerName theo CustomerCode
    const resources = customers.map(c => ({
        id: c.CustomerCode,
        title: c.CustomerName
    }));

    // --- Hàm lấy màu dựa trên status (thêm 'Actual')
    function getColorByStatus(status) {
        const colors = {
            'Planned': '#000000',
            'Pending': '#007bff',
            'Completed': '#28a745',
            'Shipped': '#ffc107',
            'Actual': '#17a2b8'  // Màu mới cho Actual (xanh dương nhạt, phân biệt) - nhưng không dùng nữa
        };
        return colors[status] || '#d3d3d3';
    }

    // --- Hàm lấy textColor dựa trên status
    function getTextColorByStatus(status) {
        const darkBgs = ['#000000', '#007bff', '#28a745', '#17a2b8'];
        const bgColor = getColorByStatus(status);
        return darkBgs.includes(bgColor) ? '#fff' : '#000';
    }

    // --- Tạo event data từ dữ liệu Order - FIX: Di chuyển status vào extendedProps
    const eventsData = orders.map((order, index) => {
        // Helper: Parse và validate time
        function parseAndValidate(timeStr) {
            if (!timeStr) return null;
            const d = new Date(timeStr);
            if (isNaN(d.getTime())) return null;  // Invalid date
            return d;
        }

        const planStart = parseAndValidate(order.PlanAsyTime);
        const planEnd = parseAndValidate(order.PlanDeliveryTime);
        const actualStart = parseAndValidate(order.AcAsyTime);
        const actualEnd = parseAndValidate(order.AcDeliveryTime);

        // Valid nếu start < end
        const validPlan = planStart && planEnd && planStart < planEnd;
        const validActual = actualStart && actualEnd && actualStart < actualEnd;

        let eventStart, eventEnd;
        // FIX: ParseInt để map đúng status (nếu order.Status là string "2" → number 2)
        let status = statusMap[parseInt(order.Status, 10)] || 'Planned';  // ← SỬA CHÍNH Ở ĐÂY

        // Debug log (xóa sau khi test OK)
        console.log('Order ID:', order.id || index, 'Status raw:', order.Status, 'Status mapped:', status, 'Color:', getColorByStatus(status));

        if (validActual) {
            eventStart = actualStart;
            eventEnd = actualEnd;
        } else if (validPlan) {
            eventStart = planStart;
            eventEnd = planEnd;
        } else {
            return null;  // Skip invalid order
        }

        // FIX: Extend span to union nếu có cả actual và plan
        let hasBoth = false;
        if (validActual && validPlan) {
            eventStart = new Date(Math.min(actualStart, planStart));
            eventEnd = new Date(Math.max(actualEnd, planEnd));
            hasBoth = true;
        }

        const customerCode = order.CustomerCode || order.Resource || 'Unknown';
        return {
            id: `order-${index}`,
            resourceId: customerCode,
            start: eventStart,
            end: eventEnd,
            title: customerCode,
            // Xóa status ở root level, di chuyển vào extendedProps
            hasBoth: hasBoth,
            extendedProps: {
                planStart: validPlan ? planStart.toISOString() : null,  // Lưu ISO string để tránh re-parse
                planEnd: validPlan ? planEnd.toISOString() : null,
                actualStart: validActual ? actualStart.toISOString() : null,
                actualEnd: validActual ? actualEnd.toISOString() : null,
                validActual: validActual,
                status: status  // ← SỬA: Di chuyển status vào extendedProps
            }
        };
    }).filter(e => e);  // Remove null events

    // --- Khởi tạo FullCalendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        schedulerLicenseKey: 'GPL-My-Project-Is-Open-Source',
        initialView: 'resourceTimelineDay',
        nowIndicator: false,
        height: 'auto',

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

        resourceAreaHeaderContent: 'Suppliers',
        resourceAreaWidth: '120px',
        resources: resources,

        events: eventsData.map(e => {
            // FIX: Dùng e.extendedProps.status thay vì e.status
            const bgColor = e.hasBoth ? 'transparent' : getColorByStatus(e.extendedProps.status);
            const borderColor = e.hasBoth ? 'transparent' : getColorByStatus(e.extendedProps.status);
            // FIX: Thêm classNames cho actual event để CSS apply
            const classNames = e.extendedProps.validActual ? ['actual-event'] : [];
            return {
                ...e,
                backgroundColor: bgColor,
                borderColor: borderColor,
                textColor: getTextColorByStatus(e.extendedProps.status),  // ← SỬA: Dùng extendedProps.status
                fontWeight: 'normal',
                classNames: classNames  // ← SỬA THÊM Ở ĐÂY
            };
        }),

        // --- Custom eventContent: FIX để vẽ actual theo màu status DB (vàng cho Shipped)
        eventContent: function (arg) {
            // FIX: Lấy status từ extendedProps
            const status = arg.event.extendedProps.status;  // ← SỬA CHÍNH: Lấy từ extendedProps
            const { planStart, planEnd, actualStart, actualEnd, validActual } = arg.event.extendedProps;
            const eventStart = arg.event.start;
            const eventEnd = arg.event.end;
            const eventDuration = eventEnd - eventStart;

            // Parse từ ISO
            const pStart = planStart ? new Date(planStart) : null;
            const pEnd = planEnd ? new Date(planEnd) : null;
            const aStart = actualStart ? new Date(actualStart) : null;
            const aEnd = actualEnd ? new Date(actualEnd) : null;

            let tooltip = '';
            let actualPercent = 0, actualWidth = 0;
            let planPercent = 0, planWidth = 0;

            if (aStart && aEnd && pStart && pEnd) {
                // Tính % so với eventDuration (union)
                actualPercent = ((aStart - eventStart) / eventDuration) * 100;
                actualWidth = ((aEnd - aStart) / eventDuration) * 100;
                planPercent = ((pStart - eventStart) / eventDuration) * 100;
                planWidth = ((pEnd - pStart) / eventDuration) * 100;

                const fmtTime = (d) => d.toTimeString().slice(0, 8);
                tooltip = `Actual: ${fmtTime(aStart)} - ${fmtTime(aEnd)}\nPlan: ${fmtTime(pStart)} - ${fmtTime(pEnd)}\nStatus: ${status}`;
            } else if (pStart && pEnd) {
                tooltip = 'Plan only\nStatus: ' + status;
            } else {
                tooltip = 'Actual only\nStatus: ' + status;
            }

            // Debug log cho rendering (xóa sau khi test OK)
            console.log('Rendering event:', arg.event.title, 'Status:', status, 'ValidActual:', validActual, 'Color for bar:', getColorByStatus(status));

            return {
                domNodes: [
                    (() => {
                        const wrapper = document.createElement('div');
                        // FIX: Fallback bg nếu transparent để tránh xám hoàn toàn
                        wrapper.style.position = 'relative';
                        wrapper.style.height = '1.2em';
                        wrapper.style.background = arg.event.backgroundColor || getColorByStatus(status) || 'transparent';  // ← SỬA: Dùng status từ extendedProps
                        wrapper.style.borderRadius = '4px';
                        wrapper.style.overflow = 'visible';  // Cho phép extend visible
                        wrapper.title = tooltip;
                        wrapper.style.color = arg.event.textColor;

                        // Vẽ actual bar (nếu có validActual) - dùng màu theo status DB
                        if (validActual && aStart && aEnd) {
                            const actualBar = document.createElement('div');
                            actualBar.style.position = 'absolute';
                            actualBar.style.left = Math.max(0, actualPercent) + '%';
                            actualBar.style.top = '0';
                            actualBar.style.height = '100%';
                            actualBar.style.width = actualWidth + '%';
                            actualBar.style.background = getColorByStatus(status);  // ← SỬA: Dùng status từ extendedProps
                            actualBar.style.borderRadius = '4px';
                            wrapper.appendChild(actualBar);
                        }
                        // FIX: Thêm else-if cho actual có data nhưng invalid (vẽ bar dựa trên actual times)
                        else if (aStart && aEnd) {  // ← SỬA THÊM ĐÂY
                            actualPercent = ((aStart - eventStart) / eventDuration) * 100;
                            actualWidth = ((aEnd - aStart) / eventDuration) * 100;
                            const actualBar = document.createElement('div');
                            actualBar.style.position = 'absolute';
                            actualBar.style.left = Math.max(0, actualPercent) + '%';
                            actualBar.style.top = '0';
                            actualBar.style.height = '100%';
                            actualBar.style.width = actualWidth + '%';
                            actualBar.style.background = getColorByStatus(status);  // ← SỬA: Dùng status từ extendedProps
                            actualBar.style.borderRadius = '4px';
                            wrapper.appendChild(actualBar);
                        }
                        else if (pStart && pEnd) {
                            // Nếu chỉ plan, vẽ full plan bar với màu status
                            const planBarFull = document.createElement('div');
                            planBarFull.style.position = 'absolute';
                            planBarFull.style.left = '0%';
                            planBarFull.style.top = '0';
                            planBarFull.style.height = '100%';
                            planBarFull.style.width = '100%';
                            planBarFull.style.background = getColorByStatus(status);  // ← SỬA: Dùng status từ extendedProps
                            planBarFull.style.borderRadius = '4px';
                            wrapper.appendChild(planBarFull);
                        }

                        // Vẽ plan bar overlay (nếu có plan, và không phải chỉ plan)
                        if (pStart && pEnd && !(aStart && aEnd && !validActual)) {
                            const planBar = document.createElement('div');
                            planBar.style.position = 'absolute';
                            planBar.style.left = Math.max(0, planPercent) + '%';
                            planBar.style.top = '25%';  // Offset để overlap visible
                            planBar.style.height = '50%';
                            planBar.style.width = planWidth + '%';
                            planBar.style.background = getColorByStatus('Planned');  // Đen cho plan overlay
                            planBar.style.borderRadius = '2px';
                            planBar.title = `Full Plan: ${planPercent.toFixed(0)}% - ${(planPercent + planWidth).toFixed(0)}%`;
                            wrapper.appendChild(planBar);
                        }

                        // Text (luôn hiển thị)
                        const text = document.createElement('span');
                        text.textContent = arg.event.title;
                        text.style.position = 'relative';
                        text.style.zIndex = '2';
                        text.style.paddingLeft = '4px';
                        wrapper.appendChild(text);

                        return wrapper;
                    })()
                ]
            };
        }
    });
    calendar.render();

    // --- Vạch đỏ hiển thị thời gian hiện tại (giữ nguyên logic cũ)
    const calComputed = window.getComputedStyle(calendarEl);
    if (calComputed.position === 'static') calendarEl.style.position = 'relative';

    const customNow = document.createElement('div');
    Object.assign(customNow.style, {
        position: 'absolute',
        width: '2px',
        backgroundColor: 'red',
        zIndex: 9999,
        top: '0px',
        height: '100px',
        transition: 'left 0.5s linear'
    });
    calendarEl.appendChild(customNow);

    // --- Đồng hồ trên đầu vạch đỏ
    const clockLabel = document.createElement('div');
    Object.assign(clockLabel.style, {
        position: 'absolute',
        backgroundColor: '#fff',
        color: '#000',
        border: '1px solid #ccc',
        borderRadius: '4px',
        padding: '3px 8px',
        fontSize: '12px',
        fontWeight: 'bold',
        transform: 'translate(-50%, -120%)',
        zIndex: 10000,
        boxShadow: '0 1px 3px rgba(0,0,0,0.3)'
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

    function positionCustomNowIndicator() {
        const now = new Date();
        const minutes = now.getMinutes();
        const seconds = now.getSeconds();
        const percent = (minutes * 60 + seconds) / 3600;

        const cells = getHeaderHourCells();
        if (cells.length === 0) return;

        const firstHour = parseInt(cells[0].textContent.trim().split(':')[0]);
        let index = now.getHours() - firstHour;
        if (index < 0) index = 0;
        if (index >= cells.length) index = cells.length - 1;

        const cell = cells[index];
        const cellRect = cell.getBoundingClientRect();
        const calRect = calendarEl.getBoundingClientRect();
        const left = (cellRect.left - calRect.left) + percent * cellRect.width;

        const header = cell.closest('.fc-col-header, .fc-col-header-row, .fc-scrollgrid-sync-inner');
        const headerBottom = header ? header.getBoundingClientRect().bottom : cellRect.bottom;
        const top = headerBottom - calRect.top;
        const height = calRect.height - top - 10;

        customNow.style.left = left + 'px';
        customNow.style.top = top + 'px';
        customNow.style.height = height + 'px';
        clockLabel.style.left = left + 'px';
        clockLabel.style.top = top + 'px';
    }

    function updateClockLabel() {
        const now = new Date();
        const hh = String(now.getHours()).padStart(2, '0');
        const mm = String(now.getMinutes()).padStart(2, '0');
        clockLabel.textContent = `${hh}:${mm}`;
    }

    function updateVisibleRange() {
        const { startHour, endHour } = getTimeRange();
        calendar.setOption('slotMinTime', startHour);
        calendar.setOption('slotMaxTime', endHour);
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
        updateVisibleRange();
    }, 60 * 1000);
});