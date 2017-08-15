function changeAvatarSource()
{
    $('.avatar-section').hide();
    var src = $('#lstAvatarResources').val();
    if (src === 'LocalStorage') {
        $('#uploadAvatar').show();
    } else if (src === 'WeChatPolling') {
        $('#wechatAvatar').show();
    } else {
        $('#gravatar').show();
    }
}

$(document).ready(function () {
    changeAvatarSource();
    $('#lstAvatarResources').change(function () {
        changeAvatarSource();
    });
});

function accountChangeApplicationAuthorization(openId, disabled)
{
    if (confirm(SR("Are you sure you want to forbid this application to access your account?")))
    {
        $('#hidOpenId').val(openId);
        $('#hidDisabled').val(disabled)
        $('#frmAuthorization').submit();
    }
}

function removeApplicationManager(uid)
{
    if (confirm(SR("Are you sure you want to remove this manager?")))
    {
        $('#hidUserId').val(uid);
        $('#frmApplicationManagerRemove').submit();
    }
}

$(window).on('mousemove', function (e) {
    console.log(e);
    if ($(e.target).hasClass('tag-item') || $(e.target).parents().hasClass('tag-item')) {
        var val = $(e.target).attr('data-value');
        $('[data-parent="' + val + '"]').addClass('active');
        $('[data-parent="' + val + '"]').outerWidth($('.grid_3').outerWidth());
        $('[data-parent="' + val + '"]').css('top', $('[data-value="' + val + '"]').offset().top);
        $('[data-parent="' + val + '"]').css('margin-left', -$('.grid_3').outerWidth()-5);
    }

    if (!$(e.target).parents().hasClass('tag-item') && !$(e.target).hasClass('tag-item') && !$(e.target).parents().hasClass('tag-extend-outer') && !$(e.target).hasClass('tag-extend-outer'))
    {
        $('[data-parent]').removeClass('active');
    }
});