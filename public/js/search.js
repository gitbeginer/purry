$(window).ready(function () {
    function escapeRegExp(string) {
        return string.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&');
    }
    function ch2pattern(ch) {
        const offset = 44032; /* '가'의 코드 */
        // 한국어 음절
        if (/[가-힣]/.test(ch)) {
            const chCode = ch.charCodeAt(0) - offset;
            // 종성이 있으면 문자 그대로를 찾는다.
            if (chCode % 28 > 0) {
                return ch;
            }
            const begin = Math.floor(chCode / 28) * 28 + offset;
            const end = begin + 27;
            return `[\\u${begin.toString(16)}-\\u${end.toString(16)}]`;
        }
        // 한글 자음
        if (/[ㄱ-ㅎ]/.test(ch)) {
            const con2syl = {
                'ㄱ': '가'.charCodeAt(0),
                'ㄲ': '까'.charCodeAt(0),
                'ㄴ': '나'.charCodeAt(0),
                'ㄷ': '다'.charCodeAt(0),
                'ㄸ': '따'.charCodeAt(0),
                'ㄹ': '라'.charCodeAt(0),
                'ㅁ': '마'.charCodeAt(0),
                'ㅂ': '바'.charCodeAt(0),
                'ㅃ': '빠'.charCodeAt(0),
                'ㅅ': '사'.charCodeAt(0),
            };
            const begin = con2syl[ch] || ((ch.charCodeAt(0) - 12613 /* 'ㅅ'의 코드 */) * 588 + con2syl['ㅅ']);
            const end = begin + 587;
            return `[${ch}\\u${begin.toString(16)}-\\u${end.toString(16)}]`;
        }
        // 그 외엔 그대로 내보냄
        return escapeRegExp(ch);
    }

    let sedata = { nicks: [], titles: [] };
    $.getJSON("/sedata.json", function (data) {
        sedata = data;
        loadAutoComplate({ e: null });
    });
    const searchInput = $('#se_writer');
    searchInput.attr("placeholder", searchInput.attr("name") == "se" ? "글쓴이" : "제목");
    $("#se_label").on("click", function (e) {
        searchInput.val('');
        if (searchInput.attr("name") == 'se') {
            searchInput.attr("placeholder", "제목");
            searchInput.attr("name", "ti");
        } else {
            searchInput.attr("placeholder", "글쓴이");
            searchInput.attr("name", "se");
        }
        loadLocaldData();
    });

    $("ul.list li").mouseover(function (e) {
        $("ul.list li").removeClass("selected")
        $(e.target).addClass("selected");
    });

    $("ul.list li").click(function (e) {
        searchInput.val(e.target.innerText);
        $("form").submit();
    });
    $("ul.list").mousedown(function (e) {
        e.preventDefault();
    });


    const params = new Proxy(new URLSearchParams(window.location.search), {
        get: function (searchParams, prop) { return searchParams.get(prop) },
    });
    const local_nick = paramLoad("se");
    const local_title = paramLoad("ti");

    function paramLoad(name) {
        const list = JSON.parse(localStorage.getItem(name) || "[]");

        name = (name || "").trim();
        const val = (params[name] || "").trim();
        if (val.length == 0) return list;
        const fidx = list.findIndex(function (ele) { return val == ele; });
        if (fidx != -1) list.splice(fidx, 1);
        list.unshift(val);
        localStorage.setItem(name, JSON.stringify(list));
        return list;
    }


    function loadLocaldData() {
        const fList = searchInput.attr("name") == 'se' ? local_nick : local_title;
        const listTag = $("ul.list li");
        for (let i = 1; i < listTag.length; i++) {
            if (fList.length < i) {
                $(listTag[i]).addClass("none");
            } else {
                listTag[i].innerHTML = fList[i - 1];
                $(listTag[i]).removeClass("none");
            }
        }
    }

    $(document).keydown(function (event) {
        if (searchInput.is(':focus') == false && event.key == "/") {
            event.preventDefault();
            searchInput.focus();
            var strLength = searchInput.val().length * 2;
            searchInput[0].setSelectionRange(strLength, strLength);
        }
    });


    searchInput.on("focus", function (e) {
        $("ul.list").css("display", "block");
    });
    searchInput.on("focusout", function (e) {
        $("ul.list").css("display", "none");
    });
    searchInput.on("propertychange change keyup paste input", loadAutoComplate);
    let se_oldval = null;

    function loadAutoComplate(e) {
        //console.log(e.keyCode);
        const val = searchInput.val().trim();
        if (e.keyCode == 38 || e.keyCode == 40) { //up down
            if ($("ul.list").css("display") == 'none') {
                $("ul.list").css("display", "block");
            } else {
                const li = $("ul.list li").not(".none");
                const sel = $("ul.list li.selected");
                $(sel).removeClass("selected");
                let idx = $("ul.list li").index(sel);
                idx += e.keyCode == 40 ? 1 : -1;
                idx = (li.length + idx) % li.length;
                const nextSel = $("ul.list li")[idx];
                $(nextSel).addClass("selected");
                searchInput.val(nextSel.innerText);
            }
        } else if (e.keyCode == 37 || e.keyCode == 39) {
            if (val.length > 0) return;
            $("#se_label").trigger('click');
        } else if (e.keyCode == 27) { //esc
            searchInput.val($("#li0").text());
            $("ul.list").css("display", "none");
        } else if (se_oldval != val) {

            $("#li0").text(val);
            if (val.length > 0) {

                $("ul.list li").removeClass("selected");
                $("#li0").addClass("selected");
                if (searchInput.is(':focus')) $("ul.list").css("display", "block");

                const pattern = val.split('').map(function (ch) { return "(" + ch2pattern(ch) + ")"; }).join('.*?');
                const reg = new RegExp(pattern, 'i');
                const keylist = searchInput.attr('name') == "se" ? sedata.nicks : sedata.titles;
                let fList = keylist.filter(function (v) { return reg.test(v); });
                fList = fList.map(function (item) {
                    let totalDistance = 0;

                    const str = item.replace(reg, function (match, ...groups) {
                        const letters = groups.slice(0, val.length);

                        const highlighted = [];
                        let lastIndex = 0;
                        for (let i = 0; i < letters.length; i++) {
                            const idx = match.indexOf(letters[i], lastIndex);
                            highlighted.push(match.substring(lastIndex, idx));
                            highlighted.push(`<mark>${letters[i]}</mark>`);

                            if (/^[A-Z]$/i.test(letters[i]) == false) {
                                totalDistance += Math.abs(letters[i].charCodeAt(0) - val[i].charCodeAt(0)) / 100000;
                            }

                            if (lastIndex > 0) totalDistance += idx - lastIndex;
                            else totalDistance += groups[val.length] / 1000000;
                            lastIndex = idx + 1;
                        }
                        return highlighted.join('');
                    });
                    return [str, totalDistance];
                });
                fList.sort(function (a, b) { return a[1] - b[1]; });

                const listTag = $("ul.list li");
                for (let i = 1; i < listTag.length; i++) {
                    if (fList.length < i) {
                        $(listTag[i]).addClass("none");
                    } else {
                        listTag[i].innerHTML = fList[i - 1][0];
                        $(listTag[i]).removeClass("none");
                    }
                }
            } else {
                loadLocaldData();
            }
        }
        se_oldval = val;
    }
});