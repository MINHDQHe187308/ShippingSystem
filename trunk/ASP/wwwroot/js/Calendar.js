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

    // Tạo event data từ dữ liệu Order trong Database - hiển thị cả plan và actual nếu có
    const eventsData = orders.flatMap((order, index) => {
        const status = statusMap[order.Status] || 'Planned';
        const isPlan = order.Status === 0;
        const resourceName = customerMap[order.Resource] || order.Resource;

        // Luôn tạo plan event
        const planEvent = {
            id: `plan-${index}`,
            resourceId: order.Resource,
            start: order.PlanAsyTime,
            end: order.PlanDeliveryTime,
            title: `Plan ${order.Resource}`,
            status: 'Planned',
            classNames: ['plan-event']
        };

        // Tạo actual event nếu không phải plan và có AcAsyTime
        let actualEvent = null;
        if (!isPlan && order.AcAsyTime) {
            actualEvent = {
                id: `actual-${index}`,
                resourceId: order.Resource,
                start: order.AcAsyTime,
                end: order.AcDeliveryTime || order.PlanDeliveryTime,
                title: `Actual ${order.Resource}`,
                status: status,
                classNames: ['actual-event']
            };
        }

        return [planEvent, ...(actualEvent ? [actualEvent] : [])];
    });

    // Tạo resources từ customers hiển thị CustomerName theo CustomerCode
    const resources = customers.map(c => ({
        id: c.CustomerCode,
        title: c.CustomerName
    }));

    // --- Hàm lấy màu dựa trên status
    function getColorByStatus(status) {
        const colors = {
            'Planned': '#000000',
            'Pending': '#007bff',
            'Completed': '#28a745',
            'Shipped': '#ffc107'
        };
        return colors[status] || '#d3d3d3';
    }

    // --- Hàm lấy textColor dựa trên status
    function getTextColorByStatus(status) {
        const darkBgs = ['#000000', '#007bff', '#28a745'];
        const bgColor = getColorByStatus(status);
        return darkBgs.includes(bgColor) ? '#fff' : '#000';
    }

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

        events: eventsData.map(e => ({
            ...e,
            backgroundColor: getColorByStatus(e.status),
            borderColor: getColorByStatus(e.status),
            textColor: getTextColorByStatus(e.status),
            fontWeight: e.classNames?.includes('actual-event') ? 'normal' : 'bold'
        }))
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