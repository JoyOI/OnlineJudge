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