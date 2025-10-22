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

    // Load Leadtimes
    window.loadLeadtimes = function (customerCode) {
        $('#currentCustomerCodeLeadtime').val(customerCode);
        $.get(getLeadtimesUrl + '?customerCode=' + encodeURIComponent(customerCode), function (response) {
            console.log("Load leadtimes response:", response); // Debug
            if (response.success) {
                var html = '<table class="table table-hover"><thead class="table-light"><tr><th>Trans Cd</th><th>Collect Time</th><th>Prepare Time</th><th>Loading Time</th><th>Actions</th></tr></thead><tbody>';
                if (response.data.length === 0) {
                    html += '<tr><td colspan="5" class="text-center text-muted">No leadtimes found.</td></tr>';
                } else {
                    response.data.forEach(function (lt) {
                        html += '<tr><td>' + lt.transCd + '</td><td>' + lt.collectTimePerPallet + '</td><td>' + lt.prepareTimePerPallet + '</td><td>' + lt.loadingTimePerColumn + '</td><td><button class="btn btn-warning btn-sm" onclick="populateEditLeadtime(\'' + lt.customerCode + '\', \'' + lt.transCd + '\', ' + lt.collectTimePerPallet + ', ' + lt.prepareTimePerPallet + ', ' + lt.loadingTimePerColumn + ')">Edit</button> <button class="btn btn-danger btn-sm" onclick="deleteLeadtime(\'' + lt.customerCode + '\', \'' + lt.transCd + '\')">Delete</button></td></tr>';
                    });
                }
                html += '</tbody></table>';
                $('#leadtimeTableContainer').html(html);
                $('#leadtimeMessage').empty().removeClass('text-danger text-success');
            } else {
                $('#leadtimeMessage').text(response.message || 'Load failed').addClass('text-danger');
            }
        }).fail(function () {
            $('#leadtimeMessage').text('Network error loading leadtimes').addClass('text-danger');
        });
    };

    // Populate Edit Leadtime
    window.populateEditLeadtime = function (customerCode, transCd, collectTime, prepareTime, loadingTime) {
        $('#currentCustomerCodeLeadtimeEdit').val(customerCode);
        $('#transCd').val(transCd);
        $('#collectTime').val(collectTime);
        $('#prepareTime').val(prepareTime);
        $('#loadingTime').val(loadingTime);
        $('#addLeadtimeModalLabel').html('<i class="bi bi-pencil me-2"></i>Edit Leadtime');
        $('#btnSaveLeadtime').text('Update');
        $('#editLeadtimeId').val(transCd); // Use transCd as ID
        var addModal = new bootstrap.Modal(document.getElementById('addLeadtimeModal'));
        addModal.show();
    };

    // Save Leadtime
    $('#btnSaveLeadtime').click(function () {
        var isEdit = $('#editLeadtimeId').val() !== '';
        var url = isEdit ? updateLeadtimeUrl : addLeadtimeUrl;
        var data = {
            CustomerCode: $('#currentCustomerCodeLeadtimeEdit').val() || $('#currentCustomerCodeLeadtime').val(),
            TransCd: $('#transCd').val(),
            CollectTimePerPallet: parseFloat($('#collectTime').val()),
            PrepareTimePerPallet: parseFloat($('#prepareTime').val()),
            LoadingTimePerColumn: parseFloat($('#loadingTime').val())
        };
        console.log("Save leadtime data:", data); // Debug
        $.ajax({
            url: url,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Save leadtime response:", response); // Debug
                if (response.success) {
                    $('#leadtimeFormMessage').text(response.message || 'Success!').removeClass('text-danger').addClass('text-success');
                    loadLeadtimes($('#currentCustomerCodeLeadtime').val());
                    $('#addLeadtimeModal').modal('hide');
                    setTimeout(() => {
                        $('#leadtimeForm')[0].reset();
                        $('#editLeadtimeId').val('');
                        $('#addLeadtimeModalLabel').html('<i class="bi bi-plus-circle me-2"></i>Add Leadtime');
                        $('#btnSaveLeadtime').text('Save');
                    }, 500);
                } else {
                    $('#leadtimeFormMessage').text(response.message).addClass('text-danger');
                }
            },
            error: function (xhr) {
                console.log("Save leadtime error:", xhr); // Debug
                $('#leadtimeFormMessage').text('Error occurred: ' + xhr.status).addClass('text-danger');
            }
        });
    });

    // Delete Leadtime
    window.deleteLeadtime = function (customerCode, transCd) {
        if (confirm('Are you sure you want to delete this leadtime?')) {
            $.ajax({
                url: deleteLeadtimeUrl,
                type: 'POST',
                data: { customerCode: customerCode, transCd: transCd },
                success: function (response) {
                    console.log("Delete leadtime response:", response); // Debug
                    if (response.success) {
                        loadLeadtimes(customerCode);
                    } else {
                        alert(response.message || 'Delete failed');
                    }
                },
                error: function () {
                    alert('Error occurred during delete');
                }
            });
        }
    };

    // Load Shipping Schedules
    window.loadShippingSchedules = function (customerCode) {
        $('#currentCustomerCodeShipping').val(customerCode);
        $.get(getShippingSchedulesUrl + '?customerCode=' + encodeURIComponent(customerCode), function (response) {
            console.log("Load shipping response:", response); // Debug
            if (response.success) {
                var html = '<table class="table table-hover"><thead class="table-light"><tr><th>Trans Cd</th><th>Weekday</th><th>Cut Off Time</th><th>Description</th><th>Actions</th></tr></thead><tbody>';
                if (response.data.length === 0) {
                    html += '<tr><td colspan="5" class="text-center text-muted">No schedules found.</td></tr>';
                } else {
                    response.data.forEach(function (ss) {
                        var weekdayText = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][ss.weekday];
                        html += '<tr><td>' + ss.transCd + '</td><td>' + weekdayText + '</td><td>' + ss.cutOffTime + '</td><td>' + (ss.description || '') + '</td><td><button class="btn btn-warning btn-sm" onclick="populateEditShipping(\'' + ss.customerCode + '\', \'' + ss.transCd + '\', ' + ss.weekday + ', \'' + ss.cutOffTime + '\', \'' + (ss.description || '') + '\')">Edit</button> <button class="btn btn-danger btn-sm" onclick="deleteShipping(\'' + ss.customerCode + '\', \'' + ss.transCd + '\', ' + ss.weekday + ')">Delete</button></td></tr>';
                    });
                }
                html += '</tbody></table>';
                $('#shippingTableContainer').html(html);
                $('#shippingMessage').empty().removeClass('text-danger text-success');
            } else {
                $('#shippingMessage').text(response.message || 'Load failed').addClass('text-danger');
            }
        }).fail(function () {
            $('#shippingMessage').text('Network error loading schedules').addClass('text-danger');
        });
    };

    // Populate Edit Shipping
    window.populateEditShipping = function (customerCode, transCd, weekday, cutOffTime, description) {
        $('#currentCustomerCodeShippingEdit').val(customerCode);
        $('#shippingTransCd').val(transCd);
        $('#weekday').val(weekday);
        $('#cutOffTime').val(cutOffTime);
        $('#shippingDescription').val(description);
        $('#currentWeekday').val(weekday);
        $('#addShippingModalLabel').html('<i class="bi bi-pencil me-2"></i>Edit Shipping Schedule');
        $('#btnSaveShipping').text('Update');
        $('#editShippingId').val(transCd + '_' + weekday); // Composite ID
        var addModal = new bootstrap.Modal(document.getElementById('addShippingModal'));
        addModal.show();
    };

    // Save Shipping Schedule
    $('#btnSaveShipping').click(function () {
        var isEdit = $('#editShippingId').val() !== '';
        var url = isEdit ? updateShippingScheduleUrl : addShippingScheduleUrl;
        var data = {
            CustomerCode: $('#currentCustomerCodeShippingEdit').val() || $('#currentCustomerCodeShipping').val(),
            TransCd: $('#shippingTransCd').val(),
            Weekday: parseInt($('#weekday').val()),
            CutOffTime: $('#cutOffTime').val(),
            Description: $('#shippingDescription').val()
        };
        console.log("Save shipping data:", data); // Debug
        $.ajax({
            url: url,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Save shipping response:", response); // Debug
                if (response.success) {
                    $('#shippingFormMessage').text(response.message || 'Success!').removeClass('text-danger').addClass('text-success');
                    loadShippingSchedules($('#currentCustomerCodeShipping').val());
                    $('#addShippingModal').modal('hide');
                    setTimeout(() => {
                        $('#shippingForm')[0].reset();
                        $('#editShippingId').val('');
                        $('#currentWeekday').val('');
                        $('#addShippingModalLabel').html('<i class="bi bi-plus-circle me-2"></i>Add Shipping Schedule');
                        $('#btnSaveShipping').text('Save');
                    }, 500);
                } else {
                    $('#shippingFormMessage').text(response.message).addClass('text-danger');
                }
            },
            error: function (xhr) {
                console.log("Save shipping error:", xhr); // Debug
                $('#shippingFormMessage').text('Error occurred: ' + xhr.status).addClass('text-danger');
            }
        });
    });

    // Delete Shipping Schedule
    window.deleteShipping = function (customerCode, transCd, weekday) {
        if (confirm('Are you sure you want to delete this schedule?')) {
            $.ajax({
                url: deleteShippingScheduleUrl,
                type: 'POST',
                data: { customerCode: customerCode, transCd: transCd, weekday: weekday },
                success: function (response) {
                    console.log("Delete shipping response:", response); // Debug
                    if (response.success) {
                        loadShippingSchedules(customerCode);
                    } else {
                        alert(response.message || 'Delete failed');
                    }
                },
                error: function () {
                    alert('Error occurred during delete');
                }
            });
        }
    };

    // Reset forms on modal hide
    $('#addLeadtimeModal').on('hidden.bs.modal', function () {
        $('#leadtimeForm')[0].reset();
        $('#editLeadtimeId').val('');
        $('#currentCustomerCodeLeadtimeEdit').val('');
        $('#addLeadtimeModalLabel').html('<i class="bi bi-plus-circle me-2"></i>Add Leadtime');
        $('#btnSaveLeadtime').text('Save');
        $('#leadtimeFormMessage').empty().removeClass('text-success text-danger');
    });

    $('#addShippingModal').on('hidden.bs.modal', function () {
        $('#shippingForm')[0].reset();
        $('#editShippingId').val('');
        $('#currentCustomerCodeShippingEdit').val('');
        $('#currentWeekday').val('');
        $('#addShippingModalLabel').html('<i class="bi bi-plus-circle me-2"></i>Add Shipping Schedule');
        $('#btnSaveShipping').text('Save');
        $('#shippingFormMessage').empty().removeClass('text-success text-danger');
    });

    // Set customer name in modals (placeholder - enhance with AJAX if needed)
    $('#manageLeadtimeModal').on('shown.bs.modal', function () {
        var customerCode = $('#currentCustomerCodeLeadtime').val();
        $('#leadtimeCustomerName').text('Customer: ' + customerCode);
    });

    $('#manageShippingModal').on('shown.bs.modal', function () {
        var customerCode = $('#currentCustomerCodeShipping').val();
        $('#shippingCustomerName').text('Customer: ' + customerCode);
    });
});