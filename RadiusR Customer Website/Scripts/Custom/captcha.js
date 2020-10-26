$(document).ready(function () {
    $('input.captcha-reload-button').click(function () {
        var currentButton = $(this);
        var captchaImage = currentButton.closest('div.captcha').find('div.captcha-image-wrapper').find('img');
        var captcha_url = captchaImage.attr('src');
        var questionMarkIndex = captcha_url.indexOf('?');
        if (questionMarkIndex > 0)
            captcha_url = captcha_url.substr(0, captcha_url.indexOf('?', 0));
        captchaImage.attr('src', captcha_url + '?id=' + Math.random());
    });
});