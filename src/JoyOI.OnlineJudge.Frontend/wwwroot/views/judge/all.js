app.title = '评测记录';
app.links = [];

component.data = function () {
    return {
        page: 1,
        pageCount: 10,
        statuses: statuses,
        languages: languages,
        selectedStatus: null,
        selectedProblem: null,
        selectedSubmittor: null,
        selectedLanguage: null,
        selectedContest: null,
        selectedTime: null,
        submittorSearchResult: []
    };
};

component.watch = {
    deep: true
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
                        role: null
                    }
                }).slice(0,5);
                qv.createView('/api/user/role', { usernames: self.submittorSearchResult.map(y => y.username).toString()}).fetch(y => {
                    for (var i = 0; i < self.submittorSearchResult.length; i++) {
                        self.submittorSearchResult[i].role = y.data[self.submittorSearchResult[i].username];
                    }
                });
            });
        }
    },
    selectSubmittor: function (username) {
        this.selectedSubmittor = username;
        $('.submit-filter').removeClass('active');
    }
};