var lightMode = "rgb(243, 243, 243)";
var darkMode = "rgb(32, 32, 32)";

$(document).ready(function () {
    $("body").css("font-family", "'Segoe UI', Tahoma, Geneva");
    var isDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (isDarkMode) {
        $("html").attr('data-theme', 'dark');
        $("body").css("color", "white"); // Set starting text color to white in dark mode
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
        $("body").css("color", "black"); // Set starting text color to black in light mode
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