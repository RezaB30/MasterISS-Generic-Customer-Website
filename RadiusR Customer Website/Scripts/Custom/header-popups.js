function SetupHeaderPopups() {
    $('.header-right-item').click(function (e) {
        e.stopPropagation();
        var current = $(this);
        ClosePopups();
        current.find('.header-popup').fadeIn(200);
    });
}