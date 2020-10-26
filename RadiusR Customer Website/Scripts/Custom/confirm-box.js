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