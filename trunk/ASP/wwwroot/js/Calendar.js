document.addEventListener('DOMContentLoaded', function () {
    // Kiểm tra xem FullCalendar có được định nghĩa không
    if (typeof FullCalendar === 'undefined') {
        console.error('FullCalendar is not loaded. Please check if the FullCalendar scripts are included correctly.');
        return;
    }

    const calendarEl = document.getElementById('calendar');
    if (!calendarEl) {
        console.error('Element with ID "calendar" not found.');
        return;
    }

    // Hàm để lấy thời gian hiện tại và tính toán slotMinTime, slotMaxTime
    function getTimeRange() {
        const now = new Date();
        const today = now.toISOString().split('T')[0];
        const startHour = new Date(now.getTime() - 6 * 60 * 60 * 1000)
            .toTimeString().substring(0, 8);
        const endHour = new Date(now.getTime() + 4 * 60 * 60 * 1000)
            .toTimeString().substring(0, 8);
        const currentTime = now.toTimeString().substring(0, 8);
        return { today, startHour, endHour, currentTime };
    }

    const timeRange = getTimeRange(); // Lấy một lần để sử dụng cho events

    // Khởi tạo FullCalendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'resourceTimelineDay',
        schedulerLicenseKey: 'GPL-My-Project-Is-Open-Source',
        nowIndicator: true,
        height: 'auto',

        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'resourceTimelineDay,resourceTimelineWeek'
        },

        slotMinTime: timeRange.startHour,
        slotMaxTime: timeRange.endHour,
        scrollTime: timeRange.currentTime,

        // Timeline mỗi ô 1 tiếng và chỉ hiển thị giờ tròn
        slotDuration: '01:00:00',
        slotLabelFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
            meridiem: false
        },

        resourceAreaHeaderContent: 'Suppliers',
        resourceAreaWidth: '120px',
        resources: [
            { id: 'TMV', title: 'Toyota' },
            { id: 'DENSO', title: 'Denso' },
            { id: 'NISSAN', title: 'Nissan' },
            { id: 'HONDA', title: 'Honda' }
        ],

        events: [
            // Plan events (solid border by default)
            {
                id: '1',
                resourceId: 'TMV',
                start: timeRange.today + 'T10:30:00',
                end: timeRange.today + 'T12:00:00',
                title: 'Plan Xuất hàng A',
                backgroundColor: 'yellow',
                borderColor: 'yellow'
            },
            {
                id: '2',
                resourceId: 'DENSO',
                start: timeRange.today + 'T11:00:00',
                end: timeRange.today + 'T13:30:00',
                title: 'Plan Xuất hàng B',
                backgroundColor: 'green',
                borderColor: 'green'
            },
            {
                id: '3',
                resourceId: 'NISSAN',
                start: timeRange.today + 'T14:00:00',
                end: timeRange.today + 'T15:30:00',
                title: 'Plan Xuất hàng C',
                backgroundColor: 'orange',
                borderColor: 'orange'
            },
            // Actual events (dashed border via class)
            {
                id: '1a',
                resourceId: 'TMV',
                start: timeRange.today + 'T10:45:00',
                end: timeRange.today + 'T12:15:00',
                title: 'Actual Xuất hàng A',
                backgroundColor: 'yellow',
                borderColor: 'yellow',
                classNames: ['actual-event']
            },
            {
                id: '2a',
                resourceId: 'DENSO',
                start: timeRange.today + 'T11:15:00',
                end: timeRange.today + 'T13:45:00',
                title: 'Actual Xuất hàng B',
                backgroundColor: 'green',
                borderColor: 'green',
                classNames: ['actual-event']
            },
            {
                id: '3a',
                resourceId: 'NISSAN',
                start: timeRange.today + 'T14:15:00',
                end: timeRange.today + 'T15:45:00',
                title: 'Actual Xuất hàng C',
                backgroundColor: 'orange',
                borderColor: 'orange',
                classNames: ['actual-event']
            }
        ]
    });

    // Render ban đầu
    calendar.render();

    // Cập nhật timeline mỗi phút
    setInterval(() => {
        const { startHour, endHour, currentTime } = getTimeRange();
        calendar.setOption('slotMinTime', startHour);
        calendar.setOption('slotMaxTime', endHour);
        calendar.setOption('scrollTime', currentTime);
        calendar.render(); // Re-render để cập nhật giao diện
    }, 60000); // 60,000ms = 1 phút
});