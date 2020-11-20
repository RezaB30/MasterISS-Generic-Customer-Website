SetupHeaderPopups();
SetupConfirmBox();
SetupDisplayBox();

function ClosePopups() {
    $('.header-popup').hide();
}

$(document).click(function () {
    ClosePopups();
});