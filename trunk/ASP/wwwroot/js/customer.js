$(document).ready(function () {
    $('#btnAddSupplier').click(function () {
        var data = {
            CustomerCode: $('#customerCode').val(),
            CustomerName: $('#customerName').val(),
            Descriptions: $('#description').val()
        };

        $.ajax({
            url: addSupplierUrl,
            type: 'POST',
            data: JSON.stringify(data), 
            contentType: 'application/json', 
            success: function (response) {
                if (response.success) {
                    $('#addSupplierMessage').text("Thêm nhà cung cấp thành công!");
                    $('#addSupplierForm')[0].reset();
                    location.reload(); 
                } else {
                    $('#addSupplierMessage').text(response.message).css('color', 'red');
                }
            },
            error: function () {
                $('#addSupplierMessage').text("Đã xảy ra lỗi").css('color', 'red');
            }
        });
    });
});