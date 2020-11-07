function GetSupportStatus(url) {
    $.ajax({
        url: url,
        method: 'POST',
        complete: function (data, status) {
            if (status == "success") {
                var count = data.responseJSON.openRequestCount;
                if (count != 0) {
                    $('.notification-badge').text(count);
                    $('.notification-badge').removeAttr("style");
                    var requestIds = data.responseJSON.requestIds;
                    for (var i = 0; i < requestIds.length; i++) {
                        $('#' + requestIds[i]).addClass("notification-row");
                    }
                }
            }
        }
    })
}