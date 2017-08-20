var langTools = ace.require('ace/ext/language_tools');
var githubTheme = ace.require('ace/theme/github');

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

var syntaxHighlighter = {
    'C': 'c_cpp',
    'C++': 'c_cpp',
    'C#': 'csharp',
    'Pascal': 'pascal',
    'Ruby': 'ruby',
    'Python': 'python',
    'JavaScript': 'javascript'
};

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
    var dom = $(e.target);
    if (dom.find('.datetime').length) {
        dom.find('.datetime').datetimepicker();
    }

    if (dom.find('#code-editor').length && !dom.find('#code-editor')[0].editor) {
        var editor = ace.edit("code-editor");
        dom.find('#code-editor')[0].editor = editor;
        editor.setTheme("ace/theme/twilight");
        editor.session.setMode('ace/mode/' + syntaxHighlighter[app.preferences.language]);
        editor.setOptions({
            enableBasicAutocompletion: true,
            enableSnippets: true
        });
    }

    if (dom.find('.code-box').length) {
        var boxes = dom.find('.code-box');
        for (var i = 0; i < boxes.length; i ++) {
            if (boxes[i].editor)
                continue;
            var id = "ace" + parseInt(Math.random() * 1000000);
            boxes[i].id = id;
            var editor = ace.edit(id);
            boxes[i].editor = editor;
            editor.setTheme("ace/theme/twilight");
            editor.session.setMode('ace/mode/' + $(boxes[i]).attr('data-mode'));
            editor.setReadOnly(true);
            editor.setAutoScrollEditorIntoView(true);
            editor.setOption("maxLines", 100);
        }
    }
});

var __split_down = false;
$(window).mousedown(function (e) {
    var dom = $(e.target);
    if (dom.hasClass('problem-edit-split-line')) {
        __split_down = true;
    }
});

$(window).mousemove(function (e) {
    if (__split_down) {
        var x = e.pageX;
        var width = $(window).width();
        var left = (x / width * 100).toFixed(3);
        var right = 100 - (x / width * 100).toFixed(3);
        $('.problem-body').css('width', left + '%');
        $('.code-editor-outer').css('width', right + '%');
        $('.code-editor-outer').css('left', left + '%');
        $('.problem-edit-split-line').css('left', 'calc(' + left + '% - 3px)');
        $('.back-to-view-mode').css('left', left + '%');
        $('.back-to-view-mode').css('margin-left', -$('.back-to-view-mode').outerWidth() - 30);
    }
});

$(window).mouseup(function (e) {
    __split_down = false;
});

$(window).click(function (e) {
    if ($('.submit-mode').length) {
        var dom = $(e.target);
        if (!dom.hasClass('submit-mode') && !dom.parents('.submit-mode').length && !dom.hasClass('trigger-submit-mode') && !dom.parents('.trigger-submit-mode').length) {
            LazyRouting.GetCurrentComponent().backToViewMode();
        }
    }
});