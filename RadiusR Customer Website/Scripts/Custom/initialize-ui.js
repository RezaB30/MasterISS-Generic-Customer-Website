SetupHeaderPopups();
SetupConfirmBox();


function ClosePopups() {
    $('.header-popup').hide();
}

$(document).click(function () {
    ClosePopups();
});