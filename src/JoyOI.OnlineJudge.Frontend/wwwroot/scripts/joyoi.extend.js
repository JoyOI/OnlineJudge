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
    if ($(e.target).hasClass('tag-item') || $(e.target).parents().hasClass('tag-item')) {
        var val = $(e.target).attr('data-value');
        $('[data-parent="' + val + '"]').addClass('active');
        $('[data-parent="' + val + '"]').outerWidth($('.col-md-3').outerWidth());
        $('[data-parent="' + val + '"]').css('margin-top', -$('[data-value="' + val + '"]').outerHeight());
        $('[data-parent="' + val + '"]').css('margin-left', -$('.col-md-3').outerWidth() - 5);
    }

    if (!$(e.target).parents().hasClass('tag-item') && !$(e.target).hasClass('tag-item') && !$(e.target).parents().hasClass('tag-extend-outer') && !$(e.target).hasClass('tag-extend-outer'))
    {
        $('[data-parent]').removeClass('active');
    }
});

var statuses = [
    { display: 'Accepted', color: 'green', value: 0 },
    { display: 'Presentation Error', color: 'red', value: 1 },
    { display: 'Wrong Answer', color: 'red', value: 2 },
    { display: 'Output Exceeded', color: 'red', value: 3 },
    { display: 'Time Exceeded', color: 'red', value: 4 },
    { display: 'Memory Exceeded', color: 'red', value: 5 },
    { display: 'Runtime Error', color: 'red', value: 6 },
    { display: 'Compile Error', color: 'orange', value: 7 },
    { display: 'System Error', color: 'orange', value: 8 },
    { display: 'Hacked', color: 'red', value: 9 },
    { display: 'Running', color: 'blue', value: 10 },
    { display: 'Pending', color: 'blue', value: 11 },
    { display: 'Hidden', color: 'purple', value: 12 }
];

var languages = [
    'C',
    'C++',
    'Pascal',
    'C#',
    'Python',
    'Ruby',
    'JavaScript'
];

$(window).click(function (e) {
    var dom = $(e.target);
    if (!dom.parents('#app').length) {
        return;
    }
    if (dom.parents('.filter-button').length)
        dom = dom.parents('.filter-button');
    else if (!dom.hasClass('filter-button'))
        return;
    var box = dom.next('.filter-outer');
    if (!box.hasClass('filter-outer'))
        return;
    if (box.hasClass('active'))
    {
        box.removeClass('active');
    }
    else
    {
        $('.filter-outer.active').removeClass('active');
        box.addClass('active');

        if ($(window).width() >= 768) {
            if (box.hasClass('submit-filter') || box.hasClass('language-filter')) {
                box.css('left', box.parents('th').offset().left - box.parents('tr').offset().left + 15 - box.outerWidth() + box.parents('th').outerWidth());
            } else {
                box.css('left', box.parents('th').offset().left - box.parents('tr').offset().left + 15);
            }

            if (box.hasClass('time-filter')) {
                box.outerWidth(box.parents('th').outerWidth());
            }

            if (box.hasClass('problem-filter')) {
                var width = box.parents('th').outerWidth();
                if (width < 250)
                    width = 250;
                box.outerWidth(width);
            }
        }
    }
});

$(window).click(function (e) {
    var dom = $(e.target);

    if (!dom.parents('#app').length) {
        return;
    }

    if (!dom.hasClass('filter-outer') && !dom.parents('.filter-outer').length && !dom.hasClass('filter-button') && !dom.parents('.filter-button').length) {
        $('.filter-outer').removeClass('active');
    }
});

$(window).click(function (e) {
    var dom = $(e.target);
    if (dom.parents('#navbar').length) {
        $('.collapse.in').removeClass('in');
    }
});

$(document).bind('DOMNodeInserted', function (e) {
    var dom = $(e.target).find('.datetime')
    if (dom.length) {
        dom.datetimepicker();
    }
});