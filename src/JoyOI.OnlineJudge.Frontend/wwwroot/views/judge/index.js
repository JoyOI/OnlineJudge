app.title = '评测结果'
app.links = [{ text: '评测', to: '/judge' }];

component.data = function () {
    return {
        control: {
            statuses: statuses,
            languages: languages,
            highlighters: syntaxHighlighter
        },
        id: '8c1185b7-4a35-499b-9344-3cd44bc732e4',
        language: null,
        hint: 'Main.cpp: In function \'int main()\': Main.cpp:58:17: error: \'putchar\' was not declared in this scope putchar(\'\\n\'); ^',
        code: '#include&lt;iostream&gt;\r\nusing namespace std;\r\nint main() \r\n {\r\n',
        substatuses: [{ status: 'Pending', time: 0, memory: 0, hint: '' }, { status: 'Pending', time: 0, memory: 0, hint: '' }],
        time: new Date()
    };
};

component.created = function () {
    this.id = router.history.current.params.id;
    var self = this;
    qv.createView('/api/judge/' + this.id)
        .fetch(x => {
            this.language = x.data.language;
            this.code = x.data.code;
        });
};

component.computed = {
    status: function () {
        if (this.substatuses.length) {
            var mapping = this.control.statuses.map(y => y.display);
            var indexes = this.substatuses.map(x => mapping.indexOf(x.status));
            var maxIndex = -1;
            for (var i = 0; i < indexes.length; i++) {
                maxIndex = Math.max(maxIndex, indexes[i]);
            }
            if (maxIndex >= 0 && maxIndex < this.control.statuses.length)
                return this.control.statuses[maxIndex].display;
            else
                return 'Unkown Result';
        } else {
            return 'Unknown Result';
        }
    },
    totalTime: function () {
        var time = 0;
        for (var i = 0; i < this.substatuses.length; i++) {
            time += this.substatuses[i].time;
        }
        return time;
    },
    peakMemory: function () {
        var mem = 0;
        for (var i = 0; i < this.substatuses.length; i++) {
            mem = Math.max(this.substatuses[i].memory, mem);
        }
        return mem;
    }
};

component.watch = {
    language: function (val) {
        var mode = this.control.highlighters[val];
        var dom = $('.code-box-outer .code-box');
        if (!dom.length || !dom[0].editor) return;
        dom[0].editor.session.setMode('ace/mode/' + mode);
    }
};

component.methods = {
    toggleStatusHint: function (index) {
        var tr = $('.judge-panel-table tr');
        if (2 * index + 1 < tr.length) {
            $(tr[2 * index + 1]).toggle();
        }
    }
};
