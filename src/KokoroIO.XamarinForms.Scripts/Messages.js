(function () {
    var IS_BOTTOM_MARGIN = 15;
    var IS_TOP_MARGIN = 4;
    var LOAD_OLDER_MARGIN = 200;
    var LOAD_NEWER_MARGIN = 200;
    var HIDE_CONTENT_MARGIN = 60;
    var isDesktop = document.documentElement.classList.contains("html-desktop");
    var isTablet = document.documentElement.classList.contains("html-tablet");
    if (!isDesktop && !isTablet) {
        document.documentElement.classList.add("html-phone");
    }
    var _hasUnread = false;
    var _isUpdating = false;
    function getTalksHost() {
        return document.body;
    }
    window.setHasUnread = function (value) {
        _hasUnread = !!value;
    };
    window.setMessages = function (messages) {
        _isUpdating = true;
        try {
            var b = getTalksHost();
            console.debug("Setting " + (messages ? messages.length : 0) + " messages");
            b.innerHTML = "";
            _addMessagessCore(messages, null, false);
            b.scrollTop = b.scrollHeight - b.clientHeight;
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    window.addMessages = function (messages, merged, showNewMessage) {
        _isUpdating = true;
        try {
            var b = getTalksHost();
            console.debug("Adding " + (messages ? messages.length : 0) + " messages");
            var isEmpty = b.children.length === 0;
            showNewMessage = showNewMessage && !isEmpty;
            _addMessagessCore(messages, merged, !showNewMessage && !isEmpty);
            if (isEmpty) {
                b.scrollTop = b.scrollHeight - b.clientHeight;
            }
            else if (showNewMessage && messages && messages.length > 0) {
                var minId = Number.MAX_VALUE;
                messages.forEach(function (v) { return minId = Math.min(minId, v.Id); });
                var talk = document.getElementById("talk" + minId);
                if (talk) {
                    _bringToTop(talk);
                }
            }
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    var removeMessages = window.removeMessages = function (ids, idempotentKeys, merged) {
        _isUpdating = true;
        try {
            console.debug("Removing " + ((ids ? ids.length : 0) + (idempotentKeys ? idempotentKeys.length : 0)) + " messages");
            var b = getTalksHost();
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
                for (var i_1 = 0; i_1 < idempotentKeys.length; i_1++) {
                    var talk_1 = _talkByIdempotentKey(idempotentKeys[i_1]);
                    if (talk_1) {
                        var nt = talk_1.offsetTop < b.scrollTop ? b.scrollTop - talk_1.clientHeight : b.scrollTop;
                        talk_1.remove();
                        b.scrollTop = nt;
                    }
                }
            }
            updateContinued(merged, true);
            _reportVisibilities();
        }
        finally {
            _isUpdating = false;
        }
    };
    function _addMessagessCore(messages, merged, scroll) {
        var b = getTalksHost();
        var lastTalk = scroll && b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight ? b.lastElementChild : null;
        scroll = scroll && !lastTalk;
        if (messages) {
            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];
                var id = m.Id;
                if (!id) {
                    var talk = createTaklElement(m);
                    b.appendChild(talk);
                    _afterTalkInserted(talk);
                    continue;
                }
                else {
                    var cur = document.getElementById("talk" + id)
                        || (m.IdempotentKey ? _talkByIdempotentKey(m.IdempotentKey) : null);
                    if (cur) {
                        var shoudScroll = scroll && cur.offsetTop + cur.clientHeight - IS_TOP_MARGIN < b.scrollTop;
                        var st = b.scrollTop - cur.clientHeight;
                        var talk = createTaklElement(m);
                        b.insertBefore(talk, cur);
                        _afterTalkInserted(talk, cur.clientHeight);
                        cur.remove();
                        if (scroll) {
                            b.scrollTop = st + talk.clientHeight;
                        }
                        continue;
                    }
                }
                for (;;) {
                    var prev = b.children[j];
                    var aft = b.children[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;
                    if (!prev || (id != pid && !aft)) {
                        // console.debug("Appending message[" + id + "]");
                        var talk = createTaklElement(m);
                        b.appendChild(talk);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    }
                    else if (id <= pid) {
                        var talk = createTaklElement(m);
                        if (id == pid) {
                            var shoudScroll = scroll && aft && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
                            var st = b.scrollTop - prev.clientHeight;
                            b.insertBefore(talk, prev);
                            _afterTalkInserted(talk, prev.clientHeight);
                            prev.remove();
                            if (scroll) {
                                b.scrollTop = st + talk.clientHeight;
                            }
                        }
                        else {
                            _insertBefore(talk, prev, scroll);
                            j++;
                        }
                        break;
                    }
                    else if (id < aid) {
                        // console.debug("Inserting message[" + id + "] before " + aid);
                        var talk = createTaklElement(m);
                        _insertBefore(talk, aft, scroll);
                        j++;
                        break;
                    }
                    else {
                        j++;
                    }
                }
            }
        }
        updateContinued(merged, scroll);
        if (lastTalk) {
            _bringToTop(lastTalk);
        }
    }
    window.showMessage = function (id, toTop) {
        _isUpdating = true;
        try {
            console.debug("showing message[" + id + "]");
            var talk = document.getElementById("talk" + id);
            if (talk) {
                var b = getTalksHost();
                console.log("current scrollTo is " + b.scrollTop + ", and offsetTop is " + talk.offsetTop);
                if (talk.offsetTop < b.scrollTop || toTop) {
                    console.log("scrolling to " + talk.offsetTop);
                    b.scrollTop = talk.offsetTop;
                }
                else if (b.scrollTop + b.clientHeight < talk.offsetTop - talk.clientHeight) {
                    console.log("scrolling to " + (talk.offsetTop - b.clientHeight));
                    b.scrollTop = talk.offsetTop - b.clientHeight;
                }
            }
        }
        finally {
            _isUpdating = false;
        }
    };
    function updateContinued(merged, scroll) {
        if (merged) {
            var b = getTalksHost();
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
    function _padLeft(i, l) {
        var offset = l == 1 ? 10 : l == 2 ? 100 : Math.pow(10, l);
        if (i > offset) {
            var s = i.toFixed(0);
            return s.substr(s.length - l, l);
        }
        return (i + offset).toFixed(0).substr(1, l);
    }
    function FA_ANCHOR(url, faClass, disabled) {
        var a = document.createElement("a");
        if (disabled) {
            a.href = "javascript:void(0)";
            a.classList.add("disabled");
        }
        else {
            a.href = url;
        }
        a.appendChild(FA(faClass));
        return a;
    }
    function FA(className) {
        var r = document.createElement("i");
        r.classList.add("fa");
        r.classList.add(className);
        return r;
    }
    function createTaklElement(m) {
        var id = m.Id;
        var talk = document.createElement("div");
        talk.classList.add("talk");
        talk.classList.add(m.IsMerged ? "continued" : "not-continued");
        if (id) {
            talk.id = "talk" + id;
            talk.setAttribute("data-message-id", id.toString());
            if (!m.IsDeleted) {
                if (isDesktop || isTablet) {
                    var control = document.createElement("div");
                    control.classList.add("message-menu");
                    // reply
                    control.appendChild(FA_ANCHOR("http://kokoro.io/client/control?event=replyToMessage&id=" + m.Id, "fa-reply"));
                    // copy
                    control.appendChild(FA_ANCHOR("http://kokoro.io/client/control?event=copyMessage&id=" + m.Id, "fa-clipboard"));
                    // delete
                    control.appendChild(FA_ANCHOR("http://kokoro.io/client/control?event=deleteMessage&id=" + m.Id, "fa-trash", !m.CanDelete));
                    talk.appendChild(control);
                }
                else {
                    var control = document.createElement("div");
                    control.classList.add("message-menu");
                    control.appendChild(FA_ANCHOR("http://kokoro.io/client/control?event=messageMenu&id=" + m.Id, "fa-chevron-down"));
                    talk.appendChild(control);
                }
            }
        }
        else {
            var idempotentKey = m.IdempotentKey;
            if (idempotentKey) {
                talk.setAttribute("data-idempotent-key", idempotentKey);
            }
        }
        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);
            var profUrl = "https://kokoro.io/profiles/" + m.ProfileId;
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
                    var d = new Date(Date.parse(m.PublishedAt));
                    small.innerText = _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2);
                    small.title = _padLeft(d.getFullYear(), 4)
                        + '/' + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2)
                        + ':' + _padLeft(d.getSeconds(), 2);
                }
                catch (ex) {
                    small.innerText = m.PublishedAt;
                }
            }
            else {
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
                var d = void 0;
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
                }
                catch (ex) {
                    ecs.innerHTML = "";
                    var err = document.createElement('p');
                    err.innerText = ex;
                    ecs.appendChild(err);
                    var json = document.createElement('pre');
                    json.innerText = JSON.stringify(d);
                    ecs.appendChild(json);
                }
            }
        }
        catch (ex) {
            talk.innerText = ex;
        }
        return talk;
    }
    function _createEmbedContent(message, d, hideInfo) {
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
                var description = document.createElement("p");
                var re = /(https?:\/\/[a-z0-9]+(?:[-.][a-z0-9]+)*(?:\/|[!$&-;=?-Z\\^_a-~]|%[A-F0-9]{2})*)/;
                var ary = d.description.split(re);
                if (ary && ary.length > 1) {
                    for (var i_2 = 0; i_2 < ary.length; i_2++) {
                        var t = ary[i_2];
                        if (i_2 % 2 == 0) {
                            description.appendChild(document.createTextNode(t));
                        }
                        else {
                            var a = document.createElement("a");
                            a.setAttribute("href", t);
                            a.appendChild(document.createTextNode(t));
                            description.appendChild(a);
                        }
                    }
                }
                else {
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
    function _createMediaDiv(media, data, message, className) {
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
        if (policies.filter(function (p) { return p !== "Unknown"; })[0] === "Restricted") {
            em.classList.add("nsfw");
            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }
        return em;
    }
    function _insertBefore(talk, aft, scroll) {
        var b = getTalksHost();
        scroll = scroll && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
        var st = b.scrollTop;
        b.insertBefore(talk, aft);
        _afterTalkInserted(talk);
        if (scroll) {
            b.scrollTop = st + talk.clientHeight;
        }
    }
    function _bringToTop(talk) {
        if (talk) {
            if (talk.offsetTop === 0) {
                if (talk.previousElementSibling) {
                    setTimeout(function () {
                        var b = getTalksHost();
                        b.scrollTop = talk.offsetTop;
                    }, 1);
                }
            }
            else {
                var b = getTalksHost();
                b.scrollTop = talk.offsetTop;
            }
        }
    }
    function _afterTalkInserted(talk, previousHeight) {
        var b = getTalksHost();
        if (talk.offsetTop < b.scrollTop) {
            var delta = talk.clientHeight - (previousHeight || 0);
            if (delta != 0) {
                b.scrollTop += delta;
                console.log("scolled " + delta);
            }
        }
        talk.setAttribute("data-height", talk.clientHeight.toString());
        var anchors = talk.getElementsByTagName("a");
        for (var i = 0; i < anchors.length; i++) {
            anchors[i].removeAttribute("target");
        }
        var imgs = talk.getElementsByTagName("img");
        talk.setAttribute("data-loading-images", imgs.length.toString());
        var handler;
        handler = function (e) {
            var img = e.target;
            var talk = img.parentElement;
            while (talk) {
                if (talk.classList.contains("talk")) {
                    talk.setAttribute("data-loading-images", (Math.max(0, (parseInt(talk.getAttribute("data-loading-images"), 10) - 1) || 0)).toString());
                    var ph = parseInt(talk.getAttribute("data-height"), 10);
                    var delta = talk.clientHeight - ph;
                    var b_1 = getTalksHost();
                    if (b_1.scrollTop + b_1.clientHeight + IS_BOTTOM_MARGIN > b_1.scrollHeight - delta) {
                        // previous viewport was bottom.
                        b_1.scrollTop = b_1.scrollHeight - b_1.clientHeight;
                    }
                    else if (talk.offsetTop < b_1.scrollTop) {
                        b_1.scrollTop += delta;
                    }
                    talk.setAttribute("data-height", talk.clientHeight.toString());
                    break;
                }
                else if (/^error$/i.test(e.type) && talk.classList.contains("embed_media")) {
                    var tp = talk.parentElement;
                    talk.remove();
                    if (tp.children.length === 0) {
                        tp.remove();
                    }
                    break;
                }
                else if (/^error$/i.test(e.type) && talk.classList.contains("thumb")) {
                    talk.remove();
                }
                talk = talk.parentElement;
            }
            img.removeEventListener("load", handler);
            img.removeEventListener("error", handler);
            _reportVisibilities();
        };
        for (var i = 0; i < imgs.length; i++) {
            imgs[i].addEventListener("load", handler);
            imgs[i].addEventListener("error", handler);
        }
    }
    function _talkByIdempotentKey(idempotentKey) {
        return document.querySelector('div.talk[data-idempotent-key=\"' + idempotentKey + "\"]");
    }
    var _visibleIds;
    function _reportVisibilities() {
        var b = getTalksHost();
        var talks = b.children;
        var ids = "";
        for (var i = 0; i < talks.length; i++) {
            var talk = talks[i];
            if (b.scrollTop < talk.offsetTop + talk.clientHeight
                && talk.offsetTop < b.scrollTop + b.clientHeight) {
                var id = talk.getAttribute("data-message-id") || talk.getAttribute("data-idempotent-key");
                if (ids.length > 0) {
                    ids += "," + id;
                }
                else {
                    ids = id;
                }
            }
            else if (ids.length > 0) {
                break;
            }
        }
        if (_visibleIds !== ids) {
            location.href = "http://kokoro.io/client/control?event=visibility&ids=" + ids;
            _visibleIds = ids;
        }
    }
    document.addEventListener("DOMContentLoaded", function () {
        var windowWidth = window.innerWidth;
        window.addEventListener("resize", function () {
            if (window.innerWidth == windowWidth) {
                return;
            }
            windowWidth = window.innerWidth;
            var b = getTalksHost();
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                talk.setAttribute("data-height", talk.clientHeight.toString());
            }
            _reportVisibilities();
        });
        document.addEventListener("scroll", function () {
            var b = getTalksHost();
            var talks = b.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                var hidden = (talk.offsetTop + talk.clientHeight + HIDE_CONTENT_MARGIN < b.scrollTop
                    || b.scrollTop + b.clientHeight < talk.offsetTop - HIDE_CONTENT_MARGIN)
                    && !(parseInt(talk.getAttribute("data-loading-images"), 10) > 0);
                if (hidden) {
                    if (!talk.classList.contains("hidden")) {
                        talk.style.height = talk.clientHeight.toString() + 'px';
                        talk.classList.add("hidden");
                    }
                }
                else {
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
                    location.href = "http://kokoro.io/client/control?event=prepend&count=" + b.children.length;
                }
            }
            else {
                var fromBottom = b.scrollHeight - b.scrollTop - b.clientHeight;
                if (fromBottom < 4 || (_hasUnread && fromBottom < LOAD_NEWER_MARGIN)) {
                    if (!_isUpdating) {
                        console.log("Loading newer messages.");
                        location.href = "http://kokoro.io/client/control?event=append&count=" + b.children.length;
                    }
                }
            }
        });
        var mouseDownStart = null;
        var hovered;
        document.body.addEventListener("mousedown", function (e) {
            if (e.button === 0) {
                if (hovered) {
                    hovered.classList.remove("message-hover");
                }
                hovered = e.currentTarget;
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
                                location.href = "http://kokoro.io/client/control?event=append&count=" + getTalksHost().children.length;
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
        document.body.addEventListener("wheel", function (e) {
            if (e.ctrlKey) {
                e.preventDefault();
                return;
            }
        });
        location.href = "http://kokoro.io/client/control?event=loaded";
    });
})();
