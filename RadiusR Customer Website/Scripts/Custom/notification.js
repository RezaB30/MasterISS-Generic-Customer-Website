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
function CloseInformationMessage() {
    $('#information-message').remove();
    var d = new Date();
    d.setTime(d.getTime() + (365 * 24 * 60 * 60 * 1000));
    var expires = d.toUTCString();
    document.cookie = "info_cookie=oim_info_msg; expires=" + expires + "; path=/";
}
function CheckInfoCookie() {
    var x = document.cookie;
    var cookies = x.split(';');
    var hasInfoMsg = false;
    for (var i = 0; i < cookies.length; i++) {
        if (cookies[i].trim() == "info_cookie=oim_info_msg") {
            hasInfoMsg = true;
        }
    }
    if (hasInfoMsg == true) {
        $('#information-message').remove();
    } else {
        $('#information-message').removeAttr("style");
    }
}
CheckInfoCookie();
$(document).ready(function () {
    ResizeInfoBox();
});
$(window).on("resize", function () {
    ResizeInfoBox();
})
function ResizeInfoBox() {
    var px = $('li.user-management-icon').width();
    $("#information-message").css("right", ((px / 16)) + "em");
}