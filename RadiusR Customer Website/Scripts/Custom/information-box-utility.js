const CookieTimeType = {
    HOUR: 'hour',
    DAY: 'day'
}
/**
 *
 * @param {string} _id selector id
 * @param {CookieTimeType} _CookieTimeType enum type
 * @param {number} _CookieTime set time day or hour 
 * @param {string} _CookieName set cookie name
 */
function CloseInformationBox(_id, _CookieTimeType, _CookieTime, _CookieName) {
    var current_id = $('#' + _id);// document.getElementById(_id);
    current_id.remove();
    var d = new Date();
    if (CookieTimeType == CookieTimeType.DAY) {
        d.setTime(d.getTime() + (_CookieTime * 24 * 60 * 60 * 1000));
    } else {
        d.setTime(d.getTime() + (_CookieTime * 60 * 60 * 1000));
    }
    var expires = d.toUTCString();
    var cookieKey = $('#info-checker').attr("data-value");
    document.cookie = _CookieName + "=" + cookieKey + "; expires=" + expires + "; path=/";
}