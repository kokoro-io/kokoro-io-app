(function () {
    var IS_BOTTOM_MARGIN = 15;
    var IS_TOP_MARGIN = 4;
    var LOAD_OLDER_MARGIN = 200;
    var LOAD_NEWER_MARGIN = 200;
    var HIDE_CONTENT_MARGIN = 60;
    var _hasUnread = false;
    window.setHasUnread = function (value) {
        _hasUnread = !!value;
    };
    window.setMessages = function (messages) {
        console.debug("Setting " + (messages ? messages.length : 0) + " messages");
        document.body.innerHTML = "";
        _addMessagessCore(messages, null, false);
        var b = document.body;
        b.scrollTop = b.scrollHeight - b.clientHeight;
    };
    window.addMessages = function (messages, merged, showNewMessage) {
        console.debug("Adding " + (messages ? messages.length : 0) + " messages");
        var isEmpty = document.body.children.length === 0;
        showNewMessage = showNewMessage && !isEmpty;
        _addMessagessCore(messages, merged, !showNewMessage && !isEmpty);
        if (isEmpty) {
            var b = document.body;
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
    };
    var removeMessages = window.removeMessages = function (ids, merged) {
        console.debug("Removing " + (ids ? ids.length : 0) + " messages");
        if (ids) {
            var j = 0;
            var b = document.body;
            for (var i = 0; i < ids.length; i++) {
                var talk = document.getElementById('talk' + ids[i]);
                if (talk) {
                    var nt = talk.offsetTop < b.scrollTop ? b.scrollTop - talk.clientHeight : b.scrollTop;
                    talk.remove();
                    b.scrollTop = nt;
                }
            }
        }
        updateContinued(merged, true);
    };
    function _addMessagessCore(messages, merged, scroll) {
        var b = document.body;
        var lastTalk = scroll && b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight ? b.lastElementChild : null;
        scroll = scroll && !lastTalk;
        if (messages) {
            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];
                var id = m.Id;
                var avatarUrl = m.Avatar;
                var displayName = m.DisplayName;
                var publishedAt = m.PublishedAt;
                var content = m.Content;
                var isMerged = m.IsMerged;
                // console.debug("Processing message[" + id + "]");
                for (;;) {
                    var prev = b.children[j];
                    var aft = b.children[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;
                    if (!prev || (id != pid && !aft)) {
                        // console.debug("Appending message[" + id + "]");
                        var talk = createTaklElement(m);
                        document.body.appendChild(talk);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    }
                    else if (id <= pid) {
                        var talk = createTaklElement(m);
                        if (id == pid) {
                            var shoudScroll = scroll && aft && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
                            var st = b.scrollTop - prev.clientHeight;
                            document.body.insertBefore(talk, prev);
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
        console.debug("showing message[" + id + "]");
        var talk = document.getElementById("talk" + id);
        if (talk) {
            var b = document.body;
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
    };
    function updateContinued(merged, scroll) {
        if (merged) {
            var b = document.body;
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
    function createTaklElement(m) {
        var id = m.Id;
        var avatarUrl = m.Avatar;
        var displayName = m.DisplayName;
        var publishedAt = m.PublishedAt;
        var content = m.Content;
        var isMerged = m.IsMerged;
        var embeds = m.EmbedContents;
        var talk = document.createElement("div");
        talk.id = "talk" + id;
        talk.classList.add("talk");
        talk.classList.add(isMerged ? "continued" : "not-continued");
        talk.setAttribute("data-message-id", id.toString());
        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);
            var imgLink = document.createElement("a");
            imgLink.classList.add("img-rounded");
            avatar.appendChild(imgLink);
            var img = document.createElement("img");
            img.src = avatarUrl;
            imgLink.appendChild(img);
            var message = document.createElement("div");
            message.classList.add("message");
            talk.appendChild(message);
            var speaker = document.createElement("div");
            speaker.classList.add("speaker");
            message.appendChild(speaker);
            var name = document.createElement("a");
            name.innerText = displayName;
            speaker.appendChild(name);
            var small = document.createElement("small");
            small.classList.add("timeleft", "text-muted");
            try {
                var d = new Date(Date.parse(publishedAt));
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
                small.innerText = publishedAt;
            }
            speaker.appendChild(small);
            var filteredText = document.createElement("div");
            filteredText.classList.add("filtered_text");
            filteredText.innerHTML = content;
            message.appendChild(filteredText);
            if (embeds && embeds.length > 0) {
                var ecs = document.createElement("div");
                ecs.classList.add("embed_contents");
                message.appendChild(ecs);
                var d = void 0;
                try {
                    for (var i = 0; i < embeds.length; i++) {
                        var e = embeds[i];
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
                description.innerText = d.description;
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
        if (media.restriction_policy === "Restricted"
            || (media.restriction_policy === "Unknown" && (message.IsNsfw || data.restriction_policy === "Restricted"))) {
            em.classList.add("nsfw");
            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }
        return em;
    }
    function _insertBefore(talk, aft, scroll) {
        var b = document.body;
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
                        var b = document.body;
                        b.scrollTop = talk.offsetTop;
                    }, 1);
                }
            }
            else {
                var b = document.body;
                b.scrollTop = talk.offsetTop;
            }
        }
    }
    function _afterTalkInserted(talk, previousHeight) {
        if (talk.offsetTop < document.body.scrollTop) {
            var delta = talk.clientHeight - (previousHeight || 0);
            if (delta != 0) {
                document.body.scrollTop += delta;
                console.log("scolled " + delta);
            }
        }
        talk.setAttribute("data-height", talk.clientHeight.toString());
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
                    var b = document.body;
                    if (b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight - delta) {
                        // previous viewport was bottom.
                        b.scrollTop = b.scrollHeight - b.clientHeight;
                    }
                    else if (talk.offsetTop < document.body.scrollTop) {
                        b.scrollTop += delta;
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
        };
        for (var i = 0; i < imgs.length; i++) {
            imgs[i].addEventListener("load", handler);
            imgs[i].addEventListener("error", handler);
        }
    }
    document.addEventListener("DOMContentLoaded", function () {
        var windowWidth = window.innerWidth;
        window.addEventListener("resize", function () {
            if (window.innerWidth == windowWidth) {
                return;
            }
            windowWidth = window.innerWidth;
            var talks = document.body.children;
            for (var i = 0; i < talks.length; i++) {
                var talk = talks[i];
                talk.setAttribute("data-height", talk.clientHeight.toString());
            }
        });
        document.addEventListener("scroll", function () {
            var b = document.body;
            var talks = document.body.children;
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
            if (b.scrollHeight < b.clientHeight) {
                return;
            }
            if (b.scrollTop < LOAD_OLDER_MARGIN) {
                console.log("Loading older messages.");
                location.href = "http://kokoro.io/client/control?event=prepend";
            }
            else {
                var fromBottom = b.scrollHeight - b.scrollTop - b.clientHeight;
                if (fromBottom < 4 || (_hasUnread && fromBottom < LOAD_NEWER_MARGIN)) {
                    console.log("Loading newer messages.");
                    location.href = "http://kokoro.io/client/control?event=append";
                }
            }
        });
        var mouseDownStart = null;
        document.body.addEventListener("mousedown", function (e) {
            if (e.button === 0) {
                var b = document.body;
                if (b.scrollTop + b.clientHeight + 4 > b.scrollHeight) {
                    mouseDownStart = new Date().getTime();
                    setTimeout(function () {
                        if (mouseDownStart !== null
                            && mouseDownStart + 800 < new Date().getTime()) {
                            console.log("Loading newer messages.");
                            location.href = "http://kokoro.io/client/control?event=append";
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
    });
})();
