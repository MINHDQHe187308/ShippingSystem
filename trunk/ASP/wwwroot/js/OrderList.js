$(document).ready(function () {
    // Optional AOS - chỉ init nếu có, tránh lỗi
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 600,
            once: true,
            offset: 100
        });
    }
});

// Function loadDelayHistory
function loadDelayHistory(uid) {
    if (!uid) {
        console.error('UID is undefined or empty');
        alert('ID đơn hàng không hợp lệ. Vui lòng làm mới và thử lại.');
        return;
    }
    console.log('Function called with UID:', uid);
    // Hiển thị trạng thái loading với animation
    const loadingRow = '<tr><td colspan="8" class="text-center py-4"><div class="spinner-border spinner-border-sm text-primary mx-auto mb-2 animate-spin" role="status"><span class="visually-hidden">Loading...</span></div><p class="text-muted small animate-fade-in">Loading delay history...</p></td></tr>';
    $('#delayTable tbody').html(loadingRow);
    // Hiển thị modal với animation
    $('#delayModal').modal('show');
    console.log('Modal shown');
    // Gọi AJAX
    $.get(window.appBaseUrl + 'api/DelayHistory/' + encodeURIComponent(uid), function (data) {
        console.log('AJAX success:', data);
        $('#delayTable tbody').empty();
        if (data && data.length > 0) {
            $.each(data, function (i, item) {
                function parseSafeDate(dateStr) {
                    if (!dateStr) return 'N/A';
                    let isoStr = dateStr.toString().replace(' ', 'T');
                    if (isoStr.includes('.')) {
                        let parts = isoStr.split('.');
                        isoStr = parts[0] + (parts[1].length > 3 ? '.000Z' : '.' + parts[1] + 'Z');
                    } else {
                        isoStr += 'Z';
                    }
                    let parsed = new Date(isoStr);
                    return isNaN(parsed.getTime()) ? 'Invalid Date' : parsed.toLocaleString();
                }
                const rowHtml = '<tr class="animate-slide-in-up">' +
                    '<td class="fw-semibold text-danger ps-4 py-3 animate-fade-in-delay">' + (item.delayType || 'N/A') + ' <i class="bi bi-exclamation-triangle text-danger animate-pulse-slow"></i></td>' +
                    '<td class="text-muted ps-4 py-3 animate-fade-in-delay">' + (item.reason || 'N/A') + '</td>' +
                    '<td class="ps-4 py-3 animate-fade-in-delay"><span class="badge bg-info text-dark px-3 py-2 rounded-pill animate-scale-in">' + parseSafeDate(item.startTime) + '</span></td>' +
                    '<td class="fw-semibold text-warning ps-4 py-3 animate-fade-in-delay">' + (item.delayTime || 'N/A') + ' <i class="bi bi-stopwatch text-warning animate-pulse-slow"></i></td>' +
                    '</tr>';
                $('#delayTable tbody').append(rowHtml);
            });
        } else {
            $('#delayTable tbody').append('<tr class="animate-slide-in-up"><td colspan="4" class="text-center py-4 text-muted animate-fade-in"><i class="bi bi-info-circle fs-1 mb-2 text-info animate-rotate-slow"></i><br>No delay history found.</td></tr>');
        }
    }).fail(function (xhr, status, error) {
        console.error('AJAX Error:', status, error, xhr.responseText, xhr.status);
        $('#delayTable tbody').html('<tr class="animate-slide-in-up"><td colspan="4" class="text-center py-4 text-danger animate-shake"><i class="bi bi-exclamation-triangle fs-1 mb-2 text-danger animate-pulse-fast"></i><br>Error loading data (' + status + '). Check console for details.</td></tr>');
    });
}

// Function exportToExcel
function exportToExcel(uid) {
    if (!uid) {
        alert('ID đơn hàng không hợp lệ. Vui lòng làm mới và thử lại.');
        return;
    }
    // Thêm animation cho button trước khi redirect
    const btn = event.target.closest('button');
    if (btn) {
        $(btn).addClass('animate-scale-out').prop('disabled', true);
    }
    setTimeout(() => {
        window.location.href = window.appBaseUrl + 'Order/ExportExcel?orderId=' + encodeURIComponent(uid);
    }, 300);
}