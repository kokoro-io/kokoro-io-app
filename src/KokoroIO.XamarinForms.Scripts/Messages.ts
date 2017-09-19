interface MergeInfo {
    Id: number;
    IsMerged: boolean;
}
interface MessageInfo extends MergeInfo {
    Avatar: string;
    DisplayName: string;
    PublishedAt: string;
    Content: string;
    EmbedContents: EmbedContent[];
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
    removeMessages(ids: number[], merged: MergeInfo[]);
    showMessage(id: number, toTop: boolean, showNewMessage?: boolean);
    setHasUnread(value: boolean);
}

(function () {
    const IS_BOTTOM_MARGIN = 15;
    const IS_TOP_MARGIN = 4;
    const LOAD_OLDER_MARGIN = 200;
    const LOAD_NEWER_MARGIN = 200;
    const HIDE_CONTENT_MARGIN = 60;

    var _hasUnread = false;

    window.setHasUnread = function (value: boolean) {
        _hasUnread = !!value;
    };

    window.setMessages = function (messages: MessageInfo[]) {
        console.debug("Setting " + (messages ? messages.length : 0) + " messages");
        document.body.innerHTML = "";
        _addMessagessCore(messages, null, false);

        var b = document.body;
        b.scrollTop = b.scrollHeight - b.clientHeight;
    }

    window.addMessages = function (messages: MessageInfo[], merged: MergeInfo[], showNewMessage?: boolean) {
        console.debug("Adding " + (messages ? messages.length : 0) + " messages");

        var isEmpty = document.body.children.length === 0;
        showNewMessage = showNewMessage && !isEmpty;

        _addMessagessCore(messages, merged, !showNewMessage && !isEmpty);

        if (isEmpty) {
            var b = document.body;
            b.scrollTop = b.scrollHeight - b.clientHeight;
        } else if (showNewMessage && messages && messages.length > 0) {
            var minId = Number.MAX_VALUE;
            messages.forEach(v => minId = Math.min(minId, v.Id));

            var talk = document.getElementById("talk" + minId);
            if (talk) {
                _bringToTop(talk);
            }
        }
    }

    var removeMessages = window.removeMessages = function (ids: number[], merged: MergeInfo[]) {
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
    }

    function _addMessagessCore(messages: MessageInfo[], merged: MergeInfo[], scroll: boolean) {
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

                for (; ;) {
                    var prev = <HTMLDivElement>b.children[j];
                    var aft = <HTMLDivElement>b.children[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;

                    if (!prev || (id != pid && !aft)) {
                        // console.debug("Appending message[" + id + "]");
                        var talk = createTaklElement(m);
                        document.body.appendChild(talk);
                        _afterTalkInserted(talk);
                        j++;
                        break;
                    } else if (id <= pid) {
                        var talk = createTaklElement(m);
                        if (id == pid) {
                            let shoudScroll = scroll && aft && aft.offsetTop - IS_TOP_MARGIN < b.scrollTop;
                            var st = b.scrollTop - prev.clientHeight;

                            document.body.insertBefore(talk, prev);
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
                        var talk = createTaklElement(m);

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
        console.debug(`showing message[${id}]`);
        var talk = document.getElementById("talk" + id);
        if (talk) {
            var b = document.body;
            console.log(`current scrollTo is ${b.scrollTop}, and offsetTop is ${talk.offsetTop}`);
            if (talk.offsetTop < b.scrollTop || toTop) {
                console.log(`scrolling to ${talk.offsetTop}`);
                b.scrollTop = talk.offsetTop;
            } else if (b.scrollTop + b.clientHeight < talk.offsetTop - talk.clientHeight) {
                console.log(`scrolling to ${talk.offsetTop - b.clientHeight}`);
                b.scrollTop = talk.offsetTop - b.clientHeight;
            }
        }
    };

    function updateContinued(merged: MergeInfo[], scroll: boolean) {
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

    function _padLeft(i: number, l: number): string {
        var offset = l == 1 ? 10 : l == 2 ? 100 : Math.pow(10, l);

        if (i > offset) {
            var s = i.toFixed(0);
            return s.substr(s.length - l, l);
        }

        return (i + offset).toFixed(0).substr(1, l);
    }

    function createTaklElement(m: MessageInfo): HTMLDivElement {
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
                let d = new Date(Date.parse(publishedAt));
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
            } catch (ex) {
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

                let d;
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

    function _createEmbedContent(d: EmbedData, hideInfo: boolean): HTMLDivElement {
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

                thumb.appendChild(_createMediaDiv(m, d, "embed-thumbnail"));
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
                    medias.appendChild(_createMediaDiv(m, d));
                }
            }
        }

        return r;
    }

    function _createMediaDiv(m: EmbedDataMedia, d: EmbedData, className?: string): HTMLDivElement {
        var em = document.createElement("div");
        em.classList.add(className || "embed_media");

        var a = document.createElement("a");
        a.href = m.location || m.raw_url || d.url;
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

    function _insertBefore(talk: HTMLDivElement, aft: HTMLDivElement, scroll: boolean) {
        var b = document.body;
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
            var b = document.body;
            b.scrollTop = talk.offsetTop;
        }
    }

    function _afterTalkInserted(talk: HTMLDivElement, previousHeight?: number) {
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
                    var delta = talk.clientHeight - ph;
                    var b = document.body;
                    if (b.scrollTop + b.clientHeight + IS_BOTTOM_MARGIN > b.scrollHeight - delta) {
                        // previous viewport was bottom.
                        b.scrollTop = b.scrollHeight - b.clientHeight;
                    } else if (talk.offsetTop < document.body.scrollTop) {
                        b.scrollTop += delta;
                    }
                    talk.setAttribute("data-height", talk.clientHeight);

                    break;
                }

                talk = talk.parentElement;
            }

            img.removeEventListener("load", handler);
            img.removeEventListener("error", handler);
        }

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

            if (b.scrollHeight < b.clientHeight) {
                return;
            }

            if (b.scrollTop < LOAD_OLDER_MARGIN) {
                console.log("Loading older messages.");
                location.href = "http://kokoro.io/client/control?event=prepend";
            } else {
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