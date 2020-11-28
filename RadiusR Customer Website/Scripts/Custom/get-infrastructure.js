var CurrentInput = "";
function GetValues(requestId, responseId, url) {
    // focus input
    if (!CurrentInput) {
        CurrentInput = responseId;
    }
    //
    var selectVal = "";
    $('' + requestId + 'List > option').each(function () {
        if ($(requestId).val() == $(this).val()) {
            selectVal = $(this).attr('data-value');
        }
    })
    var ID = selectVal; /*$(requestId).val()*/;
    var token = $("[name='__RequestVerificationToken']").val();
    ClearList(responseId);
    $(responseId).trigger('change');
    if (ID == '') {

    }
    else {
        DisableSelectList();
        $.ajax({
            dataType: 'json',
            method: 'POST',
            url: url,
            data: { code: ID, __RequestVerificationToken: token },
            complete: function (data, status) {
                if (status == "success") {
                    var results = data.responseJSON;
                    for (var i = 0; i < results.length; i++) {
                        $(responseId + 'List').append('<option data-value="' + results[i].value + '" value="' + results[i].text + '"></option>');
                    }
                    EnableSelectList();
                    if (results.length === 1 && results[0].text === "MERKEZ") {
                        $('#regionId').val(results[0].text);
                        GetValues('#regionId', '#neighborhoodId', GetNeighborhood);
                    }
                    else {
                        EnableSelectList();
                    }
                }
                else {
                    EnableSelectList();
                }
            }
        });
    }
}
function DisableSelectList() {
    $('div').each(function () {
        $(this).find("select").attr("disabled", "disabled");
    })
}
function EnableSelectList() {
    $('div').each(function () {
        $(this).find("select").removeAttr("disabled");
    })
}
function ClearList(responseId) {
    $(responseId).val("");
    var container = $(responseId + "List");
    container.html("");
}
$(document).ajaxStart(function () { // load gif function
    $('#overlay').show();
    $('input').attr("disabled", "disabled");
}).ajaxStop(function () {
    $('#overlay').hide();
    $('input').removeAttr("disabled");
    $(CurrentInput).focus();
    CurrentInput = "";
});

$(document).ready(function () {
    $('datalist').prev('input').val("");
    $('datalist').prev('input').attr("lang", "tr");
});


function PostAvailabilityResult(bbk, token) {
    $.ajax({
        url: AvailabilityResultUrl,
        data: { BBK: bbk, __RequestVerificationToken: token },
        method: 'POST',
        complete: function (data, status) {
            if (status == "success") {
                var response = data.responseText;
                $('body').html(response);
            }
        }
    })
}
function CheckCaptcha() {
    $.ajax({
        url: CaptchaCheckUrl,
        data: { Captcha: $('#Captcha').val() },
        method: 'POST',
        complete: function (data, status) {
            if (status == "success") {
                var response = data.responseJSON;
                if (response.IsSuccess) {
                    $('#captcha_content').find("div").remove();
                    var bbk = $('#BBK').val();
                    if (bbk != 0) {
                        var token = $("[name='__RequestVerificationToken']").val();
                        PostAvailabilityResult(bbk, token);
                    } else {
                        $('#apartmentId').addClass("invalidInput");
                        $('#apartmentId').focus();
                    }
                }
                else {
                    $('#captcha_content').html(response.captchaImg);
                }
            }
        }
    })
}