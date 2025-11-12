$(document).ready(function () {
    console.log("customer.js loaded - jQuery ready"); // Debug: Xác nhận jQuery + script load
    // Validation helpers
    function validateShippingData(customerCode, transCd) {
        if (!customerCode || !transCd) {
            alert("Customer Code và Trans Cd không được để trống!");
            return false;
        }
        return true;
    }
    function validateLeadtimeData(customerCode, transCd) {
        if (!customerCode || !transCd) {
            alert("Customer Code và Trans Cd không được để trống!");
            return false;
        }
        return true;
    }
    function validateSupplierData(customerCode, customerName) {
        if (!customerCode || !customerName) {
            alert("Mã và tên nhà cung cấp không được để trống!");
            return false;
        }
        return true;
    }
    // Helper function to show messages
    function showMessage(element, message, type) {
        element.innerHTML = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        // Auto hide after 5s
        setTimeout(() => element.innerHTML = '', 5000);
    }
    // ===== IMPORT EXCEL: Event delegation để fix modal dynamic =====
    $(document).on('click', '#btnImportExcel', function () {
        console.log("Import button clicked! (delegation works)"); // Debug log
        const form = $('#importForm')[0];
        const fileInput = $('#excelFile')[0];
        const messageDiv = $('#importMessage')[0];
        console.log("File selected:", fileInput.files[0]?.name || 'none'); // Debug log
        if (!fileInput.files || fileInput.files.length === 0) {
            showMessage(messageDiv, 'Please select a file.', 'danger');
            return;
        }
        const formData = new FormData(form);
    var importUrl = (typeof importExcelUrl !== 'undefined') ? importExcelUrl : (window.appBaseUrl + 'Customer/ImportExcel');
        console.log("Import URL:", importUrl); // Debug log
        // Disable button to prevent double-click
        $(this).prop('disabled', true).html('<i class="bi bi-hourglass-split me-1"></i>Importing...');
        $.ajax({
            url: importUrl,
            type: 'POST',
            data: formData,
            processData: false, // Quan trọng cho FormData
            contentType: false, // Không set contentType cho multipart
            beforeSend: function () {
                console.log("Sending AJAX request..."); // Debug log
            },
            success: function (response) {
                console.log("Import response:", response); // Debug log
                if (response.success) {
                    showMessage(messageDiv, response.message, 'success');
                    // Reload table hoặc close modal sau 2s
                    setTimeout(() => {
                        location.reload(); // Hoặc gọi hàm reload table nếu có
                    }, 2000);
                } else {
                    showMessage(messageDiv, response.message || 'Import failed.', 'danger');
                }
            },
            error: function (xhr, status, error) {
                console.error('Import error details:', { status, error, response: xhr.responseText }); // Debug chi tiết
                showMessage(messageDiv, 'Network/Server error. Check console.', 'danger');
            },
            complete: function () {
                // Re-enable button
                $('#btnImportExcel').prop('disabled', false).html('<i class="bi bi-upload me-1"></i>Import');
            }
        });
    });
    // Log khi modal shown để confirm
    $('#importExcelModal').on('shown.bs.modal', function () {
        console.log("Import modal SHOWN - button should be ready now"); // Debug
        console.log("Button exists after shown:", $('#btnImportExcel').length > 0);
    });
    // Thêm nhà cung cấp
    $('#btnAddSupplier').click(function () {
        var customerCode = $('#customerCode').val().trim();
        var customerName = $('#customerName').val().trim();
        var descriptions = $('#description').val().trim();
        if (!validateSupplierData(customerCode, customerName)) return;
        var data = {
            CustomerCode: customerCode, // PascalCase cho model binding
            CustomerName: customerName,
            Descriptions: descriptions
        };
        console.log("Add data being sent:", data); // Debug log
        $.ajax({
            url: addSupplierUrl,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Add response:", response); // Debug log
                if (response.success) {
                    showMessage($('#addSupplierMessage')[0], "Thêm nhà cung cấp thành công!", 'success');
                    $('#addSupplierForm')[0].reset();
                    setTimeout(() => location.reload(), 1500);
                } else {
                    showMessage($('#addSupplierMessage')[0], response.message || "Thêm thất bại", 'danger');
                }
            },
            error: function (xhr, status, error) {
                console.error("Add AJAX error:", xhr, status, error); // Debug log
                showMessage($('#addSupplierMessage')[0], "Đã xảy ra lỗi khi thêm", 'danger');
            }
        });
    });
    // Điền dữ liệu vào form cập nhật
    window.populateUpdateForm = function (customerCode, customerName, descriptions) {
        $('#updateCustomerCode').val(customerCode);
        $('#updateCustomerName').val(customerName);
        $('#updateDescription').val(descriptions || '');
        console.log("Populated update form:", { customerCode, customerName }); // Debug log
    };
    // Cập nhật nhà cung cấp
    $('#btnUpdateSupplier').click(function () {
        var customerCode = $('#updateCustomerCode').val().trim();
        var customerName = $('#updateCustomerName').val().trim();
        var descriptions = $('#updateDescription').val().trim();
        if (!validateSupplierData(customerCode, customerName)) return;
        var data = {
            CustomerCode: customerCode,
            CustomerName: customerName,
            Descriptions: descriptions
        };
        console.log("Update data being sent:", data); // Debug log
        $.ajax({
            url: updateSupplierUrl,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Update response:", response); // Debug log
                if (response.success) {
                    showMessage($('#updateSupplierMessage')[0], "Cập nhật nhà cung cấp thành công!", 'success');
                    setTimeout(() => location.reload(), 1500);
                } else {
                    showMessage($('#updateSupplierMessage')[0], response.message || "Cập nhật thất bại", 'danger');
                }
            },
            error: function (xhr, status, error) {
                console.error("Update AJAX error:", xhr, status, error); // Debug log
                showMessage($('#updateSupplierMessage')[0], "Đã xảy ra lỗi khi cập nhật", 'danger');
            }
        });
    });
    // Xóa nhà cung cấp
    window.deleteSupplier = function (customerCode) {
        if (confirm("Bạn có chắc chắn muốn xóa nhà cung cấp này?")) {
            console.log("Deleting supplier:", customerCode); // Debug log
            $.ajax({
                url: deleteSupplierUrl + '?code=' + encodeURIComponent(customerCode),
                type: 'POST',
                contentType: 'application/json',
                success: function (response) {
                    console.log("Delete response:", response); // Debug log
                    if (response.success) {
                        // Dynamically remove the row from the table without full page reload
                        // Assuming the table has a tbody with tr rows, and customerCode is in the first td (adjust selector if needed)
                        const $rowToRemove = $(`tbody tr td:contains('${customerCode}')`).closest('tr');
                        if ($rowToRemove.length > 0) {
                            $rowToRemove.remove();
                            // Optional: Check if table is now empty and add "no data" row
                            const $tbody = $('tbody');
                            if ($tbody.find('tr').length === 0) {
                                $tbody.html('<tr><td colspan="100%" class="text-center text-muted">No suppliers found.</td></tr>'); // Adjust colspan to match your table
                            }
                        }
                        alert(response.message || "Chúc mừng bạn đã xóa nhà cung cấp thành công:))"); // Global message
                    } else {
                        alert(response.message || "Xóa thất bại");
                    }
                },
                error: function (xhr, status, error) {
                    console.error("Delete AJAX error:", xhr, status, error); // Debug log
                    alert("Đã xảy ra lỗi khi xóa");
                }
            });
        }
    };
    // Load Leadtimes
    window.loadLeadtimes = function (customerCode) {
        console.log("Loading leadtimes for:", customerCode); // Debug log
        $('#currentCustomerCodeLeadtime').val(customerCode);
        $.get(getLeadtimesUrl + '?customerCode=' + encodeURIComponent(customerCode))
            .done(function (response) {
                console.log("Load leadtimes response:", response); // Debug log
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
            })
            .fail(function (xhr) {
                console.error("Load leadtimes error:", xhr); // Debug log
                $('#leadtimeMessage').text('Network error loading leadtimes').addClass('text-danger');
            });
    };
    // Populate Edit Leadtime
    window.populateEditLeadtime = function (customerCode, transCd, collectTime, prepareTime, loadingTime) {
        console.log("Populating edit leadtime:", { customerCode, transCd }); // Debug log
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
        var customerCode = $('#currentCustomerCodeLeadtimeEdit').val().trim() || $('#currentCustomerCodeLeadtime').val().trim();
        var transCd = $('#transCd').val().trim();
        var collectTime = parseFloat($('#collectTime').val());
        var prepareTime = parseFloat($('#prepareTime').val());
        var loadingTime = parseFloat($('#loadingTime').val());
        if (!validateLeadtimeData(customerCode, transCd)) return;
        if (isNaN(collectTime) || isNaN(prepareTime) || isNaN(loadingTime)) {
            showMessage($('#leadtimeFormMessage')[0], 'Thời gian phải là số hợp lệ', 'danger');
            return;
        }
        var data = {
            CustomerCode: customerCode,
            TransCd: transCd,
            CollectTimePerPallet: collectTime,
            PrepareTimePerPallet: prepareTime,
            LoadingTimePerColumn: loadingTime
        };
        console.log("Save leadtime data:", data); // Debug log
        $.ajax({
            url: url,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Save leadtime response:", response); // Debug log
                if (response.success) {
                    showMessage($('#leadtimeFormMessage')[0], response.message || 'Success!', 'success');
                    loadLeadtimes($('#currentCustomerCodeLeadtime').val());
                    $('#addLeadtimeModal').modal('hide');
                    setTimeout(() => {
                        $('#leadtimeForm')[0].reset();
                        $('#editLeadtimeId').val('');
                        $('#addLeadtimeModalLabel').html('<i class="bi bi-plus-circle me-2"></i>Add Leadtime');
                        $('#btnSaveLeadtime').text('Save');
                    }, 500);
                } else {
                    showMessage($('#leadtimeFormMessage')[0], response.message, 'danger');
                }
            },
            error: function (xhr) {
                console.error("Save leadtime error:", xhr); // Debug log
                showMessage($('#leadtimeFormMessage')[0], 'Error occurred: ' + xhr.status, 'danger');
            }
        });
    });
    // Delete Leadtime
    window.deleteLeadtime = function (customerCode, transCd) {
        if (confirm('Are you sure you want to delete this leadtime?')) {
            console.log("Deleting leadtime:", { customerCode, transCd }); // Debug log
            $.ajax({
                url: deleteLeadtimeUrl,
                type: 'POST',
                data: { customerCode: customerCode, transCd: transCd },
                success: function (response) {
                    console.log("Delete leadtime response:", response); // Debug log
                    if (response.success) {
                        loadLeadtimes(customerCode);
                        showMessage(document.body, "Xóa leadtime thành công!", 'success');
                    } else {
                        alert(response.message || 'Delete failed');
                    }
                },
                error: function (xhr) {
                    console.error("Delete leadtime error:", xhr); // Debug log
                    alert('Error occurred during delete');
                }
            });
        }
    };
    // Load Shipping Schedules
    window.loadShippingSchedules = function (customerCode) {
        console.log("Loading shipping schedules for:", customerCode); // Debug log
        $('#currentCustomerCodeShipping').val(customerCode);
        $.get(getShippingSchedulesUrl + '?customerCode=' + encodeURIComponent(customerCode))
            .done(function (response) {
                console.log("Load shipping response:", response); // Debug log
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
            })
            .fail(function (xhr) {
                console.error("Load shipping error:", xhr); // Debug log
                $('#shippingMessage').text('Network error loading schedules').addClass('text-danger');
            });
    };
    // Populate Edit Shipping
    window.populateEditShipping = function (customerCode, transCd, weekday, cutOffTime, description) {
        console.log("Populating edit shipping:", { customerCode, transCd, weekday }); // Debug log
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
        var customerCode = $('#currentCustomerCodeShippingEdit').val().trim() || $('#currentCustomerCodeShipping').val().trim();
        var transCd = $('#shippingTransCd').val().trim();
        var weekday = parseInt($('#weekday').val());
        var cutOffTimeInput = $('#cutOffTime').val().trim();
        var description = $('#shippingDescription').val().trim();
        if (!validateShippingData(customerCode, transCd)) return;
        if (isNaN(weekday)) {
            showMessage($('#shippingFormMessage')[0], 'Weekday phải là số hợp lệ (0-6)', 'danger');
            return;
        }
        // Fix: Append ":00" nếu cutOffTime chỉ có HH:mm
        var cutOffTime = cutOffTimeInput;
        if (cutOffTimeInput && !cutOffTimeInput.includes(':')) {
            showMessage($('#shippingFormMessage')[0], 'Cut Off Time phải có định dạng HH:mm', 'danger');
            return;
        }
        if (cutOffTimeInput.split(':').length === 2) {
            cutOffTime = cutOffTimeInput + ':00'; // Thêm giây
        } else if (cutOffTimeInput.split(':').length !== 3) {
            showMessage($('#shippingFormMessage')[0], 'Cut Off Time phải có định dạng HH:mm:ss', 'danger');
            return;
        }
        var data = {
            CustomerCode: customerCode,
            TransCd: transCd,
            Weekday: weekday,
            CutOffTime: cutOffTime, // "13:00:00"
            Description: description
        };
        console.log("Save shipping data:", data); // Debug log
        $.ajax({
            url: url,
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (response) {
                console.log("Save shipping response:", response); // Debug log
                if (response.success) {
                    showMessage($('#shippingFormMessage')[0], response.message || 'Success!', 'success');
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
                    showMessage($('#shippingFormMessage')[0], response.message, 'danger');
                }
            },
            error: function (xhr) {
                console.error("Save shipping error:", xhr); // Debug log
                showMessage($('#shippingFormMessage')[0], 'Error occurred: ' + xhr.status, 'danger');
            }
        });
    });
    // Delete Shipping Schedule
    window.deleteShipping = function (customerCode, transCd, weekday) {
        if (confirm('Are you sure you want to delete this schedule?')) {
            console.log("Deleting shipping:", { customerCode, transCd, weekday }); // Debug log
            $.ajax({
                url: deleteShippingScheduleUrl,
                type: 'POST',
                data: { customerCode: customerCode, transCd: transCd, weekday: weekday },
                success: function (response) {
                    console.log("Delete shipping response:", response); // Debug log
                    if (response.success) {
                        loadShippingSchedules(customerCode);
                        showMessage(document.body, "Xóa shipping schedule thành công!", 'success');
                    } else {
                        alert(response.message || 'Delete failed');
                    }
                },
                error: function (xhr) {
                    console.error("Delete shipping error:", xhr); // Debug log
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
    // Set customer name in modals
    $('#manageLeadtimeModal').on('shown.bs.modal', function () {
        var customerCode = $('#currentCustomerCodeLeadtime').val();
        $('#leadtimeCustomerName').text('Customer: ' + customerCode);
    });
    $('#manageShippingModal').on('shown.bs.modal', function () {
        var customerCode = $('#currentCustomerCodeShipping').val();
        $('#shippingCustomerName').text('Customer: ' + customerCode);
    });
});