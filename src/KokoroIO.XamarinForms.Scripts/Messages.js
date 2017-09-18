(function () {
    var setMessages = window.setMessages = function (messages) {
        console.debug("Setting " + (messages ? messages.length : 0) + " messages");
        document.body.innerHTML = "";
        _addMessagessCore(messages, null);
        if (messages && messages.length > 0) {
            _showItem(messages[messages.length - 1].Id, false);
        }
    };
    function _getTopId() {
        var e = document.elementFromPoint(8, 8);
        while (e) {
            if (e.classList.contains("talk")) {
                return parseInt(e.getAttribute("data-message-id"), 10);
            }
            e = e.parentElement;
        }
        return -1;
    }
    function _showItem(id, top) {
        if (!id) {
            return;
        }
        console.debug("showing item[" + id + "] to " + (top ? "top" : "bottom"));
        var talk = document.getElementById('talk' + id);
        if (!talk) {
            console.warn("#talk" + id + " is not found");
            return;
        }
        var st;
        if (top) {
            st = talk.offsetTop;
        }
        else {
            st = talk.offsetTop + document.body.clientHeight - talk.clientHeight;
        }
        document.body.scrollTop = st;
    }
    function _addMessagessCore(messages, merged) {
        if (messages) {
            var b = document.body;
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
                        document.body.insertBefore(talk, prev);
                        if (id == pid) {
                            _afterTalkInserted(talk, prev.clientHeight);
                            prev.remove();
                        }
                        else {
                            _afterTalkInserted(talk);
                            j++;
                        }
                        break;
                    }
                    else if (id < aid) {
                        // console.debug("Inserting message[" + id + "] before " + aid);
                        var talk = createTaklElement(m);
                        document.body.insertBefore(talk, aft);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    }
                    else {
                        j++;
                    }
                }
            }
        }
        updateContinued(merged);
    }
    window.addMessages = function (messages, merged) {
        console.debug("Adding " + (messages ? messages.length : 0) + " messages");
        var tid = _getTopId();
        var top = tid > 0;
        if (!top && messages && messages.length > 0) {
            tid = messages[messages.length - 1].Id;
        }
        _addMessagessCore(messages, merged);
        _showItem(tid, top);
    };
    var removeMessages = window.removeMessages = function (ids, merged) {
        console.debug("Removing " + (ids ? ids.length : 0) + " messages");
        if (ids) {
            var j = 0;
            for (var i = 0; i < ids.length; i++) {
                var talk = document.getElementById('talk' + ids[i]);
                if (talk) {
                    talk.remove();
                }
            }
        }
        updateContinued(merged);
    };
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
    function updateContinued(merged) {
        if (merged) {
            for (var i = 0; i < merged.length; i++) {
                var m = merged[i];
                var id = m.Id;
                var isMerged = m.IsMerged;
                var talk = document.getElementById('talk' + id);
                if (talk) {
                    talk.classList.remove(!isMerged ? "continued" : "not-continued");
                    talk.classList.add(isMerged ? "continued" : "not-continued");
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
                                ec.appendChild(_createEmbedContent(d, false));
                                break;
                            case 'SingleImage':
                            case 'SingleVideo':
                            case 'SingleAudio':
                                ec.appendChild(_createEmbedContent(d, true));
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
    function _createEmbedContent(d, hideInfo) {
        var r = document.createElement("div");
        r.classList.add("embed-" + d.type.toLowerCase());
        if (!hideInfo) {
            var meta = document.createElement("div");
            meta.classList.add("meta");
            r.appendChild(meta);
            if (d.metadata_image) {
                var m = d.metadata_image;
                var thumb = document.createElement("div");
                thumb.classList.add("thumb");
                meta.appendChild(thumb);
                thumb.appendChild(_createMediaDiv(m, "embed-thumbnail"));
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
            r.appendChild(medias);
            for (var i = 0; i < d.medias.length; i++) {
                var m = d.medias[i];
                if (m) {
                    medias.appendChild(_createMediaDiv(m));
                }
            }
        }
        return r;
    }
    function _createMediaDiv(m, className) {
        var em = document.createElement("div");
        em.classList.add(className || "embed_media");
        var a = document.createElement("a");
        a.href = m.location || m.raw_url;
        em.appendChild(a);
        var img = document.createElement("img");
        img.classList.add("img-rounded");
        img.src = (m.thumbnail ? m.thumbnail.url : null) || m.raw_url;
        a.appendChild(img);
        if (m.restriction_policy === "Restricted") {
            em.classList.add("nsfw");
            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }
        return em;
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
                    talk.setAttribute("data-loading-images", Math.max(0, (parseInt(talk.getAttribute("data-loading-images"), 10) - 1) || 0));
                    var ph = parseInt(talk.getAttribute("data-height"), 10);
                    if (ph > 0 && talk.offsetTop < document.body.scrollTop) {
                        var delta = talk.clientHeight - ph;
                        if (delta != 0) {
                            document.body.scrollTop += delta;
                            console.log("scolled " + delta);
                        }
                    }
                    talk.setAttribute("data-height", talk.clientHeight);
                    break;
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
                var margin = 60;
                var hidden = (talk.offsetTop + talk.clientHeight + margin < b.scrollTop
                    || b.scrollTop + b.clientHeight < talk.offsetTop - margin)
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
            if (b.scrollTop < 4) {
                console.log("Loading older messages.");
                location.href = "http://kokoro.io/client/control?event=prepend";
            }
            else if (b.scrollTop + b.clientHeight + 4 > b.scrollHeight) {
                console.log("Loading newer messages.");
                location.href = "http://kokoro.io/client/control?event=append";
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
