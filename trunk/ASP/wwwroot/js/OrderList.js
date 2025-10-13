$(document).ready(function () {
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 800,
            once: true
        });
    }
});

function loadDelayHistory(uid) {
    console.log('Function called with UID:', uid); // Debug: Xác nhận hàm chạy

    // Show loading state
    $('#delayTable tbody').html('<tr><td colspan="5" class="text-center py-4"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></td></tr>');

    // Show modal NGAY LẬP TỨC để UX tốt hơn (trước AJAX)
    $('#delayModal').modal('show');
    console.log('Modal should show now'); // Debug: Kiểm tra modal trigger

    // AJAX call
    $.get('/api/DelayHistory/' + uid, function (data) {
        console.log('AJAX success:', data); // Debug: Log data nhận được
        $('#delayTable tbody').empty();
        if (data && data.length > 0) {
            $.each(data, function (i, item) {
                $('#delayTable tbody').append(
                    '<tr>' +
                    '<td class="fw-semibold">' + item.DelayType + '</td>' +
                    '<td class="text-muted">' + item.Reason + '</td>' +
                    '<td><span class="badge bg-light text-dark px-2 py-1">' + new Date(item.StartTime).toLocaleString() + '</span></td>' +
                    '<td><span class="badge bg-light text-dark px-2 py-1">' + new Date(item.ChangeTime).toLocaleString() + '</span></td>' +
                    '<td class="fw-semibold text-warning">' + item.DelayTime + '</td>' +
                    '</tr>'
                );
            });
        } else {
            $('#delayTable tbody').append('<tr><td colspan="5" class="text-center py-4 text-muted"><i class="bi bi-info-circle fs-3 mb-2"></i><br>No delay history found.</td></tr>');
        }
    }).fail(function (xhr, status, error) {
        console.error('AJAX Error:', status, error, xhr.responseText); // Debug: Log lỗi AJAX
        $('#delayTable tbody').html('<tr><td colspan="5" class="text-center py-4 text-danger"><i class="bi bi-exclamation-triangle fs-3 mb-2"></i><br>Error loading data. Please try again.</td></tr>');
        // Xóa alert để tránh spam; dùng console để debug
    });
}