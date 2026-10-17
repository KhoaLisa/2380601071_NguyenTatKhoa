(function ($) {
    'use strict';
    $('.mobile-menu').on('click', function () {
        var opened = $('body').toggleClass('nav-open').hasClass('nav-open');
        $(this).attr('aria-expanded', String(opened));
    });
    $(document).on('keydown', function (event) {
        if (event.key === 'Escape') { $('body').removeClass('nav-open'); $('.mobile-menu').attr('aria-expanded', 'false'); }
    });
    $('form[data-busy]').on('submit', function () {
        if (this.checkValidity()) {
            if ($(this).hasClass('is-busy')) return false;
            $(this).addClass('is-busy').attr('aria-busy', 'true');
        }
    });
    $('#word-file').on('change', function () {
        var file = this.files && this.files[0];
        this.setCustomValidity('');
        if (file && (!/\.docx$/i.test(file.name) || file.size > 25 * 1024 * 1024)) {
            this.setCustomValidity('Chọn tệp .docx không vượt quá 25 MB.');
            this.reportValidity();
        }
        $('.upload-name').text(file ? file.name : 'Chưa chọn tệp');
    });
    $('.upload-zone').on('dragover', function (event) { event.preventDefault(); $(this).addClass('dragging'); })
        .on('dragleave drop', function (event) { event.preventDefault(); $(this).removeClass('dragging'); })
        .on('drop', function (event) {
            var files = event.originalEvent.dataTransfer.files;
            if (files.length === 1) { $('#word-file')[0].files = files; $('#word-file').trigger('change'); }
        });
})(window.jQuery);
