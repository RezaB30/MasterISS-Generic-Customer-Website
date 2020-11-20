var confirmForm;
function SetupConfirmBox() {
    var box = $('div.confirm-box');
    var yes = box.find('input.jungle');
    var no = box.find('input.red');
    no.click(function () {
        box.removeClass('open');
    });

    yes.click(function () {
        box.removeClass('open');
        confirmForm.unbind('submit');
        confirmForm.submit();
    });
}

function ShowConfirm(form) {
    var box = $('div.confirm-box');
    var formIdInput = box.find('input[type="hidden"]');
    confirmForm = form;
    box.addClass('open');
}
function SetupDisplayBox() {
    var box = $('div.display-box');
    var no = box.find('input.red');
    no.click(function () {
        box.removeClass('open');
    });
}
function ShowDisplayMessage() {
    var box = $('div.display-box');
    box.addClass('open');
}

$(document).ready(function () {
    var msg = $('#display-valid-message').html();
    if (msg.trim()) {
        ShowDisplayMessage();
    }
})