var lightMode = "rgb(243, 243, 243)";
var darkMode = "rgb(32, 32, 32)";

$(document).ready(function () {
    $("body").css("font-family", "'Segoe UI', Tahoma, Geneva");
    var isDarkMode = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

    if (isDarkMode) {
        $("html").attr('data-theme', 'dark');
        $("body").css("color", lightMode);
        $("input[name='button']").css({
            "background-color": darkMode,
            "color": lightMode
        });
        $("h2").hover(
            function () {
                $(this).css("color", "orange");
            },
            function () {
                $(this).css("color", "cyan");
            }
        );
    } else {
        $("html").attr('data-theme', 'light');
        $("body").css("color", darkMode);
        $("input[name='button']").css({
            "background-color": lightMode,
            "color": darkMode
        });
        $("h2").hover(
            function () {
                $(this).css("color", "blue");
            },
            function () {
                $(this).css("color", "red");
            }
        );
    }

    $("input[name='button']").click(function () {
        window.location.assign("ExtensionSettingsMainPage.html");
    });
});