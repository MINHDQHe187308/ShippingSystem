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

    // --- Thêm CSS styles cho z-index, height, centering và CUSTOM TOOLTIP + HOVER EFFECT CHỈ CHO DELAY MODE + THÊM STYLE CHO DELAY BAR (COMMENT CLASS DELAY-EVENT)
    const style = document.createElement('style');
    style.textContent = `
        .fc-event {
            height: 80px !important;  /* Tăng từ 70px lên 80px để chứa thêm thông tin progress */
            display: flex !important;
            align-items: center !important;  /* Căn giữa theo chiều dọc trong slot */
            justify-content: center !important;  /* Căn giữa text nếu cần */
            margin: 0 !important;
            line-height: 1.2 !important;
            cursor: pointer;  /* Thêm cursor pointer để dễ nhận biết hover */
            transition: transform 0.2s ease, box-shadow 0.2s ease;  /* Giữ transition để mượt mà khi có effect */
            position: relative;  /* THÊM: Để position absolute cho delay bar */
            overflow: visible !important;  /* THÊM: Cho phép delay bar extend ra ngoài */
        }
        .fc-event.plan-event {
            z-index: 1;
        }
        .fc-event.actual-event {
            z-index: 2;
            height: 80px !important;  /* Đồng bộ height với .fc-event */
            line-height: 1.2 !important;
        }
        /* COMMENT: .fc-event.delay-event { ... } để không apply viền đỏ cho Delay */
        /*
        .fc-event.delay-event {
            border: 3px solid #ff0000 !important;
            box-shadow: 0 0 10px rgba(255, 0, 0, 0.5);
        }
        */
        .fc-event .fc-event-main {  /* Đảm bảo inner content cũng center */
            display: flex;
            align-items: center;
            height: 100%;
            font-size: 16px;  /* Tăng font-size từ 14px lên 16px để dễ đọc hơn */
        }
        /* Tăng chiều cao resource rows để chứa event lớn hơn */
        .fc-resource-timeline .fc-resource-cell {
            height: 90px !important;  /* Tăng height của resource cell từ 80px lên 90px */
            padding: 4px !important;  /* Thêm padding để không sát mép */
        }
        .fc-resource-timeline .fc-timeline-slot-table .fc-resource-cell {
            height: 90px !important;  /* Đảm bảo apply cho slot table cells */
        }
        /* XÓA: Hover effect cơ bản - CHỈ GIỮ CHO DELAY MODE */
        /* THÊM: Hover effect mạnh hơn khi delay mode on */
        .delay-mode .fc-event:hover {
            transform: scale(1.05);
            box-shadow: 0 4px 12px rgba(255, 0, 0, 0.3);  /* Đỏ nhạt để match theme */
            border: 2px solid #ff0000;  /* Viền đỏ */
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
        /* Custom Tooltip Styles - ĐẸP MẮT VÀ DỄ NHÌN */
        #custom-tooltip {
            position: absolute;
            background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 16px;  /* Tăng padding từ 12px lên 16px */
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
            z-index: 10001;
            pointer-events: none;
            max-width: 300px;  /* Tăng max-width từ 250px lên 300px */
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            font-size: 15px;  /* Tăng font-size tổng thể từ 14px lên 15px */
            line-height: 1.5;  /* Tăng line-height từ 1.4 lên 1.5 cho dễ đọc hơn */
            display: none;
            transition: opacity 0.1s ease-out, transform 0.1s ease-out;  /* Transition mượt mà nhưng nhanh */
        }
        #custom-tooltip.show {
            display: block;
            opacity: 1;
            transform: translateY(0);
        }
        #custom-tooltip h4 {
            margin: 0 0 12px 0;  /* Tăng margin-bottom từ 8px lên 12px */
            color: #333;
            font-size: 17px;  /* Tăng font-size từ 15px lên 17px */
            font-weight: 600;
            border-bottom: 1px solid #eee;
            padding-bottom: 6px;  /* Tăng padding-bottom từ 4px lên 6px */
        }
        #custom-tooltip dl {
            margin: 0;
            display: grid;
            grid-template-columns: auto 1fr;
            gap: 6px 10px;  /* Tăng gap từ 4px 8px lên 6px 10px */
            align-items: center;
        }
        #custom-tooltip dt {
            font-weight: 700;  /* Tăng font-weight từ 600 lên 700 để đậm hơn */
            color: #000;  /* Đổi màu từ #555 sang #000 (đen) */
            text-align: right;
            min-width: 120px;  /* Tăng min-width từ 100px lên 120px để thẳng hàng tốt hơn */
            font-size: 15px;  /* Thêm font-size để tăng kích thước cho dt */
        }
        #custom-tooltip dd {
            margin: 0;
            color: #333;
            font-weight: 400;
            font-size: 15px;  /* Tăng font-size từ 14px lên 15px cho dd */
        }
        #custom-tooltip .status {
            padding: 4px 8px;  /* Tăng padding từ 2px 6px lên 4px 8px */
            border-radius: 4px;
            font-size: 12px;  /* Tăng font-size từ 11px lên 12px */
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
    left: 0;  /* Default, override bằng JS */
    transform-origin: left center;  /* Để scale nếu cần */
}
.fc-event {  /* Đảm bảo wrapper cho extend */
    overflow: visible !important;
    position: relative;
}
        #custom-tooltip .status.planned { background: #000; color: #fff; }
        #custom-tooltip .status.pending { background: #007bff; color: #fff; }
        #custom-tooltip .status.shipped { background: #ffc107; color: #000; }
        #custom-tooltip .status.completed { background: #28a745; color: #fff; }
        #custom-tooltip .status.delay { background: #ff0000; color: #fff; }  /* THÊM: Style cho Delay status trong tooltip */
    `;
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

    // --- Helper: lấy chuỗi HH:MM:SS từ Date
    function hhmmss(d) {
        const hh = String(d.getHours()).padStart(2, '0');
        const mm = String(d.getMinutes()).padStart(2, '0');
        const ss = String(d.getSeconds()).padStart(2, '0');
        return `${hh}:${mm}:${ss}`;
    }

    // --- Helper: Format time range
    function formatTimeRange(start, end) {
        if (!start || !end) return 'N/A';
        return `${hhmmss(start)} - ${hhmmss(end)}`;
    }

    // --- SỬA: getTimeRange() - Thêm param 'midnightMode' để xử lý mode đặc biệt lúc 00:00
    function getTimeRange(midnightMode = false) {
        const now = new Date();
        const todayMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);
        const todayEnd = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 24, 0, 0);

        let start, end;

        if (midnightMode) {
            // Mode đặc biệt: Từ 00:00 đến giờ hiện tại +4h (cap 24:00)
            start = new Date(todayMidnight);
            end = new Date(now);
            end.setHours(now.getHours() + 4, 0, 0, 0);
            if (end >= todayEnd) {
                end = todayEnd;
            }
        } else {
            // Logic cũ: -6h đến +4h so với giờ hiện tại
            start = new Date(now);
            start.setHours(now.getHours() - 6, 0, 0, 0);
            end = new Date(now);
            end.setHours(now.getHours() + 4, 0, 0, 0);
        }

        let startHour = hhmmss(start);
        let endHour = hhmmss(end);

        // Cap start: nếu < 00:00 hôm nay, set "00:00:00"
        if (start < todayMidnight) {
            startHour = '00:00:00';
        }

        // Cap end: nếu >= 24:00 hôm nay, set "24:00:00"
        if (end >= todayEnd) {
            endHour = '24:00:00';
        }

        return {
            startHour,
            endHour,
            currentShort: hhmmss(now).slice(0, 5),
            start, end
        };
    }

    let timeRange = getTimeRange();
    let isMidnightMode = false;  // Flag để track mode

    // --- SỬA: Mapping status từ số sang string - THÊM Delay (4)
    const statusMap = {
        0: 'Planned',
        1: 'Pending',
        2: 'Completed',
        3: 'Shipped',
        4: 'Delay'  // THÊM MỚI: Status Delay
    };

    // Tạo customer map để lấy tên
    const customerMap = {};
    customers.forEach(c => {
        customerMap[c.CustomerCode] = c.CustomerName;
    });

    // THÊM: Đảm bảo resourceId của mọi event đều có trong resources (cho initial load)
    const allResourceIds = Array.from(new Set(orders.map(o => o.resource)));
    if (customers) {
        const customerIds = customers.map(c => c.CustomerCode);
        allResourceIds.forEach(rid => {
            if (!customerIds.includes(rid)) {
                customers.push({ CustomerCode: rid, CustomerName: rid });
            }
        });
    }

    // Tạo resources từ customers hiển thị CustomerCode theo CustomerCode
    const resources = customers.map(c => ({
        id: c.CustomerCode,
        title: c.CustomerCode  // SỬA: Hiển thị CustomerCode thay vì CustomerName
    }));

    // --- SỬA: Hàm lấy màu dựa trên status (fallback cho Delay để giữ màu cũ) + THÊM: Nếu Delay && delayTime >24 thì return #ff0000 (đỏ)
    function getColorByStatus(status, validActual = false, delayTime = 0) {
        // THÊM MỚI: Nếu Delay && delayTime >24h thì override toàn bộ thành đỏ
        if (status === 'Delay' && delayTime > 24) {
            return '#ff0000';  // Đỏ cho multi-day delay
        }
        // Nếu Delay, fallback về màu dựa trên actual/plan
        if (status === 'Delay') {
            if (validActual) {
                return getColorByStatus('Completed');  // Xanh lá nếu có actual
            } else {
                return getColorByStatus('Planned');  // Đen nếu chỉ plan
            }
        }
        const colors = {
            'Planned': '#000000',
            'Pending': '#007bff',
            'Completed': '#28a745',
            'Shipped': '#ffc107',
            'Delay': '#ff0000',  // Giữ nguyên nhưng không dùng cho Delay nữa
            'Actual': '#17a2b8'
        };
        return colors[status] || '#d3d3d3';
    }

    // --- SỬA: Hàm lấy textColor dựa trên status (fallback cho Delay) + THÊM: Nếu multi-day delay thì text trắng (#fff)
    function getTextColorByStatus(status, validActual = false, delayTime = 0) {
        // THÊM MỚI: Nếu Delay && delayTime >24h thì text trắng
        if (status === 'Delay' && delayTime > 24) {
            return '#fff';  // Trắng trên nền đỏ
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

    // --- Tạo event data từ dữ liệu Order - THÊM UId VÀO EXTENDEDPROPS VÀ THÊM CÁC TRƯỜNG PROGRESS + THÊM DELAY INFO NẾU STATUS=4
    const eventsData = orders.map((order, index) => {
        // Helper: Parse và validate time
        function parseAndValidate(timeStr) {
            if (!timeStr) return null;
            const d = new Date(timeStr);
            if (isNaN(d.getTime())) return null;  // Invalid date
            return d;
        }

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
            title: '',  // Đặt rỗng vì dùng custom eventContent
            // Xóa status ở root level, di chuyển vào extendedProps
            hasBoth: hasBoth,
            extendedProps: {
                uid: order.UId,  // THÊM: Để sử dụng trong eventClick
                customerCode: customerCode,  // THÊM: Lưu CustomerCode vào extendedProps để dùng trong modal title
                planStart: validPlan ? planStart.toISOString() : null,  // Lưu ISO string để tránh re-parse
                planEnd: validPlan ? planEnd.toISOString() : null,
                actualStart: validActual ? actualStart.toISOString() : null,
                actualEnd: validActual ? actualEnd.toISOString() : null,
                validActual: validActual,
                status: status,  // ← SỬA: Di chuyển status vào extendedProps
                totalPallet: order.TotalPallet || 0,  // THÊM: Lưu TotalPallet vào extendedProps để dùng trong eventContent
                collectPallet: order.CollectPallet || '0 / 0',  // THÊM: Progress Collect
                threePointScan: order.ThreePointScan || '0 / 0',  // THÊM: Progress ThreePointScan
                loadCont: order.LoadCont || '0 / 0',  // THÊM: Progress LoadCont
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
            const extendedProps = e.extendedProps;
            const status = extendedProps.status;
            const validActual = extendedProps.validActual;
            const delayTime = extendedProps.delayTime || 0;  // THÊM: Lấy delayTime để check multi-day

            // SỬA: Fallback màu cho Delay + THÊM: Nếu Delay && delayTime >24 thì đỏ
            const bgColor = e.hasBoth ? 'transparent' : getColorByStatus(status, validActual, delayTime);
            const borderColor = e.hasBoth ? 'transparent' : getColorByStatus(status, validActual, delayTime);

            // SỬA: Chỉ thêm class actual-event, XÓA delay-event để không apply viền đỏ + THÊM: Thêm class multi-day-delay nếu >24h
            let classNames = extendedProps.validActual ? ['actual-event'] : [];
            if (status === 'Delay' && delayTime > 24) {
                classNames.push('multi-day-delay');  // THÊM: Class cho CSS override đỏ
            }
            // Không push 'delay-event' nữa

            return {
                ...e,
                backgroundColor: bgColor,
                borderColor: borderColor,
                textColor: getTextColorByStatus(status, validActual, delayTime),  // Truyền delayTime
                fontWeight: 'normal',
                classNames: classNames
            };
        }),

        // SỬA: Event listener cho click event - Mở modal với OrderDetails - THÊM DEBUG LOGS + XỬ LÝ DELAY MODE + CHECK STATUS CHO DELAY + THÊM SET CURRENT UID CHO DELAY MODAL + THÊM: Lưu event info global cho future delay
        eventClick: function (info) {
            console.log('Event clicked! Info:', info);  // DEBUG: Log toàn bộ info
            window.currentDelayUid = info.event.extendedProps.uid;  // THÊM: Lưu global UID cho delay modal
            window.currentDelayEvent = info.event;  // THÊM MỚI: Lưu full event info (cho set StartTime future)

            // THÊM: Nếu delayMode on, phát sound và kiểm tra status
            if (delayMode) {
                playBeep(0.5);  // Tiếng kêu khi click event
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
                    playBeep(0.3, 400, 300);  // Beep thấp để báo lỗi
                } else {
                    // Fallback cho status khác (như Delay)
                    alert(`Không thể DELAY order có trạng thái ${status.toLowerCase()}!`);
                }
                return;  // Không chạy code order details
            }

            // Code gốc cho order details nếu delayMode off
            const uid = info.event.extendedProps.uid;
            const customerCode = info.event.extendedProps.customerCode || info.event.resourceId || 'Unknown';  // Lấy CustomerCode từ extendedProps hoặc resourceId
            console.log('Extracted UID:', uid, 'CustomerCode:', customerCode);  // DEBUG: Kiểm tra UID và CustomerCode
            if (!uid) {
                console.log('UID is undefined or null');  // DEBUG
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

            const fetchUrl = `/DensoWareHouse/GetOrderDetails?orderId=${uid}`;
            console.log('Fetching from URL:', fetchUrl);  // DEBUG: Log URL

            // AJAX gọi Controller để lấy OrderDetails
            fetch(fetchUrl)
                .then(response => {
                    console.log('Fetch response status:', response.status);  // DEBUG: Kiểm tra status
                    if (!response.ok) {
                        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
                    }
                    return response.json();
                })
                .then(data => {
                    console.log('Fetch data received:', data);  // DEBUG: Log data
                    console.log('JSON data sample:', data.data[0]);  // DEBUG: Log item đầu tiên để xem keys
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
                    console.log('Attempting to show modal');  // DEBUG
                    const modalElement = document.getElementById('orderDetailsModal');
                    console.log('Modal element:', modalElement);  // DEBUG
                    if (modalElement) {
                        const modal = new bootstrap.Modal(modalElement);
                        modal.show();
                        console.log('Modal shown');  // DEBUG
                    } else {
                        console.error('Modal element not found!');  // DEBUG
                    }
                })
                .catch(error => {
                    console.error('Fetch error:', error);  // DEBUG: Log error chi tiết
                    loadingEl.style.display = 'none';
                    bodyEl.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Lỗi kết nối: ${error.message}</td></tr>`;
                    tableEl.style.display = 'table';
                    const modal = new bootstrap.Modal(document.getElementById('orderDetailsModal'));
                    modal.show();
                });
        },

        // --- SỬA: Custom eventContent: VẼ EVENT VÀ ATTACH HOVER EVENTS CHO TOOLTIP - THÊM HIỂN THỊ CUSTOMERCODE VÀ PROGRESS TRONG HÀNG NGANG VỚI BG KHÁC NHAU + HOVER EFFECT KHI DELAY MODE + THÊM VẼ DELAY BAR ĐỎ NỐI TIẾP (handle multi-day tự động vì end updated)
        eventContent: function (arg) {
            // FIX: Lấy status từ extendedProps
            const status = arg.event.extendedProps.status;
            const extendedProps = arg.event.extendedProps;
            const eventStart = arg.event.start;
            const eventEnd = arg.event.end;
            const eventDuration = eventEnd - eventStart;

            // Parse từ ISO
            const pStart = extendedProps.planStart ? new Date(extendedProps.planStart) : null;
            const pEnd = extendedProps.planEnd ? new Date(extendedProps.planEnd) : null;
            const aStart = extendedProps.actualStart ? new Date(extendedProps.actualStart) : null;
            const aEnd = extendedProps.actualEnd ? new Date(extendedProps.actualEnd) : null;

            let actualPercent = 0, actualWidth = 0;
            let planPercent = 0, planWidth = 0;

            if (aStart && aEnd && pStart && pEnd) {
                // Tính % so với eventDuration (union)
                actualPercent = ((aStart - eventStart) / eventDuration) * 100;
                actualWidth = ((aEnd - aStart) / eventDuration) * 100;
                planPercent = ((pStart - eventStart) / eventDuration) * 100;
                planWidth = ((pEnd - pStart) / eventDuration) * 100;
            } else if (aStart && aEnd) {
                actualPercent = 0;
                actualWidth = 100;
            } else if (pStart && pEnd) {
                planPercent = 0;
                planWidth = 100;
            }

            // THÊM: Delay bar info nếu Delay
            const dStart = extendedProps.delayStart ? new Date(extendedProps.delayStart) : null;
            const dEnd = extendedProps.delayEnd ? new Date(extendedProps.delayEnd) : null;
            const delayPercent = dStart ? ((dStart - eventStart) / eventDuration) * 100 : 100;  // Nối tiếp nên left=100%
            const delayWidth = extendedProps.delayTime ? (extendedProps.delayTime / (eventDuration / (1000 * 60 * 60))) * 100 : 0;  // % dựa trên giờ

            // Debug log cho rendering (xóa sau khi test OK)
            console.log('Rendering event:', arg.event.title, 'Status:', status, 'ValidActual:', extendedProps.validActual, 'Color for bar:', getColorByStatus(status, extendedProps.validActual));

            return {
                domNodes: [
                    (() => {
                        const wrapper = document.createElement('div');
                        // SỬA: Background fallback cho Delay (dùng màu cũ)
                        const fallbackColor = getColorByStatus(status, extendedProps.validActual);
                        wrapper.style.position = 'relative';
                        wrapper.style.height = '100%';  // Đồng bộ với height của .fc-event (80px)
                        wrapper.style.width = '100%';
                        wrapper.style.background = arg.event.backgroundColor || fallbackColor || 'transparent';
                        wrapper.style.borderRadius = '4px';
                        wrapper.style.overflow = 'visible';  // Cho phép extend visible
                        wrapper.style.color = arg.event.textColor;
                        wrapper.style.display = 'flex';  // Flex row để thẳng hàng
                        wrapper.style.flexDirection = 'row';
                        wrapper.style.alignItems = 'center';
                        wrapper.style.justifyContent = 'space-around';  // Phân bố đều các phần
                        wrapper.style.padding = '2px';  // Thêm padding nhẹ để không sát mép

                        // Vẽ plan bar - giữ nguyên kích thước ban đầu
                        if (pStart && pEnd) {
                            if (aStart && aEnd) {
                                // Nếu có both: vẽ plan overlay (top 15%, height 70%)
                                const planBar = document.createElement('div');
                                planBar.style.position = 'absolute';
                                planBar.style.left = Math.max(0, planPercent) + '%';
                                planBar.style.top = '15%';  // Giữ nguyên như ban đầu
                                planBar.style.height = '70%';  // Giữ nguyên như ban đầu
                                planBar.style.width = planWidth + '%';
                                planBar.style.background = getColorByStatus('Planned');  // Đen cho plan overlay
                                planBar.style.borderRadius = '2px';
                                planBar.title = `Full Plan: ${planPercent.toFixed(0)}% - ${(planPercent + planWidth).toFixed(0)}%`;
                                wrapper.appendChild(planBar);
                            } else {
                                // Nếu chỉ plan: vẽ full plan bar
                                const planBarFull = document.createElement('div');
                                planBarFull.style.position = 'absolute';
                                planBarFull.style.left = '0%';
                                planBarFull.style.top = '0';
                                planBarFull.style.height = '100%';
                                planBarFull.style.width = '100%';
                                planBarFull.style.background = fallbackColor;  // SỬA: Dùng fallback cho plan full
                                planBarFull.style.borderRadius = '4px';
                                wrapper.appendChild(planBarFull);
                            }
                        }

                        // Vẽ actual bar (nếu có) - đè lên plan với mờ và opacity, giữ full height
                        if (aStart && aEnd) {
                            const actualBar = document.createElement('div');
                            actualBar.style.position = 'absolute';
                            actualBar.style.left = Math.max(0, actualPercent) + '%';
                            actualBar.style.top = '0';
                            actualBar.style.height = '100%';
                            actualBar.style.width = actualWidth + '%';
                            actualBar.style.background = fallbackColor;  // SỬA: Fallback cho actual nếu Delay
                            actualBar.style.borderRadius = '4px';
                            actualBar.style.filter = 'blur(1px)';  // Actual mờ
                            actualBar.style.opacity = '0.7';  // Actual trong suốt để thấy plan bên dưới
                            actualBar.title = `Actual: ${actualPercent.toFixed(0)}% - ${(actualPercent + actualWidth).toFixed(0)}%`;
                            wrapper.appendChild(actualBar);
                        }

                        // THÊM: Vẽ delay bar nối tiếp nếu Delay (từ max(eventEnd, delayStart) đến delayEnd, extend ra phải nếu >100%)
                        if (status === 'Delay' && dStart && dEnd) {
                            // Calc position relative to event: Nếu delayStart >= eventEnd, vẽ từ 100% (end) + offset
                            const eventDuration = eventEnd - eventStart;  // Giữ eventDuration gốc (plan/actual)
                            let delayLeftPercent = ((dStart - eventStart) / eventDuration) * 100;
                            let delayWidthPercent = ((dEnd - dStart) / eventDuration) * 100;

                            // FIX: Nếu delay nối từ end (delayStart ≈ eventEnd), set left=100%, width=delayTime hours %
                            if (Math.abs(dStart - eventEnd) < 60000) {  // <1min tolerance
                                delayLeftPercent = 100;  // Bắt đầu từ end
                                delayWidthPercent = (extendedProps.delayTime / (eventDuration / (1000 * 60 * 60))) * 100;  // % dựa trên giờ event
                            } else if (dStart > eventEnd) {
                                // Nếu delayStart future > end, vẽ từ 100% + offset (bay ra phải)
                                const offsetPercent = ((dStart - eventEnd) / eventDuration) * 100;
                                delayLeftPercent = 100 + offsetPercent;
                                delayWidthPercent = ((dEnd - dStart) / eventDuration) * 100;
                            }

                            const delayBar = document.createElement('div');
                            delayBar.className = 'delay-bar';
                            delayBar.style.left = Math.max(0, delayLeftPercent) + '%';  // Min 0 để không âm
                            delayBar.style.width = delayWidthPercent + '%';  // Có thể >100% để extend
                            delayBar.style.background = '#ff0000';
                            delayBar.style.right = 'auto';  // Để left calc đúng
                            delayBar.title = `Delay : ${hhmmss(dStart)} - ${hhmmss(dEnd)} (${extendedProps.delayTime}h)`;
                            wrapper.appendChild(delayBar);

                            // Nếu width >100%, thêm pseudo-element để extend visual (optional, nếu CSS hỗ trợ)
                            if (delayWidthPercent > 100) {
                                delayBar.style.position = 'absolute';
                                delayBar.style.clipPath = 'none';  // Đảm bảo không clip
                            }

                            console.log('Delay bar positioned:', { left: delayLeftPercent.toFixed(1), width: delayWidthPercent.toFixed(1), isExtend: delayWidthPercent > 100 });
                        }

                        // THÊM MỚI: Hiển thị CustomerCode và 3 progress thẳng hàng nhau với background khác nhau
                        // Phần 1: CustomerCode (background xám nhạt, chữ đen, đậm)
                        const customerDiv = document.createElement('div');
                        customerDiv.textContent = extendedProps.customerCode || 'N/A';
                        customerDiv.style.position = 'relative';
                        customerDiv.style.zIndex = '2';
                        customerDiv.style.backgroundColor = '#f8f9fa';  // Xám nhạt dễ nhìn
                        customerDiv.style.color = '#000000';
                        customerDiv.style.padding = '4px 8px';
                        customerDiv.style.borderRadius = '4px';
                        customerDiv.style.fontSize = '12px';
                        customerDiv.style.fontWeight = 'bold';
                        customerDiv.style.textAlign = 'center';
                        customerDiv.style.minWidth = '60px';  // Đảm bảo không bị ép
                        wrapper.appendChild(customerDiv);

                        // Phần 2: CollectPallet (background xanh dương, chữ trắng, đậm)
                        const collectDiv = document.createElement('div');
                        collectDiv.textContent = `COLLECTEDPL: ${extendedProps.collectPallet}`;
                        collectDiv.style.position = 'relative';
                        collectDiv.style.zIndex = '2';
                        collectDiv.style.backgroundColor = '#FFF2CC';  // Xanh dương trực quan cho Collect
                        collectDiv.style.color = '#000000';
                        collectDiv.style.padding = '4px 8px';
                        collectDiv.style.borderRadius = '4px';
                        collectDiv.style.fontSize = '11px';
                        collectDiv.style.fontWeight = 'bold';
                        collectDiv.style.textAlign = 'center';
                        collectDiv.style.minWidth = '70px';
                        wrapper.appendChild(collectDiv);

                        // Phần 3: ThreePointScan (background xanh lá, chữ trắng, đậm)
                        const threeDiv = document.createElement('div');
                        threeDiv.textContent = `3POINTCHECKED: ${extendedProps.threePointScan}`;
                        threeDiv.style.position = 'relative';
                        threeDiv.style.zIndex = '2';
                        threeDiv.style.backgroundColor = '#FFCE9F';  // Xanh lá trực quan cho Prepared
                        threeDiv.style.color = '#000000';
                        threeDiv.style.padding = '4px 8px';
                        threeDiv.style.borderRadius = '4px';
                        threeDiv.style.fontSize = '11px';
                        threeDiv.style.fontWeight = 'bold';
                        threeDiv.style.textAlign = 'center';
                        threeDiv.style.minWidth = '70px';
                        wrapper.appendChild(threeDiv);

                        // Phần 4: LoadCont (background cam, chữ đen, đậm)
                        const loadDiv = document.createElement('div');
                        loadDiv.textContent = `LOADCONT: ${extendedProps.loadCont}`;
                        loadDiv.style.position = 'relative';
                        loadDiv.style.zIndex = '2';
                        loadDiv.style.backgroundColor = '#F19C99';  // Cam trực quan cho Loaded
                        loadDiv.style.color = '#000000';
                        loadDiv.style.padding = '4px 8px';
                        loadDiv.style.borderRadius = '4px';
                        loadDiv.style.fontSize = '11px';
                        loadDiv.style.fontWeight = 'bold';
                        loadDiv.style.textAlign = 'center';
                        loadDiv.style.minWidth = '70px';
                        wrapper.appendChild(loadDiv);

                        // THÊM: Hover listener cho effect (đã có CSS, nhưng thêm sound subtle nếu delayMode)
                        wrapper.addEventListener('mouseenter', (e) => {
                            if (delayMode) {
                                // Optional: Play sound nhẹ khi hover (giảm volume tạm thời)
                                playBeep(0.2, 600, 100); // Beep nhẹ hơn cho hover
                            }
                            // Cập nhật nội dung tooltip với tất cả thông tin
                            const planTime = formatTimeRange(pStart, pEnd);
                            const actualTime = formatTimeRange(aStart, aEnd);
                            const delayTime = formatTimeRange(dStart, dEnd);
                            const tooltip = createTooltip();
                            tooltip.innerHTML = `
                                <h4>Order Information</h4>  <!-- SỬA: Sửa lỗi chính tả từ "Infomation" sang "Information" -->
                                <dl>
                                    <dt>ShipDate:</dt>
                                    <dd>${extendedProps.shipDate}</dd>
                                    <dt>Plan Time:</dt>
                                    <dd>${planTime}</dd>
                                    <dt>Actual Time:</dt>
                                    <dd>${actualTime}</dd>
                                    ${status === 'Delay' ? `<dt>Delay Time:</dt><dd>${delayTime}</dd>` : ''}
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

                            // Position tooltip dưới event
                            const rect = e.currentTarget.getBoundingClientRect();
                            tooltip.style.left = (rect.left + window.scrollX) + 'px';
                            tooltip.style.top = (rect.bottom + window.scrollY + 5) + 'px';

                            // Hiển thị ngay lập tức với class show
                            tooltip.classList.add('show');
                        });

                        wrapper.addEventListener('mouseleave', () => {
                            // Ẩn tooltip
                            const tooltip = document.getElementById('custom-tooltip');
                            tooltip.classList.remove('show');
                        });

                        // Optional: Follow mouse nếu muốn, nhưng giữ fixed dưới event cho đơn giản
                        // wrapper.addEventListener('mousemove', (e) => {
                        //     tooltip.style.left = (e.pageX + 10) + 'px';
                        //     tooltip.style.top = (e.pageY - 10) + 'px';
                        // });

                        return wrapper;
                    })()
                ]
            };
        }
    });
    calendar.render();

    // --- THÊM MỚI: SignalR cho realtime status update (FIX: Cải thiện update logic - remove refetch/changeDate, chỉ render)
    let connection = null;

    function initSignalR() {
        // Kiểm tra SignalR mới có load chưa
        if (typeof signalR === 'undefined') {
            console.error('SignalR client not loaded. Please include signalr.js.');
            return;
        }

        // Tạo connection với Hub URL
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/orderHub')  // URL hub từ backend
            .configureLogging(signalR.LogLevel.Information)  // Optional: Log level
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

            console.log('SignalR refetch starting...');  // ← THÊM LOG BẮT ĐẦU

            // Refetch data từ server để update calendar
            fetch('/DensoWareHouse/GetCalendarData')
                .then(response => {
                    console.log('SignalR response status:', response.status);  // ← THÊM LOG STATUS
                    if (!response.ok) throw new Error('SignalR refetch failed');
                    return response.json();
                })
                .then(data => {
                    console.log('SignalR refetched data:', data);  // ← THÊM LOG DATA RAW
                    // Log chi tiết từng order
                    if (data.orders) {
                        data.orders.forEach((order, idx) => {
                            console.log(`Order[${idx}]:`, order);
                        });
                    }
                    console.log('SignalR orders length:', data.orders ? data.orders.length : 0);  // ← THÊM LOG LENGTH

                    const fetchedOrders = data.orders;
                    const fetchedCustomers = data.customers;

                    // THÊM: Đảm bảo resourceId của mọi event đều có trong resources
                    const allResourceIds = Array.from(new Set(fetchedOrders.map(o => o.resource)));
                    if (fetchedCustomers) {
                        const customerIds = fetchedCustomers.map(c => c.CustomerCode);
                        allResourceIds.forEach(rid => {
                            if (!customerIds.includes(rid)) {
                                fetchedCustomers.push({ CustomerCode: rid, CustomerName: rid });
                            }
                        });
                    }

                    // Rebuild customerMap từ local
                    const customerMap = {};
                    fetchedCustomers.forEach(c => {
                        customerMap[c.CustomerCode] = c.CustomerName;
                    });

                    // Rebuild resources từ local
                    const resources = fetchedCustomers.map(c => ({
                        id: c.CustomerCode,
                        title: c.CustomerCode
                    }));

                    // Rebuild eventsData từ local (copy logic từ code gốc - với delay info + LOG/FALLBACK)
                    const newEventsData = fetchedOrders.map((order) => {  // ← BỎ index, dùng UId cho id
                        function parseAndValidate(timeStr) {
                            if (!timeStr) return null;
                            console.log(`SignalR Parsing time for ${order.uId}: "${timeStr}"`);  // ← THÊM LOG RAW TIME
                            const d = new Date(timeStr);
                            if (isNaN(d.getTime())) {
                                console.warn(`SignalR Invalid time parse for ${order.uId}: "${timeStr}" → null`);  // ← THÊM LOG FAIL
                                return null;
                            }
                            console.log(`SignalR Parsed OK: ${d.toISOString()}`);  // ← THÊM LOG SUCCESS
                            return d;
                        }

                        const planStart = parseAndValidate(order.startTime);
                        const planEnd = parseAndValidate(order.endTime);
                        const actualStart = parseAndValidate(order.acStartTime);
                        const actualEnd = parseAndValidate(order.acEndTime);

                        // THÊM LOG: Check raw values
                        console.log(`SignalR Order ${order.uId}: planStart="${order.startTime}", planEnd="${order.endTime}", actualStart="${order.acStartTime}", actualEnd="${order.acEndTime}"`);  // ← THÊM RAW TIMES

                        let validPlan = planStart && planEnd && planStart < planEnd;
                        let validActual = actualStart && actualEnd && actualStart < actualEnd;

                        // FIX: Fallback nếu chỉ có plan nhưng invalid (e.g., EndTime null sau delay) - dùng current time dummy
                        if (!validPlan && order.startTime && !order.endTime) {  // Chỉ StartTime có, EndTime null
                            validPlan = true;
                            planEnd = new Date(planStart.getTime() + 60 * 60 * 1000);  // Dummy 1h end
                            console.log(`SignalR Fallback dummy end for ${order.uId}: ${planEnd.toISOString()}`);  // ← THÊM LOG FALLBACK
                        }
                        if (!validActual && order.acStartTime && !order.acEndTime) {
                            validActual = true;
                            actualEnd = new Date(actualStart.getTime() + 60 * 60 * 1000);  // Dummy
                            console.log(`SignalR Fallback dummy actual end for ${order.uId}`);  // ← THÊM LOG
                        }

                        let eventStart, eventEnd;
                        let status = statusMap[parseInt(order.status, 10)] || 'Planned';

                        console.log(`SignalR Rebuilding order ${order.uId}: Status=${order.status} (${status}), ValidPlan=${validPlan}, ValidActual=${validActual}`);  // ← THÊM LOG REBUILD

                        if (validActual) {
                            eventStart = actualStart;
                            eventEnd = actualEnd;
                        } else if (validPlan) {
                            eventStart = planStart;
                            eventEnd = planEnd;
                        } else {
                            console.warn(`SignalR Skipping invalid order ${order.uId}: No valid times even after fallback`);  // ← THÊM LOG SKIP
                            return null;
                        }

                        let hasBoth = false;
                        if (validActual && validPlan) {
                            eventStart = new Date(Math.min(actualStart, planStart));
                            eventEnd = new Date(Math.max(actualEnd, planEnd));
                            hasBoth = true;
                        }

                        const customerCode = order.customerCode || order.resource || 'Unknown';

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
                            id: `order-${order.uId}`,  // ← SỬA: Dùng UId thay index để unique
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

                    console.log('SignalR newEventsData length:', newEventsData.length);  // ← THÊM LOG LENGTH

                    if (newEventsData.length === 0) {
                        console.error('SignalR: No events after rebuild! Skipping update to avoid blank calendar.');
                        return;  // ← THÊM: Không update nếu fail, tránh mất events (fallback manual sẽ chạy sau)
                    }

                    // Update calendar options và events (FIX: Clear sources đúng cách, chỉ render)
                    calendar.setOption('resources', resources);
                    // Log resources hiện tại
                    console.log('Current resources:', resources.map(r => r.id));
                    calendar.removeAllEventSources();  // ← GIỮ, clear tất cả sources cũ
                    const formattedEvents = newEventsData.map(e => {
                        const extendedProps = e.extendedProps;
                        const status = extendedProps.status;
                        const validActual = extendedProps.validActual;
                        const delayTime = extendedProps.delayTime || 0;

                        const bgColor = e.hasBoth ? 'transparent' : getColorByStatus(status, validActual, delayTime);
                        const borderColor = e.hasBoth ? 'transparent' : getColorByStatus(status, validActual, delayTime);

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
                        console.log(`Event[${idx}] resourceId:`, ev.resourceId, '| id:', ev.id, '| start:', ev.start, '| end:', ev.end);
                    });

                    console.log('SignalR Calendar updated with new data.');
                    // Log để debug sau khi FullCalendar tự render
                    setTimeout(() => {
                        console.log('SignalR Events after refetch:', calendar.getEvents().length);
                        console.log('SignalR Visible events in DOM:', document.querySelectorAll('.fc-event').length);
                    }, 100);
                })
                .catch(error => {
                    console.error('SignalR Error refetching calendar data:', error);  // ← THÊM LOG ERROR
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

    // --- Vạch đỏ hiển thị thời gian hiện tại (FIX: Giảm z-index và ẩn khi modal mở)
    const calComputed = window.getComputedStyle(calendarEl);
    if (calComputed.position === 'static') calendarEl.style.position = 'relative';

    const customNow = document.createElement('div');
    Object.assign(customNow.style, {
        position: 'absolute',
        width: '4px',  // Tăng từ 2px lên 4px để dễ nhìn hơn
        backgroundColor: 'red',
        zIndex: 10,  // FIX: Giảm từ 9999 xuống 10 (dưới modal 1050)
        top: '0px',  // Đặt top ở 0 để bắt đầu từ đầu bảng
        height: '100%',  // Chiếm hết chiều cao của bảng
        transition: 'left 0.5s linear',
        left: '0px'  // SỬA: Ban đầu set left=0 cho midnight mode
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
        transform: 'translate(-50%, -120%)',
        zIndex: 11,  // FIX: Giảm từ 10000 xuống 11 (vẫn trên vạch nhưng dưới modal)
        boxShadow: '0 1px 3px rgba(0,0,0,0.3)',
        left: '0px'  // SỬA: Ban đầu set left=0 cho midnight mode
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

    // SỬA: positionCustomNowIndicator() - Adjust cho midnight mode (ban đầu ở 0)
    function positionCustomNowIndicator() {
        const now = new Date();
        const nowHour = now.getHours();
        const minutes = now.getMinutes();
        const seconds = now.getSeconds();
        const percent = (minutes * 60 + seconds) / 3600;
        const cells = getHeaderHourCells();
        if (cells.length === 0) return;

        let left;
        if (isMidnightMode && nowHour < 6) {
            // Midnight mode: Position từ đầu (00:00), percent dựa trên giờ hiện tại
            const firstHour = 0;  // Bắt đầu từ 00:00
            let index = nowHour - firstHour;
            if (index < 0) index = 0;
            if (index >= cells.length) index = cells.length - 1;

            const cell = cells[index];
            const cellRect = cell.getBoundingClientRect();
            const calRect = calendarEl.getBoundingClientRect();
            left = (cellRect.left - calRect.left) + percent * cellRect.width;
        } else {
            // Logic cũ: Position dựa trên giờ hiện tại so với first slot
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
        clockLabel.style.top = '0px';  // Đặt clockLabel ở top của bảng
    }

    function updateClockLabel() {
        const now = new Date();
        const hh = String(now.getHours()).padStart(2, '0');
        const mm = String(now.getMinutes()).padStart(2, '0');
        clockLabel.textContent = `${hh}:${mm}`;
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

    // SỬA: updateVisibleRange() - Thêm detect midnight và switch mode lúc 06:00
    function updateVisibleRange() {
        const now = new Date();
        const nowHour = now.getHours();

        // Detect midnight (00:00) và chuyển ngày mới
        if (nowHour === 0 && now.getMinutes() === 0) {  // Chính xác lúc 00:00:00
            console.log('Midnight detected! Switching to new day and midnight mode.');
            calendar.today();  // Nhảy sang ngày mới
            isMidnightMode = true;  // Bật midnight mode
            const midnightRange = getTimeRange(true);  // Mode đặc biệt
            calendar.setOption('slotMinTime', midnightRange.startHour);
            calendar.setOption('slotMaxTime', midnightRange.endHour);
            calendar.setOption('scrollTime', midnightRange.currentShort);
            // Reset position now về 0
            customNow.style.left = '0px';
            clockLabel.style.left = '0px';
        } else if (isMidnightMode && nowHour >= 6) {
            // Switch về logic cũ khi đủ 6 tiếng
            console.log('6 hours passed in midnight mode! Switching back to normal logic.');
            isMidnightMode = false;
            const normalRange = getTimeRange(false);  // Logic cũ
            calendar.setOption('slotMinTime', normalRange.startHour);
            calendar.setOption('slotMaxTime', normalRange.endHour);
            calendar.setOption('scrollTime', normalRange.currentShort);
        } else if (!isMidnightMode) {
            // Logic cũ bình thường
            const normalRange = getTimeRange(false);
            calendar.setOption('slotMinTime', normalRange.startHour);
            calendar.setOption('slotMaxTime', normalRange.endHour);
            calendar.setOption('scrollTime', normalRange.currentShort);
        }
        // Nếu vẫn midnight mode (giờ <6), update range với mode true
        else if (isMidnightMode) {
            const midnightRange = getTimeRange(true);
            calendar.setOption('slotMinTime', midnightRange.startHour);
            calendar.setOption('slotMaxTime', midnightRange.endHour);
            calendar.setOption('scrollTime', midnightRange.currentShort);
        }
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
        updateVisibleRange();  // ← SỬA: Luôn update full range với detect midnight/switch
    }, 60 * 1000);

    // SỬA MỚI: Khi mở delayModal, set hidden OrderId từ event + Set StartTime = max(now, event.start) cho future events (editable)
    document.getElementById('delayModal').addEventListener('show.bs.modal', function (event) {
        const uid = window.currentDelayUid || '';  // Set global trước khi show
        document.querySelector('#delayModal input[name="OrderId"]').value = uid;

        // THÊM MỚI: Set StartTime = max(now, event.planStart hoặc event.start) cho future delay
        if (window.currentDelayEvent) {
            const event = window.currentDelayEvent;
            const now = new Date();
            let defaultStart = now;
            const planStart = event.extendedProps.planStart ? new Date(event.extendedProps.planStart) : event.start;
            if (planStart > now) {
                defaultStart = planStart;  // Sử dụng planStart nếu future
            }
            document.querySelector('#delayModal input[name="StartTime"]').value = defaultStart.toISOString().slice(0, 16);
        } else {
            // Fallback nếu không có event info
            const now = new Date();
            document.querySelector('#delayModal input[name="StartTime"]').value = now.toISOString().slice(0, 16);
        }

        // THÊM: Làm StartTime editable (remove readonly nếu có)
        const startInput = document.querySelector('#delayModal input[name="StartTime"]');
        startInput.removeAttribute('readonly');  // Làm editable
    });

    // SỬA: Xử lý save button cho delay modal - GỌI API POST ĐỂ SAVE VÀO DELAYHISTORY + UPDATE STATUS SANG DELAY + REFETCH DATA + SỬA: Sử dụng input StartTime/ChangeTime + THÊM: Switch to week view nếu delayTime >24h (FIX: BỎ REFETCH THỦ CÔNG, DÙNG SIGNALR)
    document.addEventListener('click', function (e) {
        if (e.target.id === 'saveDelay') {
            console.log('SAVE DELAY CLICKED! Starting handler...');
            const form = document.getElementById('delayForm');
            const formData = new FormData(form);
            const data = {
                OrderId: document.querySelector('input[name="OrderId"]').value || '',  // THÊM: Lấy OrderId từ hidden input (sẽ set khi mở modal)
                DelayType: parseInt(formData.get('DelayType')) || 0,
                Reason: formData.get('Reason') || '',
                StartTime: formData.get('StartTime') || '',  // SỬA: Lấy từ input (editable)
                ChangeTime: new Date().toISOString(),  // Default ChangeTime = now (có thể editable nếu thêm field)
                DelayTime: parseFloat(formData.get('DelayTime')) || 0
            };

            // THÊM MỚI: Gọi API POST để save
            fetch('/api/DelayHistory/SaveDelay', { // Route mới trong DelayController
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
                        alert('Delay applied and status updated to Delay!');
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
});