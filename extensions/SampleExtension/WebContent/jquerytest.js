var lightMode = "rgb(243, 243, 243)";
var darkMode = "rgb(32, 32, 32)";

$(document).ready(function () {
    $("body").css("font-family", "'Segoe UI', Tahoma, Geneva");
    var isDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (isDarkMode) {
        $("html").attr('data-theme', 'dark');
        $("h1").hover(
            function () {
                $(this).css("color", "orange"); // High contrast with darkMode
            },
            function () {
                $(this).css("color", "cyan"); // High contrast with darkMode
            }
        );
    } else {
        $("html").attr('data-theme', 'light');
        $("h1").hover(
            function () {
                $(this).css("color", "blue"); // High contrast with lightMode
            },
            function () {
                $(this).css("color", "red"); // High contrast with lightMode
            }
        );
    }

    $("input[name='button']").click(function () {
        window.location.assign("ExtensionSettingsPage.html");
    });
});