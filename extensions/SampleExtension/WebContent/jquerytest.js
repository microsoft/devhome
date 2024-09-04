$(document).ready(function () {
    $("h1").hover(
        function () {
            $(this).css(
                "color",
                "purple"
            );
        },
        function () {
            $(this).css(
                "color",
                "aliceblue"
            );
        }
    );
});

function navigateToPage(webPage) {
    window.location.assign(webPage);
};