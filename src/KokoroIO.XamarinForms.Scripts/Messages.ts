interface MergeInfo {
    Id: number;
    IsMerged: boolean;
}
interface MessageInfo extends MergeInfo {
    IdempotentKey: string;
    ProfileId: string;
    Avatar: string;
    ScreenName: string;
    DisplayName: string;
    PublishedAt: string;
    IsBot: boolean;
    HtmlContent: string;
    EmbedContents: EmbedContent[];
    IsNsfw: boolean;
    CanDelete: boolean;
    IsDeleted: boolean;
}
interface EmbedContent {
    url: string;
    data: EmbedData;
}
interface EmbedData {
    type: "MixedContent" | "SingleImage" | "SingleVideo" | "SingleAudio";
    title: string;
    description: string;
    author_name: string;
    author_url: string;
    provider_name: string;
    provider_url: string;
    cache_age: number;
    metadata_image: EmbedDataMedia;
    url: string;
    restriction_policy: "Unknown" | "Safe" | "Restricted";
    medias: EmbedDataMedia[];
}
interface EmbedDataMedia {
    type: "Image" | "Video" | "Audio";
    thumbnail: EmbedDataImageInfo;
    raw_url: string;
    location: string;
    restriction_policy: "Unknown" | "Safe" | "Restricted";
}
interface EmbedDataImageInfo {
    url: string;
    width: number;
    height: number;
}

interface Window {
    setMessages(messages: MessageInfo[]);
    addMessages(messages: MessageInfo[], merged: MergeInfo[]);
    removeMessages(ids: number[], idempotentKeys: string[], merged: MergeInfo[]);
    showMessage(id: number, toTop: boolean, showNewMessage?: boolean);

    setHasUnread(value: boolean);
}

(function () {
    const IS_BOTTOM_MARGIN = 15;
    const IS_TOP_MARGIN = 4;
    const LOAD_OLDER_MARGIN = 300;
    const LOAD_NEWER_MARGIN = 300;
    const HIDE_CONTENT_MARGIN = 60;

    const LOG_VIEWPORT = false;

    var isDesktop = document.documentElement.classList.contains("html-desktop");
    var isTablet = document.documentElement.classList.contains("html-tablet");
    if (!isDesktop && !isTablet) {
        document.documentElement.classList.add("html-phone");
    }

    var _hasUnread = false;
    var _isUpdating = false;

    function getTalksHost(): HTMLElement {
        return document.body;
    }

    window.setHasUnread = function (value: boolean) {
        _hasUnread = !!value;
    };

    window.setMessages = function (messages: MessageInfo[]) {
        _isUpdating = true;
        try {
            let b = getTalksHost();
            console.debug("Setting " + (messages ? messages.length : 0) + " messages");
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
            let b = getTalksHost();
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
            let b = getTalksHost();
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
        let b = getTalksHost();
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
                let b = getTalksHost();
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
            let b = getTalksHost();
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

    function _padLeft(i: number, l: number): string {
        var offset = l == 1 ? 10 : l == 2 ? 100 : Math.pow(10, l);

        if (i > offset) {
            var s = i.toFixed(0);
            return s.substr(s.length - l, l);
        }

        return (i + offset).toFixed(0).substr(1, l);
    }

    function FA_ANCHOR(url: string, faClass: string, disabled?: boolean): HTMLAnchorElement {
        let a = document.createElement("a");
        if (disabled) {
            a.href = "javascript:void(0)";
            a.classList.add("disabled");
        } else {
            a.href = url;
        }
        a.appendChild(FA(faClass));
        return a;
    }

    function FA(className: string): HTMLUnknownElement {
        let r = document.createElement("i");
        r.classList.add("fa");
        r.classList.add(className);
        return r;
    }

    function createTaklElement(m: MessageInfo): HTMLDivElement {
        var id = m.Id;

        let talk = document.createElement("div");
        talk.classList.add("talk");
        talk.classList.add(m.IsMerged ? "continued" : "not-continued");
        if (id) {
            talk.id = "talk" + id;
            talk.setAttribute("data-message-id", id.toString());

            if (!m.IsDeleted) {
                if (isDesktop || isTablet) {
                    let control = document.createElement("div");
                    control.classList.add("message-menu");

                    // reply
                    control.appendChild(FA_ANCHOR(`http://kokoro.io/client/control?event=replyToMessage&id=${m.Id}`, "fa-reply"));

                    // copy
                    control.appendChild(FA_ANCHOR(`http://kokoro.io/client/control?event=copyMessage&id=${m.Id}`, "fa-clipboard"));

                    // delete
                    control.appendChild(FA_ANCHOR(`http://kokoro.io/client/control?event=deleteMessage&id=${m.Id}`, "fa-trash", !m.CanDelete));

                    talk.appendChild(control);
                } else {
                    let control = document.createElement("div");
                    control.classList.add("message-menu");
                    control.appendChild(FA_ANCHOR(`http://kokoro.io/client/control?event=messageMenu&id=${m.Id}`, "fa-chevron-down"));
                    talk.appendChild(control);
                }
            }
        } else {
            var idempotentKey = m.IdempotentKey;
            if (idempotentKey) {
                talk.setAttribute("data-idempotent-key", idempotentKey);
            }
        }

        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);

            var profUrl = `https://kokoro.io/profiles/${m.ProfileId}`;

            var imgLink = document.createElement("a");
            imgLink.href = profUrl;
            imgLink.classList.add("img-rounded");
            avatar.appendChild(imgLink);

            var img = document.createElement("img");
            img.src = m.Avatar;
            imgLink.appendChild(img);

            var message = document.createElement("div");
            message.classList.add("message");
            talk.appendChild(message);

            var speaker = document.createElement("div");
            speaker.classList.add("speaker");
            message.appendChild(speaker);

            var name = document.createElement("a");
            name.innerText = m.DisplayName;
            name.href = profUrl;
            speaker.appendChild(name);

            if (m.IsBot) {
                var small = document.createElement("small");
                small.className = "label label-default";
                small.innerText = "bot";
                speaker.appendChild(small);
            }

            var small = document.createElement("small");
            small.classList.add("timeleft", "text-muted");
            if (m.PublishedAt) {
                try {
                    let d = new Date(Date.parse(m.PublishedAt));

                    small.innerText = (d.getFullYear() == new Date().getFullYear() ? '' : _padLeft(d.getFullYear(), 4) + '/')
                        + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2);

                    small.title = _padLeft(d.getFullYear(), 4)
                        + '/' + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2)
                        + ':' + _padLeft(d.getSeconds(), 2);
                } catch (ex) {
                    small.innerText = m.PublishedAt;
                }
            } else {
                small.innerText = "Now sending...";
            }
            speaker.appendChild(small);

            var filteredText = document.createElement("div");
            filteredText.classList.add(m.IsDeleted ? "deleted_text" : "filtered_text");
            filteredText.innerHTML = m.HtmlContent;
            message.appendChild(filteredText);

            if (m.EmbedContents && m.EmbedContents.length > 0) {
                var ecs = document.createElement("div");
                ecs.classList.add("embed_contents");
                message.appendChild(ecs);

                let d;
                try {
                    for (var i = 0; i < m.EmbedContents.length; i++) {
                        var e = m.EmbedContents[i];
                        if (!e) {
                            continue;
                        }
                        d = e.data;
                        if (!d) {
                            continue;
                        }
                        var ec = document.createElement("div");
                        ec.classList.add("embed_content");
                        ecs.appendChild(ec);

                        switch (d.type) {
                            case 'MixedContent':
                                ec.appendChild(_createEmbedContent(m, d, false));
                                break;
                            case 'SingleImage':
                            case 'SingleVideo':
                            case 'SingleAudio':
                                ec.appendChild(_createEmbedContent(m, d, true));
                                break;
                            default:
                                console.warn("Unknown embed data: ", d);
                                break;
                        }
                    }
                } catch (ex) {
                    ecs.innerHTML = "";

                    var err = document.createElement('p');
                    err.innerText = ex;
                    ecs.appendChild(err);

                    var json = document.createElement('pre');
                    json.innerText = JSON.stringify(d);
                    ecs.appendChild(json);
                }
            }
        } catch (ex) {
            talk.innerText = ex;
        }

        return talk;
    }

    function _createEmbedContent(message: MessageInfo, d: EmbedData, hideInfo: boolean): HTMLDivElement {
        var r = document.createElement("div");
        r.classList.add("embed-" + d.type.toLowerCase());

        if (!hideInfo) {
            var meta = document.createElement("div");
            meta.classList.add("meta");
            r.appendChild(meta);

            if (d.metadata_image) {
                var m = d.metadata_image;

                var thd = _createMediaDiv(m, d, message, "embed-thumbnail");

                if (thd) {
                    var thumb = document.createElement("div");
                    thumb.classList.add("thumb");
                    meta.appendChild(thumb);

                    thumb.appendChild(thd);
                }
            }
            var info = document.createElement("div");
            info.classList.add("info");
            meta.appendChild(info);

            if (d.title) {
                var titleDiv = document.createElement("div");
                titleDiv.classList.add("title");
                info.appendChild(titleDiv);

                var titleLink = document.createElement("a");
                titleLink.href = d.url;
                titleDiv.appendChild(titleLink);

                var title = document.createElement("strong");
                title.innerText = d.title;
                titleLink.appendChild(title);
            }

            if (d.description) {
                var descriptionDiv = document.createElement("div");
                descriptionDiv.classList.add("description");
                info.appendChild(descriptionDiv);

                let description = document.createElement("p");

                let re = /(https?:\/\/[a-z0-9]+(?:[-.][a-z0-9]+)*(?:\/|[!$&-;=?-Z\\^_a-~]|%[A-F0-9]{2})*)/;

                let ary = d.description.split(re);

                if (ary && ary.length > 1) {
                    for (let i = 0; i < ary.length; i++) {
                        let t = ary[i];
                        if (i % 2 == 0) {
                            description.appendChild(document.createTextNode(t));
                        } else {
                            let a = document.createElement("a");
                            a.setAttribute("href", t);
                            a.appendChild(document.createTextNode(t));

                            description.appendChild(a);
                        }
                    }
                } else {
                    description.innerText = d.description;
                }

                descriptionDiv.appendChild(description);
            }
        }

        if (d.medias && d.medias.length > 0) {
            var medias = document.createElement("div");
            medias.classList.add("medias");

            for (var i = 0; i < d.medias.length; i++) {
                var m = d.medias[i];
                if (m) {
                    var md = _createMediaDiv(m, d, message);
                    if (md) {
                        medias.appendChild(md);
                    }
                }
            }

            if (medias.children.length > 0) {
                r.appendChild(medias);
            }
        }

        return r;
    }

    function _createMediaDiv(media: EmbedDataMedia, data: EmbedData, message: MessageInfo, className?: string): HTMLDivElement {
        var tu = (media.thumbnail ? media.thumbnail.url : null) || media.raw_url;

        if (!tu) {
            return null;
        }

        var em = document.createElement("div");
        em.classList.add(className || "embed_media");

        var a = document.createElement("a");
        a.href = media.location || media.raw_url || data.url;
        em.appendChild(a);

        var img = document.createElement("img");
        img.classList.add("img-rounded");
        img.src = tu;
        a.appendChild(img);

        var policies = [(message.IsNsfw ? "Restricted" : "Unknown"), media.restriction_policy, data.restriction_policy];

        if (policies.filter(p => p !== "Unknown")[0] === "Restricted") {
            em.classList.add("nsfw");

            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }

        return em;
    }

    function _insertBefore(talk: HTMLDivElement, aft: HTMLDivElement, scroll: boolean) {
        let b = getTalksHost();
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
                        let b = getTalksHost();
                        b.scrollTop = talk.offsetTop;
                    }, 1);
                }
            } else {
                let b = getTalksHost();
                b.scrollTop = talk.offsetTop;
            }
        }
    }

    function _afterTalkInserted(talk: HTMLDivElement, previousHeight?: number) {
        let b = getTalksHost();
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
            anchors[i].removeAttribute("target");
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
                    let b = getTalksHost();
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

    var _visibleIds: string;

    var _lastTimer;
    var _lastScrollHeight;
    var _lastScrollTop;

    function _reportVisibilities() {
        let b = getTalksHost();
        _lastScrollTop = b.scrollTop;
        _lastScrollHeight = b.scrollHeight;

        if (_lastTimer) {
            clearTimeout(_lastTimer);
        }

        _lastTimer = setTimeout(function () {
            _lastTimer = null;

            let b = getTalksHost();

            if (_lastScrollTop != b.scrollTop || _lastScrollHeight != b.scrollHeight) {
                _reportVisibilities();
            } else {
                _reportVisibilitiesCore();
            }
        }, 250);
    }
    function _reportVisibilitiesCore() {
        let b = getTalksHost();

        let talks = b.children;

        let ids = "";

        for (let i = 0; i < talks.length; i++) {
            let talk = <HTMLDivElement>talks[i];

            if (b.scrollTop < talk.offsetTop + talk.clientHeight
                && talk.offsetTop < b.scrollTop + b.clientHeight) {
                var id = talk.getAttribute("data-message-id") || talk.getAttribute("data-idempotent-key");

                if (ids.length > 0) {
                    ids += "," + id;
                } else {
                    ids = id;
                }
            } else if (ids.length > 0) {
                break;
            }
        }

        if (_visibleIds !== ids) {
            location.href = "http://kokoro.io/client/control?event=visibility&ids=" + ids;
            _visibleIds = ids;
            if (LOG_VIEWPORT) {
                console.log(`visibility changed: scrollTop: ${b.scrollTop}`
                    + `, clientHeight: ${b.clientHeight}`
                    + `, lastElementChild.offsetTop: ${b.lastElementChild ? (b.lastElementChild as HTMLElement).offsetTop : -1}`
                    + `, lastElementChild.clientHeight: ${b.lastElementChild ? (b.lastElementChild as HTMLElement).clientHeight : -1}`);
            }
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        var windowWidth = window.innerWidth;

        window.addEventListener("resize", function () {
            if (window.innerWidth == windowWidth) {
                return;
            }

            windowWidth = window.innerWidth;
            let b = getTalksHost();
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                talk.setAttribute("data-height", talk.clientHeight.toString());
            }

            _reportVisibilities();
        });

        document.addEventListener("scroll", function () {
            let b = getTalksHost();

            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = <HTMLDivElement>talks[i];

                var hidden = (talk.offsetTop + talk.clientHeight + HIDE_CONTENT_MARGIN < b.scrollTop
                    || b.scrollTop + b.clientHeight < talk.offsetTop - HIDE_CONTENT_MARGIN)
                    && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0);

                if (hidden) {
                    if (!talk.classList.contains("hidden")) {
                        talk.style.height = talk.clientHeight.toString() + 'px';
                        talk.classList.add("hidden");
                    }
                } else {
                    if (talk.classList.contains("hidden")) {
                        talk.style.height = null;
                        talk.classList.remove("hidden");
                    }
                }
            }

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
                                location.href = `http://kokoro.io/client/control?event=append&count=${getTalksHost().children.length}`;
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
})();