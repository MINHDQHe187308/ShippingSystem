$(document).ready(function () {
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 800,
            once: true
        });
    }
});

function loadDelayHistory(uid) {
    if (!uid) {
        console.error('UID is undefined or empty'); // Debug: Bắt lỗi UID undefined sớm
        alert('ID đơn hàng không hợp lệ. Vui lòng làm mới và thử lại.');
        return;
    }
    console.log('Function called with UID:', uid); // Debug: Xác nhận UID

    // Hiển thị trạng thái loading
    $('#delayTable tbody').html('<tr><td colspan="4" class="text-center py-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></td></tr>');

    // Hiển thị modal ngay lập tức để UX tốt hơn
    $('#delayModal').modal('show');
    console.log('Modal shown'); // Debug: Kiểm tra modal

    // Gọi AJAX với mã hóa URL để an toàn
    $.get('/api/DelayHistory/' + encodeURIComponent(uid), function (data) {
        console.log('AJAX success:', data); // Debug: Log phản hồi đầy đủ
        $('#delayTable tbody').empty();
        if (data && data.length > 0) {
            $.each(data, function (i, item) {
                // Hàm hỗ trợ phân tích ngày tháng an toàn (xử lý định dạng khoảng trắng và vi giây)
                function parseSafeDate(dateStr) {
                    if (!dateStr) return 'N/A';
                    // Thay khoảng trắng bằng 'T' cho ISO, cắt vi giây nếu >3 chữ số thập phân
                    let isoStr = dateStr.toString().replace(' ', 'T');
                    if (isoStr.includes('.')) {
                        let parts = isoStr.split('.');
                        isoStr = parts[0] + (parts[1].length > 3 ? '.000Z' : '.' + parts[1] + 'Z');
                    } else {
                        isoStr += 'Z'; // Giả sử UTC nếu không có múi giờ
                    }
                    let parsed = new Date(isoStr);
                    return isNaN(parsed.getTime()) ? 'Invalid Date' : parsed.toLocaleString();
                }

                $('#delayTable tbody').append(
                    '<tr>' +
                    '<td class="fw-semibold">' + (item.delayType || 'N/A') + '</td>' +
                    '<td class="text-muted">' + (item.reason || 'N/A') + '</td>' +
                    '<td><span class="badge bg-light text-dark px-2 py-1">' + parseSafeDate(item.startTime) + '</span></td>' +
                    '<td class="fw-semibold text-warning">' + (item.delayTime || 'N/A') + '</td>' +
                    '</tr>'
                );
            });
        } else {
            $('#delayTable tbody').append('<tr><td colspan="4" class="text-center py-4 text-muted"><i class="bi bi-info-circle fs-3 mb-2"></i><br>No delay history found.</td></tr>');
        }
    }).fail(function (xhr, status, error) {
        console.error('AJAX Error:', status, error, xhr.responseText, xhr.status); // Debug: Chi tiết lỗi đầy đủ
        $('#delayTable tbody').html('<tr><td colspan="4" class="text-center py-4 text-danger"><i class="bi bi-exclamation-triangle fs-3 mb-2"></i><br>Error loading data (' + status + '). Check console for details.</td></tr>');
    });
}

// THÊM MỚI: Function để export Excel
function exportToExcel(uid) {
    if (!uid) {
        alert('ID đơn hàng không hợp lệ. Vui lòng làm mới và thử lại.');
        return;
    }
    // Redirect đến action export với orderId
    window.location.href = '/Order/ExportExcel?orderId=' + encodeURIComponent(uid);
}