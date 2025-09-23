$(document).ready(function () {
    console.log("customer.js loaded"); // Xác nhận tệp được tải

    // Thêm nhà cung cấp
    $('#btnAddSupplier').click(function () {
        var data = {
            CustomerCode: $('#customerCode').val(),
            CustomerName: $('#customerName').val(),
            Descriptions: $('#description').val()
        };
        console.log("Add data being sent:", data);
        $.ajax({
            url: addSupplierUrl,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Add response:", response);
                if (response.success) {
                    $('#addSupplierMessage')
                        .text("Thêm nhà cung cấp thành công!")
                        .removeClass('error')
                        .addClass('success');
                    $('#addSupplierForm')[0].reset();
                    location.reload();
                } else {
                    $('#addSupplierMessage')
                        .text(response.message)
                        .removeClass('success')
                        .addClass('error');
                }
            },
            error: function (xhr, status, error) {
                console.log("Add AJAX error:", xhr, status, error);
                $('#addSupplierMessage')
                    .text("Đã xảy ra lỗi khi thêm")
                    .removeClass('success')
                    .addClass('error');
            }
        });
    });

    // Điền dữ liệu vào form cập nhật
    window.populateUpdateForm = function (customerCode, customerName, descriptions) {
        $('#updateCustomerCode').val(customerCode);
        $('#updateCustomerName').val(customerName);
        $('#updateDescription').val(descriptions);
    };

    // Cập nhật nhà cung cấp
    $('#btnUpdateSupplier').click(function () {
        var data = {
            CustomerCode: $('#updateCustomerCode').val(),
            CustomerName: $('#updateCustomerName').val(),
            Descriptions: $('#updateDescription').val()
        };
        console.log("Update data being sent:", data);
        $.ajax({
            url: updateSupplierUrl,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Update response:", response);
                if (response.success) {
                    $('#updateSupplierMessage')
                        .text("Cập nhật nhà cung cấp thành công!")
                        .removeClass('error')
                        .addClass('success');
                    location.reload();
                } else {
                    $('#updateSupplierMessage')
                        .text(response.message)
                        .removeClass('success')
                        .addClass('error');
                }
            },
            error: function (xhr, status, error) {
                console.log("Update AJAX error:", xhr, status, error);
                $('#updateSupplierMessage')
                    .text("Đã xảy ra lỗi khi cập nhật")
                    .removeClass('success')
                    .addClass('error');
            }
        });
    });

    // Xóa nhà cung cấp
    window.deleteSupplier = function (customerCode) {
        if (confirm("Bạn có chắc chắn muốn xóa nhà cung cấp này?")) {
            $.ajax({
                url: deleteSupplierUrl + '?code=' + encodeURIComponent(customerCode),
                type: 'POST',
                contentType: 'application/json',
                success: function (response) {
                    console.log("Delete response:", response);
                    if (response.success) {
                        alert("Xóa nhà cung cấp thành công!");
                        location.reload();
                    } else {
                        alert(response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.log("Delete AJAX error:", xhr, status, error);
                    alert("Đã xảy ra lỗi khi xóa");
                }
            });
        }
    };
});