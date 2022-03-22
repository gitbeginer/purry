
let btn = document.getElementById("close_This_Web");

btn.addEventListener("click", e => {
    setCookie("goBackNow", 'true', 3000);
});

function ifGoBack() {
    if (getCookie("goBackNow") != 'true') return;

    if (!document.referrer.startsWith(window.location.origin)) {
        setCookie("goBackNow", 'false', 1);
    }
    history.back();
    if (document.referrer != '') setTimeout(window.close, 100);
}

setInterval(ifGoBack, 100);


function setCookie(cname, cvalue, exMs) {
    const d = new Date();
    d.setTime(d.getTime() + exMs);
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function getCookie(cname) {
    const name = cname + "=";
    const str = document.cookie;
    let stIdx = str.indexOf(name);
    if (stIdx == -1) return "";
    stIdx = stIdx + cname.length + 1;
    let ltIdx = str.indexOf(";", stIdx);
    if (ltIdx == -1) ltIdx = Infinity;

    return str.substring(stIdx, ltIdx).trim();
}
