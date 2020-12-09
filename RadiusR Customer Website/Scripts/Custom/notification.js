function GetSupportStatus(url, culture) {
    $.ajax({
        url: url,
        method: 'POST',
        complete: function (data, status) {
            if (status == "success") {
                var count = data.responseJSON.openRequestCount;
                if (count != 0) {
                    //$('.notification-badge').text(count);
                    //$('.notification-badge').removeAttr("style");
                    var requestIds = data.responseJSON.requestIds;
                    for (var i = 0; i < requestIds.length; i++) {
                        $('#' + requestIds[i]).addClass("notification-row");
                        $('body').prepend($('#new-notification').html());
                        $('#new-notification').remove();
                        $('#notif-content').find("a").first().attr("data-url", "/" + culture + "/Support/SupportDetails/" + requestIds[i]);
                    }
                }
            }
        }
    })
}
function GoToUrl(url) {
    CloseInformationBox("notif-content", CookieTimeType.HOUR, 1, "notification_cookie");
    window.location.href = url;
}
function CloseNotification() {
    CloseInformationBox("notif-content", CookieTimeType.HOUR, 1, "notification_cookie");
    clearInterval(_notifClosingInterval);
}
function CloseInformationMessage() {
    CloseInformationBox("information-message", CookieTimeType.DAY, 365, "info_cookie");
    ResizeInfoBox();
}
function CheckInfoCookie() {
    var x = document.cookie;
    var cookies = x.split(';');
    var hasInfoMsg = false;
    var hasNotification = false;

    for (var i = 0; i < cookies.length; i++) {
        if (cookies[i].trim() == "info_cookie=" + $('#info-checker').attr("data-value") + "") {
            hasInfoMsg = true;
        }
        if (cookies[i].trim() == "notification_cookie=" + $('#info-checker').attr("data-value") + "") {
            hasNotification = true;
        }
    }
    if (hasInfoMsg == true) {
        $('#information-message').remove();
    } else {
        $('#information-message').removeAttr("style");
    }
    if (hasNotification == true) {
        $('#notif-content').remove();
    } else {
        //NotificationClosing();
        //_notifClosingInterval = setInterval(NotificationClosing, 200);
    }
}
var _notifClosingInterval;
$(document).ready(function () {
    CheckInfoCookie();
    ResizeInfoBox();
});
$(window).on("resize", function () {
    ResizeInfoBox();
})
function ResizeInfoBox() {
    var px = $('li.user-management-icon').width();
    $("#information-message").css("right", ((px / 16)) + "em");
    $("#notif-content").css("right", ((px / 16)) + "em");
    if ($('#information-message').hasClass("tooltiptext")) {
        if (!$("#notif-content").hasClass("notification-box-top")) {
            $("#notif-content").addClass("notification-box-top");
        }
    } else {
        $("#notif-content").removeClass("notification-box-top");
    }
}
//function NotificationClosing() {
//    var _width = $('#notif-content').find("div").first().css("width");
//    if (_width) {
//        if (_width.replace("px", "") <= 0) {
//            CloseNotification();
//        }
//        $('#notif-content').find("div").first().css("width", (_width.replace("px", "") - 1) + "px");
//    }
//}