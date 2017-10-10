app.title = '评测记录';
app.links = [];

component.data = function () {
    return {
        paging: {
            current: 1,
            count: 1
        },
        statuses: statuses,
        result: [],
        languages: languages,
        selectedStatus: null,
        selectedProblem: null,
        selectedSubmittor: null,
        selectedLanguage: null,
        selectedContest: null,
        selectedTime: null,
        page: null,
        submittorSearchResult: [],
        problemSearchResult: []
    };
};

component.watch = {
    selectedStatus: function () {
        this.loadStatuses();
    },
    selectedProblem: function () {
        this.loadStatuses();
    },
    selectedSubmittor: function () {
        this.loadStatuses();
    },
    selectedLanguage: function () {
        this.loadStatuses();
    },
    selectedContest: function () {
        this.loadStatuses();
    },
    selectedTime: function () {
        this.loadStatuses();
    },
    page: function () {
        this.loadStatuses();
    }
};

component.created = function () {
    this.loadStatuses();
};

component.methods = {
    filterStatus: function (status) {
        $('[data-value="' + this.selectedStatus + '"]').removeClass('active');
        if (this.selectedStatus !== null && this.selectedStatus === status) {
            this.selectedStatus = null;
        } else {
            this.selectedStatus = status;
            $('[data-value="' + this.selectedStatus + '"]').addClass('active');
        }
        setTimeout(function () {
            $('.filter-outer').removeClass('active');
        }, 300);
    },
    filterLanguage: function (language) {
        $('[data-value="' + this.selectedLanguage + '"]').removeClass('active');
        if (this.selectedLanguage && this.selectedLanguage == language) {
            this.selectedLanguage = null;
        } else {
            this.selectedLanguage = language;
            $('[data-value="' + this.selectedLanguage + '"]').addClass('active');
        }
        setTimeout(function () {
            $('.filter-outer').removeClass('active');
        }, 300);
    },
    filterTime: function (start, end) {
        if (!start && !end) {
            this.selectedTime = null;
        } else {
            this.selectedTime = { start: start, end: end };
        }
    },
    toPage: function (p) {
        this.paging.current = p;
        this.page = p;
    },
    searchSubmittor: function () {
        var val = $('.textbox-search-submittor').val();
        var self = this;
        if (!val) {
            self.submittorSearchResult = [];
            this.selectedSubmittor = null;
        } else {
            qv.createView('/api/user/all', { username: val }).fetch(x => {
                self.submittorSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        username: y.userName,
                        avatarUrl: y.avatarUrl,
                        roleClass: null
                    }
                }).slice(0,5);
                qv.createView('/api/user/role', { usernames: self.submittorSearchResult.map(y => y.username).toString()}).fetch(y => {
                    for (var i = 0; i < self.submittorSearchResult.length; i++) {
                        self.submittorSearchResult[i].roleClass = ConvertUserRoleToCss(y.data[self.submittorSearchResult[i].username].role);
                    }
                });
            });
        }
    },
    selectSubmittor: function (username) {
        this.selectedSubmittor = username;
        $('.submit-filter').removeClass('active');
    },
    searchProblem: function () {
        var val = $('.textbox-search-problem').val();
        var self = this;
        if (!val) {
            self.problemSearchResult = [];
            self.selectedProblem = null;
        } else {
            qv.createView('/api/problem/all', { title: val }).fetch(x => {
                self.problemSearchResult = x.data.result.map(y => {
                    return {
                        id: y.id,
                        title: y.title
                    };
                });
            });
        }
    },
    selectProblem: function (id, title) {
        this.selectedProblem = { id: id, title: title };
        $('.problem-filter').removeClass('active');
    },
    selectTimeRange: function () {
        this.selectedTime = {
            begin: $('.time-range-begin').val(),
            end: $('.time-range-end').val()
        };
        $('.time-filter').removeClass('active');
    },
    clearTimeRange: function () {
        this.selectedTime = null;
        $('.time-range-begin').val('')
        $('.time-range-end').val('')
        $('.time-filter').removeClass('active');
    },
    loadStatuses: function () {
        var self = this;
        qv.createView('/api/judge/all', {
            problemid: self.selectedProblem ? self.selectedProblem.id : null,
            status: self.selectedStatus,
            userId: self.selectedSubmittor,
            language: self.selectedLanguage,
            begin: self.selectedTime ? self.selectedTime.begin : null,
            end: self.selectedTime ? self.selectedTime.end : null,
            page: self.page
        }).fetch(x => {
            self.paging.count = x.data.count;
            self.paging.current = x.data.current;
            self.result = x.data.result.map(y =>
            {
                y.result = formatJudgeResult(y.result);
                y.resultClass = ConvertJudgeResultToCss(y.result);
                y.userName = y.userId.substr(0, 8);
                y.roleClass = null;
                return y;
            });

            if (self.result.length) {
                qv.createView('/api/problem/title', { problemids: x.data.result.map(y => y.problemId).toString() })
                    .fetch(y => {
                        for (var i = 0; i < self.result.length; i++) {
                            self.result[i].problemTitle = y.data[self.result[i].problemId].title;
                        }
                    });
                qv.createView('/api/user/role', { userids: x.data.result.map(y => y.userId).toString() })
                    .fetch(y => {
                        for (var i = 0; i < self.result.length; i++) {
                            self.result[i].userName = y.data[self.result[i].userId].username;
                            self.result[i].userRole = y.data[self.result[i].userId].role;
                            self.result[i].roleClass = ConvertUserRoleToCss(self.result[i].userRole);
                        }
                    })
            }

            // TODO: Fetch problem title & username
        });
    }
};