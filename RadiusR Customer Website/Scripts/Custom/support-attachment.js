function GetSupportAttachments(url, supportId, downloadUrl) {
    $.ajax({
        url: url,
        method: 'POST',
        data: { supportId: supportId },
        complete: function (data, status) {
            if (status == "success") {
                //FileName , ServerName,StageId
                var files = data.responseJSON;
                for (var i = 0; i < files.length; i++) {
                    var html = '<a style="color:red; display:inline-block;" href="' + downloadUrl + '?supportId=' + supportId + '&fileName=' + files[i].ServerName + '">' +
                        '<img style="vertical-align:middle;" src="/Content/Images/Icons/attachment.svg"></img>' + files[i].FileName + '</a>';

                    $('.support-file-content').each(function () {
                        var stage = $(this).attr("data-stage");
                        if (stage == files[i].StageId) {
                            $(this).append(html);
                        }
                    })
                }
            }
        }
    });
}
$('#support-attachment-id').click(function () {
    $('.upload-attachments').trigger("click");
});
$('.upload-attachments').change(function () {
    var items = "";
    //for (var i = 0; i < $(this).files.length; i++) {
    //    items += '<li>' + $(this).files[i].name + '</li>';
    //}
    var inp = document.getElementsByClassName('upload-attachments')[0];
    for (var i = 0; i < inp.files.length; ++i) {
        var name = inp.files.item(i).name;
        items += '<li class="list-item">' + name + '</li>';
    }
    $('.upload-attachment-list').html(items);
});