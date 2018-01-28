/// <reference path="Constants.ts" />
/// <reference path="Builder.ts" />
interface Window {
    setMessages(messages: MessageInfo[]);
    addMessages(messages: MessageInfo[], merged: MergeInfo[]);
    removeMessages(ids: number[], idempotentKeys: string[], merged: MergeInfo[]);
    showMessage(id: number, toTop: boolean, showNewMessage?: boolean);

    setHasUnread(value: boolean);
}

module Messages {
    var _hasUnread = false;
    var _isUpdating = false;

    let displayed: HTMLDivElement[];

    function HOST(): HTMLElement {
        return document.body;
    }

    window.setHasUnread = function (value: boolean) {
        _hasUnread = !!value;
    };

    window.setMessages = function (messages: MessageInfo[]) {
        _isUpdating = true;
        try {
            let b = HOST();
            console.debug("Setting " + (messages ? messages.length : 0) + " messages");
            if (!messages || messages.length === 0) {
                displayed = null;
            }
            b.innerHTML = "";
            _addMessagessCore(messages, null, false);

            b.scrollTop = b.scrollHeight - b.clientHeight;
            _reportVisibilities();
        } finally {
            _isUpdating = false;
        }
    }

    window.addMessages = function (messages: MessageInfo[], merged: MergeInfo[], showNewMessage?: boolean) {
        _isUpdating = true;
        try {
            let b = HOST();
            console.debug("Adding " + (messages ? messages.length : 0) + " messages");

            var isEmpty = b.children.length === 0;
            showNewMessage = showNewMessage && !isEmpty;

            _addMessagessCore(messages, merged, !showNewMessage && !isEmpty);

            if (isEmpty) {
                b.scrollTop = b.scrollHeight - b.clientHeight;
            } else if (showNewMessage && messages && messages.length > 0) {
                var minId = Number.MAX_VALUE;
                messages.forEach(v => minId = Math.min(minId, v.Id));

                var talk = document.getElementById("talk" + minId);
                if (talk) {
                    _bringToTop(talk);
                }
            }
            _reportVisibilities();
        } finally {
            _isUpdating = false;
        }
    }

    var removeMessages = window.removeMessages = function (ids: number[], idempotentKeys: string[], merged: MergeInfo[]) {
        _isUpdating = true;
        try {
            console.debug("Removing " + ((ids ? ids.length : 0) + (idempotentKeys ? idempotentKeys.length : 0)) + " messages");
            let b = HOST();
            if (ids) {
                for (var i = 0; i < ids.length; i++) {
                    var talk = document.getElementById('talk' + ids[i]);

                    if (talk) {
                        var nt = talk.offsetTop < b.scrollTop ? b.scrollTop - talk.clientHeight : b.scrollTop;

                        talk.remove();

                        b.scrollTop = nt;
                    }
                }
            }
            if (idempotentKeys) {
                for (let i = 0; i < idempotentKeys.length; i++) {
                    let talk = _talkByIdempotentKey(idempotentKeys[i]);
                    if (talk) {
                        var nt = talk.offsetTop < b.scrollTop ? b.scrollTop - talk.clientHeight : b.scrollTop;

                        talk.remove();

                        b.scrollTop = nt;
                    }
                }
            }
            updateContinued(merged, true);
            _reportVisibilities();
        } finally {
            _isUpdating = false;
        }
    }

    function _addMessagessCore(messages: MessageInfo[], merged: MergeInfo[], scroll: boolean) {
        let b = HOST();
        var lastTalk = scroll && b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight ? b.lastElementChild : null;
        scroll = scroll && !lastTalk;
        if (messages) {
            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];

                var id = m.Id;

                if (!id) {
                    let talk = createTaklElement(m);
                    b.appendChild(talk);
                    _afterTalkInserted(talk);

                    continue;
                } else {
                    let cur = <HTMLElement>document.getElementById("talk" + id)
                        || (m.IdempotentKey ? _talkByIdempotentKey(m.IdempotentKey) : null);

                    if (cur) {
                        let shoudScroll = scroll && cur.offsetTop + cur.clientHeight - IS_TOP_MARGIN < b.scrollTop;
                        let st = b.scrollTop - cur.clientHeight;

                        let talk = createTaklElement(m);
                        b.insertBefore(talk, cur);
                        _afterTalkInserted(talk, cur.clientHeight);
                        cur.remove();

                        if (scroll) {
                            b.scrollTop = st + talk.clientHeight;
                        }
                        continue;
                    }
                }

                for (; ;) {
                    let prev = <HTMLDivElement>b.children[j];
                    let aft = <HTMLDivElement>b.children[j + 1];
                    let pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    let aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;

                    if (!prev || (id != pid && !aft)) {
                        // console.debug("Appending message[" + id + "]");
                        let talk = createTaklElement(m);
                        b.appendChild(talk);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    } else if (id <= pid) {
                        let talk = createTaklElement(m);
                        if (id == pid) {
                            let shoudScroll = scroll && aft && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
                            let st = b.scrollTop - prev.clientHeight;

                            b.insertBefore(talk, prev);
                            _afterTalkInserted(talk, prev.clientHeight);
                            prev.remove();

                            if (scroll) {
                                b.scrollTop = st + talk.clientHeight;
                            }
                        } else {
                            _insertBefore(talk, prev, scroll);
                            j++;
                        }
                        break;
                    } else if (id < aid) {
                        // console.debug("Inserting message[" + id + "] before " + aid);
                        let talk = createTaklElement(m);

                        _insertBefore(talk, aft, scroll);

                        j++;
                        break;
                    } else {
                        j++;
                    }
                }
            }
        }
        updateContinued(merged, scroll);

        if (lastTalk) {
            _bringToTop(<HTMLDivElement>lastTalk);
        }
    }

    window.showMessage = function (id: number, toTop: boolean) {
        _isUpdating = true;
        try {
            console.debug(`showing message[${id}]`);
            var talk = document.getElementById("talk" + id);
            if (talk) {
                let b = HOST();
                console.log(`current scrollTo is ${b.scrollTop}, and offsetTop is ${talk.offsetTop}`);
                if (talk.offsetTop < b.scrollTop || toTop) {
                    console.log(`scrolling to ${talk.offsetTop}`);
                    b.scrollTop = talk.offsetTop;
                } else if (b.scrollTop + b.clientHeight < talk.offsetTop - talk.clientHeight) {
                    console.log(`scrolling to ${talk.offsetTop - b.clientHeight}`);
                    b.scrollTop = talk.offsetTop - b.clientHeight;
                }
            }
        } finally {
            _isUpdating = false;
        }
    };

    function updateContinued(merged: MergeInfo[], scroll: boolean) {
        if (merged) {
            let b = HOST();
            for (var i = 0; i < merged.length; i++) {
                var m = merged[i];
                var id = m.Id;
                var isMerged = m.IsMerged;

                var talk = document.getElementById('talk' + id);
                if (talk) {
                    var shouldScroll = scroll && talk.offsetTop - IS_TOP_MARGIN < b.scrollTop;

                    var bt = b.scrollTop - talk.clientHeight;
                    talk.classList.remove(!isMerged ? "continued" : "not-continued");
                    talk.classList.add(isMerged ? "continued" : "not-continued");
                    if (shouldScroll) {
                        b.scrollTop = bt + talk.clientHeight;
                    }
                }
            }
        }
    }

    function _insertBefore(talk: HTMLDivElement, aft: HTMLDivElement, scroll: boolean) {
        let b = HOST();
        scroll = scroll && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
        var st = b.scrollTop;

        b.insertBefore(talk, aft);
        _afterTalkInserted(talk);

        if (scroll) {
            b.scrollTop = st + talk.clientHeight;
        }
    }

    function _bringToTop(talk: HTMLElement) {
        if (talk) {
            if (talk.offsetTop === 0) {
                if (talk.previousElementSibling) {
                    setTimeout(function () {
                        let b = HOST();
                        b.scrollTop = talk.offsetTop;
                    }, 1);
                }
            } else {
                let b = HOST();
                b.scrollTop = talk.offsetTop;
            }
        }
    }

    function _afterTalkInserted(talk: HTMLDivElement, previousHeight?: number) {
        let b = HOST();
        if (talk.offsetTop < b.scrollTop) {
            let delta = talk.clientHeight - (previousHeight || 0);
            if (delta != 0) {
                b.scrollTop += delta;
                console.log("scolled " + delta);
            }
        }

        talk.setAttribute("data-height", talk.clientHeight.toString());

        let anchors = talk.getElementsByTagName("a");
        for (let i = 0; i < anchors.length; i++) {
            let a = anchors[i];
            if (/^javascript:/.test(a.href) && !/^javascript:void\(0?\);?/.test(a.href)) {
                console.warn(`unsupported scheme: ${a.href}`);
                a.href = '#';
            }
            a.removeAttribute("target");
        }

        let imgs = talk.getElementsByTagName("img");
        talk.setAttribute("data-loading-images", imgs.length.toString());

        let handler;
        handler = function (e: Event) {
            let img = e.target;

            let talk = (<HTMLElement>img).parentElement;

            while (talk) {
                if (talk.classList.contains("talk")) {
                    talk.setAttribute("data-loading-images", (Math.max(0, (parseInt(talk.getAttribute("data-loading-images"), 10) - 1) || 0)).toString());

                    let ph = parseInt(talk.getAttribute("data-height"), 10);
                    let delta = talk.clientHeight - ph;
                    let b = HOST();
                    if (b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight - delta) {
                        // previous viewport was bottom.
                        b.scrollTop = b.scrollHeight - b.clientHeight;
                    } else if (talk.offsetTop < b.scrollTop) {
                        b.scrollTop += delta;
                    }
                    talk.setAttribute("data-height", talk.clientHeight.toString());

                    break;
                } else if (/^error$/i.test(e.type) && talk.classList.contains("embed_media")) {
                    let tp = talk.parentElement;

                    talk.remove();

                    if (tp.children.length === 0) {
                        tp.remove();
                    }
                    break;
                } else if (/^error$/i.test(e.type) && talk.classList.contains("thumb")) {
                    talk.remove();
                }

                talk = talk.parentElement;
            }

            img.removeEventListener("load", handler);
            img.removeEventListener("error", handler);
            _reportVisibilities();
        }

        for (let i = 0; i < imgs.length; i++) {
            imgs[i].addEventListener("load", handler);
            imgs[i].addEventListener("error", handler);
        }
    }

    function _talkByIdempotentKey(idempotentKey: string): HTMLDivElement {
        return <HTMLDivElement>document.querySelector('div.talk[data-idempotent-key=\"' + idempotentKey + "\"]");
    }

    let _visibleIds: string;

    function _reportVisibilities() {

        _determineDisplayedElements();
        const ids = displayed.map(e => e.getAttribute("data-message-id") || e.getAttribute("data-idempotent-key")).join(",");

        if (_visibleIds !== ids) {
            location.href = "http://kokoro.io/client/control?event=visibility&ids=" + ids;
            _visibleIds = ids;
            if (LOG_VIEWPORT) {
                const b = HOST();
                console.log(`visibility changed: scrollTop: ${b.scrollTop}`
                    + `, clientHeight: ${b.clientHeight}`
                    + `, lastElementChild.offsetTop: ${b.lastElementChild ? (b.lastElementChild as HTMLElement).offsetTop : -1}`
                    + `, lastElementChild.clientHeight: ${b.lastElementChild ? (b.lastElementChild as HTMLElement).clientHeight : -1}`);
            }
            _visibleIds = ids;
        }
    }

    function _isAbove(talk: HTMLDivElement, b: HTMLElement) {
        return talk.offsetTop + talk.clientHeight + HIDE_CONTENT_MARGIN < b.scrollTop;
    }

    function _isBelow(talk: HTMLDivElement, b: HTMLElement) {
        return b.scrollTop + b.clientHeight < talk.offsetTop - HIDE_CONTENT_MARGIN;
    }

    function _hideTalk(talk: HTMLElement) {
        if (!talk.classList.contains("hidden")) {
            talk.style.height = talk.clientHeight.toString() + 'px';
            talk.classList.add("hidden");
        }
    }

    function _showTalk(talk: HTMLElement) {
        if (talk.classList.contains("hidden")) {
            talk.style.height = null;
            talk.classList.remove("hidden");
        }
    }

    function _determineDisplayedElements() {
        const b = HOST();
        let displaying: HTMLDivElement[];

        if (displayed && displayed.length > 0) {
            for (let talk of displayed) {
                if (!talk.parentElement) {
                    continue;
                }
                if (_isAbove(talk, b)) {
                    continue;
                } else if (_isBelow(talk, b)) {
                    break;
                } else {
                    displaying = [];

                    if (displayed[0] === talk) {
                        for (let n = talk.previousSibling; n; n = n.previousSibling) {
                            const t = <HTMLDivElement>n;
                            if (t.nodeType === Node.ELEMENT_NODE) {
                                if (_isAbove(t, b)) {
                                    break;
                                }
                                displaying.unshift(t);
                            }
                        }
                    }

                    displaying.push(talk);

                    for (let n = talk.nextSibling; n; n = n.nextSibling) {
                        const t = <HTMLDivElement>n;
                        if (t.nodeType === Node.ELEMENT_NODE) {
                            if (_isBelow(t, b)) {
                                break;
                            }
                            displaying.push(t);
                        }
                    }

                    break;
                }
            }
        }

        if (displayed && displaying) {
            for (let talk of displayed) {
                if (displaying.indexOf(talk) < 0
                    && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0)) {
                    _hideTalk(talk);
                }
            }

            for (let talk of displaying) {
                _showTalk(talk);
            }
        } else {
            displaying = [];
            const talks = b.children;
            for (let i = 0; i < talks.length; i++) {
                const talk = <HTMLDivElement>talks[i];
                const visible = !_isAbove(talk, b) && !_isBelow(talk, b);
                const hidden = !visible && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0);

                if (hidden) {
                    _hideTalk(talk);
                } else {
                    _showTalk(talk);
                }

                if (visible) {
                    displaying.push(talk);
                }
            }
        }

        displayed = displaying;
    }

    document.addEventListener("DOMContentLoaded", function () {
        var windowWidth = window.innerWidth;

        window.addEventListener("resize", function () {
            if (window.innerWidth == windowWidth) {
                return;
            }

            windowWidth = window.innerWidth;
            let b = HOST();
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                talk.setAttribute("data-height", talk.clientHeight.toString());
            }

            _reportVisibilities();
        });

        document.addEventListener("scroll", function () {
            const b = HOST();

            _reportVisibilities();

            if (b.scrollHeight < b.clientHeight) {
                return;
            }

            if (b.scrollTop < LOAD_OLDER_MARGIN) {
                if (!_isUpdating) {
                    console.log("Loading older messages.");
                    location.href = `http://kokoro.io/client/control?event=prepend&count=${b.children.length}`;
                }
            } else {
                var fromBottom = b.scrollHeight - b.scrollTop - b.clientHeight;
                if (fromBottom < 4 || (_hasUnread && fromBottom < LOAD_NEWER_MARGIN)) {
                    if (!_isUpdating) {
                        console.log("Loading newer messages.");
                        location.href = `http://kokoro.io/client/control?event=append&count=${b.children.length}`;
                    }
                }
            }
        });

        var mouseDownStart = null;

        let hovered: HTMLElement;

        document.body.addEventListener("mousedown", function (e) {
            if (e.button === 0) {
                if (hovered) {
                    hovered.classList.remove("message-hover");
                }

                hovered = e.currentTarget as HTMLElement;

                while (hovered && !hovered.classList.contains("talk")) {
                    hovered = hovered.parentElement;
                }

                if (hovered) {
                    hovered.classList.add("message-hover");
                }

                var b = document.body;
                if (b.scrollTop + b.clientHeight + 4 > b.scrollHeight) {
                    mouseDownStart = new Date().getTime();

                    setTimeout(function () {
                        if (mouseDownStart !== null
                            && mouseDownStart + 800 < new Date().getTime()) {
                            if (!_isUpdating) {
                                console.log("Loading newer messages.");
                                location.href = `http://kokoro.io/client/control?event=append&count=${HOST().children.length}`;
                            }
                        }
                        mouseDownStart = null;
                    }, 1000);

                    return;
                }
            }
            mouseDownStart = null;
        });
        document.body.addEventListener("mouseup", function (e) {
            mouseDownStart = null;
        });
        document.body.addEventListener("mousemove", function (e) {
            if (hovered) {
                hovered.classList.remove("message-hover");
                hovered = null;
            }
        });
        document.body.addEventListener("wheel", function (e: WheelEvent) {
            if (e.ctrlKey) {
                e.preventDefault();
                return;
            }
        });
        location.href = "http://kokoro.io/client/control?event=loaded";
    });
}