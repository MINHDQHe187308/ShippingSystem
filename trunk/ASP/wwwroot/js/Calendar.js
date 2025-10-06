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

    // --- Thêm CSS styles cho z-index, height, centering và CUSTOM TOOLTIP
    const style = document.createElement('style');
    style.textContent = `
        .fc-event {
            height: 30px !important;  /* Tăng kích thước event để dễ nhìn */
            display: flex !important;
            align-items: center !important;  /* Căn giữa theo chiều dọc trong slot */
            justify-content: center !important;  /* Căn giữa text nếu cần */
            margin: 0 !important;
            line-height: 1.2 !important;
            cursor: pointer;  /* Thêm cursor pointer để dễ nhận biết hover */
        }
        .fc-event.plan-event {
            z-index: 1;
        }
        .fc-event.actual-event {
            z-index: 2;
            height: 30px !important;  /* Đồng bộ height với .fc-event */
            line-height: 1.2 !important;
        }
        .fc-event .fc-event-main {  /* Đảm bảo inner content cũng center */
            display: flex;
            align-items: center;
            height: 100%;
            font-size: 14px;  /* Tăng font-size để dễ đọc hơn */
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
        #custom-tooltip .status.planned { background: #000; color: #fff; }
        #custom-tooltip .status.pending { background: #007bff; color: #fff; }
        #custom-tooltip .status.shipped { background: #ffc107; color: #000; }
        #custom-tooltip .status.completed { background: #28a745; color: #fff; }
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

    // Tạo resources từ customers hiển thị CustomerCode theo CustomerCode
    const resources = customers.map(c => ({
        id: c.CustomerCode,
        title: c.CustomerCode  // SỬA: Hiển thị CustomerCode thay vì CustomerName
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

    // --- Tạo event data từ dữ liệu Order - THÊM UId VÀO EXTENDEDPROPS
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
        return {
            id: `order-${index}`,
            resourceId: customerCode,
            start: eventStart,
            end: eventEnd,
            title: order.TotalPallet ? order.TotalPallet.toString() : '0',  // SỬA: Hiển thị TotalPallet thay vì customerCode
            // Xóa status ở root level, di chuyển vào extendedProps
            hasBoth: hasBoth,
            extendedProps: {
                uid: order.UId,  // THÊM: Để sử dụng trong eventClick
                planStart: validPlan ? planStart.toISOString() : null,  // Lưu ISO string để tránh re-parse
                planEnd: validPlan ? planEnd.toISOString() : null,
                actualStart: validActual ? actualStart.toISOString() : null,
                actualEnd: validActual ? actualEnd.toISOString() : null,
                validActual: validActual,
                status: status,  // ← SỬA: Di chuyển status vào extendedProps
                totalPallet: order.TotalPallet || 0,  // THÊM: Lưu TotalPallet vào extendedProps để dùng trong eventContent
                shipDate: order.ShipDate || 'N/A',
                transCd: order.TransCd || 'N/A',
                transMethod: order.TransMethod || 'N/A',
                contSize: order.ContSize || 'N/A',
                totalColumn: order.TotalColumn || 0
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

        // THÊM MỚI: Event listener cho click event - Mở modal với OrderDetails - THÊM DEBUG LOGS
        eventClick: function (info) {
            console.log('Event clicked! Info:', info);  // DEBUG: Log toàn bộ info
            const uid = info.event.extendedProps.uid;
            console.log('Extracted UID:', uid);  // DEBUG: Kiểm tra UID
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
                    loadingEl.style.display = 'none';
                    if (data.success && data.data && data.data.length > 0) {
                        // Build table rows
                        data.data.forEach(item => {
                            const statusText = item.bookContStatus === 0 ? 'Chưa xuất' :
                                item.bookContStatus === 1 ? 'Đang xuất' :
                                    item.bookContStatus === 2 ? 'Đã xuất' : 'Không xác định';
                            const row = `
                                <tr>
                                    <td>${item.partNo || 'N/A'}</td>
                                    <td>${item.quantity || 0}</td>
                                    <td>${item.totalPallet || 0}</td>
                                    <td>${item.palletSize || 'N/A'}</td>
                                    <td>${item.warehouse || 'N/A'}</td>
                                    <td>${item.contNo || 'N/A'}</td>
                                    <td><span class="badge bg-secondary">${statusText}</span></td>
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

        // --- Custom eventContent: VẼ EVENT VÀ ATTACH HOVER EVENTS CHO TOOLTIP
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

            // Debug log cho rendering (xóa sau khi test OK)
            console.log('Rendering event:', arg.event.title, 'Status:', status, 'ValidActual:', extendedProps.validActual, 'Color for bar:', getColorByStatus(status));

            return {
                domNodes: [
                    (() => {
                        const wrapper = document.createElement('div');
                        // FIX: Fallback bg nếu transparent để tránh xám hoàn toàn
                        wrapper.style.position = 'relative';
                        wrapper.style.height = '100%';  // Đồng bộ với height của .fc-event (30px)
                        wrapper.style.width = '100%';
                        wrapper.style.background = arg.event.backgroundColor || getColorByStatus(status) || 'transparent';
                        wrapper.style.borderRadius = '4px';
                        wrapper.style.overflow = 'visible';  // Cho phép extend visible
                        wrapper.style.color = arg.event.textColor;
                        wrapper.style.display = 'flex';  // Thêm flex để hỗ trợ centering nếu CSS chưa apply
                        wrapper.style.alignItems = 'center';
                        wrapper.style.justifyContent = 'center';

                        // Vẽ actual bar (nếu có) - dùng màu theo status DB
                        if (aStart && aEnd) {
                            const actualBar = document.createElement('div');
                            actualBar.style.position = 'absolute';
                            actualBar.style.left = Math.max(0, actualPercent) + '%';
                            actualBar.style.top = '0';
                            actualBar.style.height = '100%';
                            actualBar.style.width = actualWidth + '%';
                            actualBar.style.background = getColorByStatus(status);
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
                            planBarFull.style.background = getColorByStatus(status);
                            planBarFull.style.borderRadius = '4px';
                            wrapper.appendChild(planBarFull);
                        }

                        // Vẽ plan bar overlay (nếu có plan, và không phải chỉ plan)
                        if (pStart && pEnd && !(aStart && aEnd && !extendedProps.validActual)) {
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

                        // Text (luôn hiển thị) - SỬA: HIỂN THỊ TOTALPALLET VỚI NHÃN, MÀU TRẮNG, ĐẬM
                        const text = document.createElement('span');
                        text.textContent = `TotalPallet: ${extendedProps.totalPallet ? extendedProps.totalPallet.toString() : '0'}`;
                        text.style.position = 'relative';
                        text.style.zIndex = '2';
                        text.style.paddingLeft = '4px';
                        text.style.fontSize = '15px';
                        text.style.color = 'white';
                        text.style.fontWeight = 'bold';
                        wrapper.appendChild(text);

                        // --- ATTACH HOVER EVENTS CHO CUSTOM TOOLTIP - HIỂN THỊ NGAY LẬP TỨC
                        const tooltip = createTooltip();

                        wrapper.addEventListener('mouseenter', (e) => {
                            // Cập nhật nội dung tooltip với tất cả thông tin
                            const planTime = formatTimeRange(pStart, pEnd);
                            const actualTime = formatTimeRange(aStart, aEnd);
                            tooltip.innerHTML = `
                                <h4>Order Information</h4>  <!-- SỬA: Sửa lỗi chính tả từ "Infomation" sang "Information" -->
                                <dl>
                                    <dt>ShipDate:</dt>
                                    <dd>${extendedProps.shipDate}</dd>
                                    <dt>Plan Time:</dt>
                                    <dd>${planTime}</dd>
                                    <dt>Actual Time:</dt>
                                    <dd>${actualTime}</dd>
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
        transition: 'left 0.5s linear'
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

        // Vì height đã là 100% và top=0, không cần tính top và height nữa
        // Chỉ cập nhật left cho customNow và clockLabel
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